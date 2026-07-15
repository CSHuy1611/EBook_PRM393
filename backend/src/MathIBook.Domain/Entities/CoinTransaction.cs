using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MathIBook.Domain.Entities;

public class CoinTransaction
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }

    public int Amount { get; set; }

    [Required, MaxLength(50)]
    public string SourceType { get; set; } = string.Empty;

    public Guid? SourceId { get; set; }

    public Guid? RewardPolicyId { get; set; }

    public Guid? ClientAttemptId { get; set; }

    [MaxLength(150)]
    public string? IdempotencyKey { get; set; }

    public int BalanceAfter { get; set; }

    [Required, MaxLength(255)]
    public string Description { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [ForeignKey(nameof(RewardPolicyId))]
    public RewardPolicy? RewardPolicy { get; set; }
}
