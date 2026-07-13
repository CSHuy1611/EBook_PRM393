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
    public string ConditionType { get; set; } = string.Empty; // complete_chapter/complete_book/perfect_quiz_streak/total_coins

    public string? ConditionValue { get; set; } // JSONB nullable

    public ICollection<UserBadge> UserBadges { get; set; } = new List<UserBadge>();
}
