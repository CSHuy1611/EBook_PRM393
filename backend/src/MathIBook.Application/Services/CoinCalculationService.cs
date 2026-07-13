using MathIBook.Application.Interfaces;
using MathIBook.Domain.Entities;
using MathIBook.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MathIBook.Application.Services;

public class CoinCalculationService : ICoinCalculationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CoinCalculationService> _logger;

    public CoinCalculationService(IUnitOfWork unitOfWork, ILogger<CoinCalculationService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<int> CalculateQuizCoinsAsync(Guid userId, int score, int totalQuestions, Guid? quizAttemptId)
    {
        if (quizAttemptId.HasValue)
        {
            var existing = await _unitOfWork.CoinTransactions.FirstOrDefaultAsync(
                ct => ct.UserId == userId && ct.SourceId == quizAttemptId.Value && ct.SourceType == "quiz_reward");
            if (existing is not null)
            {
                _logger.LogWarning("Duplicate coin transaction attempted for quiz attempt {QuizAttemptId}", quizAttemptId.Value);
                return 0;
            }
        }

        var coins = score * 10;

        if (score == totalQuestions)
        {
            coins += 5;
        }

        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user is null)
        {
            throw new InvalidOperationException("User not found.");
        }

        user.Coins += coins;
        _unitOfWork.Users.Update(user);

        var transaction = new CoinTransaction
        {
            UserId = userId,
            Amount = coins,
            SourceType = "quiz_reward",
            SourceId = quizAttemptId,
            Description = score == totalQuestions
                ? $"Perfect score! Earned {coins} coins ({score * 10} base + 5 bonus)"
                : $"Earned {coins} coins for scoring {score}/{totalQuestions}",
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.CoinTransactions.AddAsync(transaction);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Awarded {Coins} coins to user {UserId} for quiz attempt {QuizAttemptId}", coins, userId, quizAttemptId);

        return coins;
    }
}
