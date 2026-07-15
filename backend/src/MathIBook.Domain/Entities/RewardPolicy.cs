using System.ComponentModel.DataAnnotations;
using MathIBook.Domain.Enums;

namespace MathIBook.Domain.Entities;

public class RewardPolicy
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    public QuizType QuizType { get; set; }

    public int CoinsPerCorrectAnswer { get; set; }

    public int FirstPassBonusCoins { get; set; }

    public int PerfectScoreBonusCoins { get; set; }

    public int ChapterCompletionBonusCoins { get; set; }

    public int RetryRewardPercent { get; set; }

    public int? DailyCoinLimit { get; set; }

    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;

    public DateTime? EffectiveTo { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
    public ICollection<CoinTransaction> CoinTransactions { get; set; } = new List<CoinTransaction>();
}
