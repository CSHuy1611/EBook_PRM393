using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MathIBook.Domain.Entities;

public class UserBadge
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid BadgeId { get; set; }

    public Guid? BadgeRuleId { get; set; }

    public Guid? SourceId { get; set; }

    public DateTime EarnedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [ForeignKey(nameof(BadgeId))]
    public Badge Badge { get; set; } = null!;

    [ForeignKey(nameof(BadgeRuleId))]
    public BadgeRule? BadgeRule { get; set; }
}
