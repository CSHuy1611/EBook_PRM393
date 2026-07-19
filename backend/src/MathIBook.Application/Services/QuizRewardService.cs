using MathIBook.Application.DTOs;
using MathIBook.Application.Interfaces;
using MathIBook.Domain.Entities;
using MathIBook.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MathIBook.Application.Services;

public class QuizRewardService : IQuizRewardService
{
    // Giá trị fallback dùng khi database chưa có RewardPolicy phù hợp.
    private const int LegacyCoinsPerCorrectAnswer = 10;
    private const int LegacyPerfectBonusCoins = 5;

    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<QuizRewardService> _logger;

    public QuizRewardService(IUnitOfWork unitOfWork, ILogger<QuizRewardService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<int> AwardAsync(Guid userId, QuizRewardContext context)
    {
        // Khóa theo user+attempt: một attempt chỉ có đúng một giao dịch thưởng.
        var idempotencyKey = $"quiz_reward:{userId:N}:{context.Attempt.Id:N}";
        var existing = await _unitOfWork.CoinTransactions.FirstOrDefaultAsync(
            transaction => transaction.IdempotencyKey == idempotencyKey);

        if (existing is not null)
        {
            // Retry trả lại số xu cũ thay vì cộng lại vào User.Coins.
            return existing.Amount;
        }

        // Chính sách được chọn theo quiz và thời điểm attempt xảy ra.
        var now = context.Attempt.CreatedAt;
        var policy = await ResolvePolicyAsync(context.Quiz, now);
        var calculation = Calculate(context, policy);

        if (policy?.DailyCoinLimit is int dailyLimit)
        {
            // Tính tổng giao dịch trong cùng ngày để chặn vượt giới hạn thưởng.
            var startOfDay = now.Date;
            var nextDay = startOfDay.AddDays(1);
            var transactions = await _unitOfWork.CoinTransactions.FindAsync(
                transaction =>
                    transaction.UserId == userId
                    && transaction.CreatedAt >= startOfDay
                    && transaction.CreatedAt < nextDay);
            var awardedToday = transactions.Sum(transaction => transaction.Amount);
            // Phần còn lại có thể bằng 0 nhưng vẫn không âm.
            calculation.Coins = Math.Min(calculation.Coins, Math.Max(0, dailyLimit - awardedToday));
        }

        var user = await _unitOfWork.Users.GetByIdAsync(userId)
            ?? throw new InvalidOperationException("User not found.");

        // Cập nhật số dư trước để BalanceAfter trong transaction phản ánh số mới.
        user.Coins += calculation.Coins;
        user.CoinsUpdatedAt = now;
        user.UpdatedAt = now;
        _unitOfWork.Users.Update(user);

        // SourceType giúp màn hình lịch sử xu chọn đúng icon/nhãn.
        var transaction = new CoinTransaction
        {
            UserId = userId,
            Amount = calculation.Coins,
            SourceType = context.Quiz.QuizType == Domain.Enums.QuizType.Chapter
                ? "chapter_quiz"
                : "lesson_quiz",
            SourceId = context.Attempt.Id,
            RewardPolicyId = calculation.RewardPolicyId,
            // Lưu clientAttemptId để truy vết giao dịch về lần làm offline ban đầu.
            ClientAttemptId = context.Attempt.ClientAttemptId,
            IdempotencyKey = idempotencyKey,
            BalanceAfter = user.Coins,
            Description = calculation.Description,
            CreatedAt = now
        };

        // User balance và transaction được lưu qua cùng UnitOfWork.
        await _unitOfWork.CoinTransactions.AddAsync(transaction);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            "Awarded {Coins} coins to user {UserId} for attempt {AttemptId}",
            calculation.Coins,
            userId,
            context.Attempt.Id);

        return calculation.Coins;
    }

    internal static RewardCalculationDto Calculate(
        QuizRewardContext context,
        RewardPolicy? policy)
    {
        // Không có policy: dùng công thức legacy để quiz cũ vẫn hoạt động.
        if (policy is null)
        {
            var retryPercent = context.IsRetry ? 100 : 100;
            var baseCoins = context.Attempt.Score * LegacyCoinsPerCorrectAnswer;
            if (context.Attempt.Score == context.Attempt.TotalQuestions)
            {
                baseCoins += LegacyPerfectBonusCoins;
            }

            baseCoins = baseCoins * retryPercent / 100;
            if (context.IsFirstPass)
            {
                baseCoins += context.Quiz.FirstPassCoins;
            }

            return new RewardCalculationDto
            {
                Coins = Math.Max(0, baseCoins),
                Description = $"Nhận {Math.Max(0, baseCoins)} xu từ {context.Attempt.Score}/{context.Attempt.TotalQuestions} câu đúng."
            };
        }

        // Phần thưởng lặp lại = số câu đúng × xu mỗi câu + bonus điểm tuyệt đối.
        var repeatableCoins = context.Attempt.Score * policy.CoinsPerCorrectAnswer;
        if (context.Attempt.Score == context.Attempt.TotalQuestions)
        {
            repeatableCoins += policy.PerfectScoreBonusCoins;
        }

        // Retry có thể chỉ nhận một tỷ lệ phần thưởng lặp lại.
        if (context.IsRetry)
        {
            repeatableCoins = repeatableCoins * policy.RetryRewardPercent / 100;
        }

        // Bonus first-pass/chapter chỉ cấp đúng lần đầu đạt.
        var firstPassCoins = 0;
        if (context.IsFirstPass)
        {
            firstPassCoins += policy.FirstPassBonusCoins + context.Quiz.FirstPassCoins;
            if (context.Quiz.QuizType == Domain.Enums.QuizType.Chapter)
            {
                firstPassCoins += policy.ChapterCompletionBonusCoins;
            }
        }

        var coins = Math.Max(0, repeatableCoins + firstPassCoins);
        return new RewardCalculationDto
        {
            Coins = coins,
            RewardPolicyId = policy.Id,
            Description = $"Nhận {coins} xu theo chính sách {policy.Name}."
        };
    }

    private async Task<RewardPolicy?> ResolvePolicyAsync(Quiz quiz, DateTime occurredAt)
    {
        if (quiz.RewardPolicyId.HasValue)
        {
            var assigned = await _unitOfWork.RewardPolicies.GetByIdAsync(quiz.RewardPolicyId.Value);
            if (assigned is not null
                && assigned.IsActive
                && assigned.EffectiveFrom <= occurredAt
                && (!assigned.EffectiveTo.HasValue || assigned.EffectiveTo > occurredAt))
            {
                return assigned;
            }
        }

        var policies = await _unitOfWork.RewardPolicies.FindAsync(
            candidate =>
                candidate.QuizType == quiz.QuizType
                && candidate.IsActive
                && candidate.EffectiveFrom <= occurredAt
                && (!candidate.EffectiveTo.HasValue || candidate.EffectiveTo > occurredAt));

        return policies.OrderByDescending(candidate => candidate.EffectiveFrom).FirstOrDefault();
    }
}
