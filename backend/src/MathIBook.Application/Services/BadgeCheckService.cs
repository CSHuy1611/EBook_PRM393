using System.Text.Json;
using MathIBook.Application.DTOs;
using MathIBook.Application.Interfaces;
using MathIBook.Domain.Entities;
using MathIBook.Domain.Enums;
using MathIBook.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MathIBook.Application.Services;

public class BadgeCheckService : IBadgeCheckService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<BadgeCheckService> _logger;

    public BadgeCheckService(IUnitOfWork unitOfWork, ILogger<BadgeCheckService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<List<BadgeEarnedDto>> CheckAndAwardBadgesAsync(
        Guid userId,
        Guid? contextChapterId = null)
    {
        // Không thể xét badge nếu user trong JWT không còn tồn tại.
        var user = await _unitOfWork.Users.GetByIdAsync(userId)
            ?? throw new InvalidOperationException("User not found.");
        var earnedBadges = new List<BadgeEarnedDto>();
        // Chỉ badge đang hoạt động và chưa soft-delete mới được xét.
        var badges = (await _unitOfWork.Badges.GetAllAsync())
            .Where(badge => badge.IsActive && !badge.IsDeleted)
            .ToList();
        // HashSet giúp kiểm tra badge đã nhận O(1) và là lớp chống trao trùng đầu tiên.
        var alreadyEarned = (await _unitOfWork.UserBadges.FindAsync(
                userBadge => userBadge.UserId == userId))
            .Select(userBadge => userBadge.BadgeId)
            .ToHashSet();

        foreach (var badge in badges)
        {
            // Badge đã nhận không được đánh giá/cộng thưởng lại.
            if (alreadyEarned.Contains(badge.Id))
            {
                continue;
            }

            // Ưu tiên rules cấu trúc; badge cũ chưa migrate vẫn dùng condition legacy.
            var rules = (await _unitOfWork.BadgeRules.FindAsync(rule => rule.BadgeId == badge.Id))
                .OrderBy(rule => rule.OrderIndex)
                .ToList();
            var conditionMet = rules.Count > 0
                ? await CheckStructuredRulesAsync(userId, badge, badge.RuleMatchMode, rules)
                : await CheckLegacyConditionAsync(userId, badge);

            // Không đạt thì chuyển sang badge tiếp theo mà không ghi database.
            if (!conditionMet)
            {
                continue;
            }

            // Dùng chung một timestamp cho UserBadge, CoinTransaction và Notification.
            var occurredAt = DateTime.UtcNow;
            await _unitOfWork.UserBadges.AddAsync(new UserBadge
            {
                UserId = userId,
                BadgeId = badge.Id,
                BadgeRuleId = rules.Count == 1 ? rules[0].Id : null,
                SourceId = contextChapterId ?? badge.Id,
                EarnedAt = occurredAt
            });

            // RewardCoins chỉ do server quyết định; client không được truyền số xu thưởng.
            if (badge.RewardCoins > 0)
            {
                user.Coins += badge.RewardCoins;
                user.CoinsUpdatedAt = occurredAt;
                user.UpdatedAt = occurredAt;
                _unitOfWork.Users.Update(user);

                // IdempotencyKey duy nhất theo user+badge ngăn giao dịch xu bị ghi hai lần.
                await _unitOfWork.CoinTransactions.AddAsync(new CoinTransaction
                {
                    UserId = userId,
                    Amount = badge.RewardCoins,
                    SourceType = "badge_unlock",
                    SourceId = badge.Id,
                    IdempotencyKey = $"badge_unlock:{userId:N}:{badge.Id:N}",
                    BalanceAfter = user.Coins,
                    Description = $"Nhận huy hiệu: {badge.Title}",
                    CreatedAt = occurredAt
                });
            }

            // Mỗi badge mới tạo một thông báo để Student thấy thành tích vừa đạt.
            await _unitOfWork.Notifications.AddAsync(new Notification
            {
                UserId = userId,
                Title = "Bạn vừa nhận huy hiệu mới",
                Body = $"Chúc mừng! Bạn đã nhận huy hiệu “{badge.Title}”.",
                Link = "/badges",
                Type = "badge_awarded",
                RelatedEntityId = badge.Id,
                CreatedAt = occurredAt
            });
            // Lưu badge, xu và notification sau khi toàn bộ entity đã được chuẩn bị.
            await _unitOfWork.SaveChangesAsync();

            // Cập nhật bộ nhớ trong vòng lặp để không xét lại badge vừa trao.
            alreadyEarned.Add(badge.Id);
            earnedBadges.Add(new BadgeEarnedDto
            {
                BadgeId = badge.Id,
                Title = badge.Title,
                Description = badge.Description,
                IconUrl = badge.IconUrl
            });

            _logger.LogInformation(
                "Badge {BadgeId} awarded to user {UserId}",
                badge.Id,
                userId);
        }

        return earnedBadges;
    }

    private async Task<bool> CheckStructuredRulesAsync(
        Guid userId,
        Badge badge,
        string matchMode,
        IReadOnlyCollection<BadgeRule> rules)
    {
        // Đánh giá từng rule độc lập rồi kết hợp theo RuleMatchMode.
        var results = new List<bool>();
        foreach (var rule in rules)
        {
            results.Add(await CheckRuleAsync(userId, badge, rule));
        }

        // ANY chỉ cần một rule đúng; mặc định còn lại là ALL.
        return string.Equals(matchMode, "ANY", StringComparison.OrdinalIgnoreCase)
            ? results.Any(result => result)
            : results.All(result => result);
    }

    private Task<bool> CheckRuleAsync(Guid userId, Badge badge, BadgeRule rule)
    {
        return rule.RuleType.ToLowerInvariant() switch
        {
            "complete_chapter" => CheckCompleteChapterAsync(userId, rule.TargetChapterId),
            "total_coins" => CheckTotalCoinsAsync(userId, ResolveThreshold(rule, badge)),
            "passed_quizzes" => CheckPassedQuizCountAsync(userId, ResolveThreshold(rule, badge)),
            "perfect_quiz_streak" => CheckPerfectQuizStreakAsync(userId, ResolveThreshold(rule, badge)),
            "complete_book" => CheckCompleteBookAsync(userId),
            _ => Task.FromResult(false)
        };
    }

    private Task<bool> CheckLegacyConditionAsync(Guid userId, Badge badge)
    {
        return badge.ConditionType.ToLowerInvariant() switch
        {
            "complete_chapter" => CheckCompleteChapterAsync(userId, TryExtractGuid(badge.ConditionValue)),
            "complete_book" => CheckCompleteBookAsync(userId),
            "perfect_quiz_streak" => CheckPerfectQuizStreakAsync(
                userId,
                TryExtractInt(badge.ConditionValue)),
            "total_coins" => CheckTotalCoinsAsync(userId, TryExtractInt(badge.ConditionValue)),
            "passed_quizzes" => CheckPassedQuizCountAsync(userId, TryExtractInt(badge.ConditionValue)),
            _ => Task.FromResult(false)
        };
    }

    private async Task<bool> CheckCompleteChapterAsync(Guid userId, Guid? chapterId)
    {
        if (!chapterId.HasValue)
        {
            return false;
        }

        var progress = await _unitOfWork.ChapterProgresses.FirstOrDefaultAsync(
            item => item.UserId == userId && item.ChapterId == chapterId.Value);
        return progress?.Status == LearningStatus.Passed;
    }

    private async Task<bool> CheckCompleteBookAsync(Guid userId)
    {
        var chapters = (await _unitOfWork.Chapters.FindAsync(
                chapter => chapter.IsPublished && !chapter.IsDeleted))
            .Select(chapter => chapter.Id)
            .ToList();
        if (chapters.Count == 0)
        {
            return false;
        }

        var completed = (await _unitOfWork.ChapterProgresses.FindAsync(
                item => item.UserId == userId && item.Status == LearningStatus.Passed))
            .Select(item => item.ChapterId)
            .ToHashSet();
        return chapters.All(completed.Contains);
    }

    private async Task<bool> CheckPerfectQuizStreakAsync(Guid userId, int? requiredStreak)
    {
        if (!requiredStreak.HasValue || requiredStreak.Value <= 0)
        {
            return false;
        }

        var attempts = (await _unitOfWork.QuizAttempts.FindAsync(
                attempt => attempt.UserId == userId))
            .OrderByDescending(attempt => attempt.CreatedAt)
            .Take(requiredStreak.Value)
            .ToList();
        return attempts.Count == requiredStreak.Value
            && attempts.All(attempt =>
                attempt.TotalQuestions > 0
                && attempt.Score == attempt.TotalQuestions);
    }

    private async Task<bool> CheckPassedQuizCountAsync(Guid userId, int? requiredCount)
    {
        if (!requiredCount.HasValue || requiredCount.Value <= 0)
        {
            return false;
        }

        var attempts = await _unitOfWork.QuizAttempts.FindAsync(
            attempt => attempt.UserId == userId && attempt.IsPassed);
        return attempts
            .Select(attempt => attempt.QuizId)
            .Where(quizId => quizId.HasValue)
            .Distinct()
            .Count() >= requiredCount.Value;
    }

    private async Task<bool> CheckTotalCoinsAsync(Guid userId, int? requiredCoins)
    {
        if (!requiredCoins.HasValue || requiredCoins.Value < 0)
        {
            return false;
        }

        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        return user is not null && user.Coins >= requiredCoins.Value;
    }

    private static Guid? TryExtractGuid(string? conditionValue)
    {
        if (string.IsNullOrWhiteSpace(conditionValue))
        {
            return null;
        }

        if (Guid.TryParse(conditionValue.Trim('"'), out var direct))
        {
            return direct;
        }

        try
        {
            var values = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(conditionValue);
            if (values is not null
                && values.TryGetValue("chapterId", out var chapterId)
                && Guid.TryParse(chapterId.ToString(), out var parsed))
            {
                return parsed;
            }
        }
        catch (JsonException)
        {
        }

        return null;
    }

    private static int? ResolveThreshold(BadgeRule rule, Badge badge) =>
        rule.ThresholdValue is > 0
            ? rule.ThresholdValue.Value
            : TryExtractInt(badge.ConditionValue);

    private static int? TryExtractInt(string? conditionValue)
    {
        if (string.IsNullOrWhiteSpace(conditionValue))
        {
            return null;
        }

        if (int.TryParse(conditionValue.Trim('"'), out var direct))
        {
            return direct;
        }

        try
        {
            var values = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(conditionValue);
            if (values is null)
            {
                return null;
            }

            foreach (var key in new[] { "value", "streak", "coins", "count" })
            {
                if (values.TryGetValue(key, out var value) && value.TryGetInt32(out var parsed))
                {
                    return parsed;
                }
            }
        }
        catch (JsonException)
        {
        }

        return null;
    }
}
