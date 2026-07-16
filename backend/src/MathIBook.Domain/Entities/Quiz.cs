using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MathIBook.Domain.Enums;

namespace MathIBook.Domain.Entities;

public class Quiz
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public QuizType QuizType { get; set; }

    public Guid? LessonId { get; set; }

    public Guid? ChapterId { get; set; }

    public Guid? RewardPolicyId { get; set; }

    [Required, MaxLength(250)]
    public string Title { get; set; } = string.Empty;

    public decimal PassScore { get; set; } = 5.0m;

    public int DurationSeconds { get; set; } = 900;

    public int FirstPassCoins { get; set; }

    public bool IsPublished { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? PublishedAt { get; set; }

    [ForeignKey(nameof(LessonId))]
    public Lesson? Lesson { get; set; }

    [ForeignKey(nameof(ChapterId))]
    public Chapter? Chapter { get; set; }

    [ForeignKey(nameof(RewardPolicyId))]
    public RewardPolicy? RewardPolicy { get; set; }

    public ICollection<QuizQuestion> QuizQuestions { get; set; } = new List<QuizQuestion>();
    public ICollection<QuizAttempt> Attempts { get; set; } = new List<QuizAttempt>();
    public ICollection<BadgeRule> BadgeRules { get; set; } = new List<BadgeRule>();

    public bool HasValidTarget() => QuizType switch
    {
        QuizType.Lesson => LessonId.HasValue && !ChapterId.HasValue,
        QuizType.Chapter => ChapterId.HasValue && !LessonId.HasValue,
        _ => false
    };
}
