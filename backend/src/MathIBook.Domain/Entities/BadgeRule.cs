using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MathIBook.Domain.Entities;

public class BadgeRule
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid BadgeId { get; set; }

    [Required, MaxLength(50)]
    public string RuleType { get; set; } = string.Empty;

    public Guid? TargetChapterId { get; set; }

    public Guid? TargetQuizId { get; set; }

    public int? ThresholdValue { get; set; }

    public int OrderIndex { get; set; }

    public string? Parameters { get; set; }

    [ForeignKey(nameof(BadgeId))]
    public Badge Badge { get; set; } = null!;

    [ForeignKey(nameof(TargetChapterId))]
    public Chapter? TargetChapter { get; set; }

    [ForeignKey(nameof(TargetQuizId))]
    public Quiz? TargetQuiz { get; set; }
}
