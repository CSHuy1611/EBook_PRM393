using MathIBook.Domain.Entities;

namespace MathIBook.Application.DTOs;

public class QuizRewardContext
{
    public required Quiz Quiz { get; init; }
    public required QuizAttempt Attempt { get; init; }
    public bool IsFirstPass { get; init; }
    public bool IsRetry { get; init; }
}

public class RewardCalculationDto
{
    public int Coins { get; set; }
    public Guid? RewardPolicyId { get; set; }
    public string Description { get; set; } = string.Empty;
}
