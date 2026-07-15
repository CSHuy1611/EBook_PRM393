using System.ComponentModel.DataAnnotations;

namespace MathIBook.Domain.Entities;

public class Badge
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string IconUrl { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string ConditionType { get; set; } = string.Empty;

    public string? ConditionValue { get; set; }

    [Required, MaxLength(10)]
    public string RuleMatchMode { get; set; } = "ALL";

    public int RewardCoins { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<UserBadge> UserBadges { get; set; } = new List<UserBadge>();
    public ICollection<BadgeRule> Rules { get; set; } = new List<BadgeRule>();
}
