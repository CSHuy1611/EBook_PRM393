using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MathIBook.Domain.Entities;

public class Chapter
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int OrderIndex { get; set; }

    public Guid? CurriculumTopicId { get; set; }

    public bool IsPublished { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? PublishedAt { get; set; }

    [ForeignKey(nameof(CurriculumTopicId))]
    public CurriculumTopic? CurriculumTopic { get; set; }

    public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
    public ICollection<Question> Questions { get; set; } = new List<Question>();
    public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
    public ICollection<ChapterProgress> Progresses { get; set; } = new List<ChapterProgress>();
    public ICollection<BadgeRule> BadgeRules { get; set; } = new List<BadgeRule>();
}
