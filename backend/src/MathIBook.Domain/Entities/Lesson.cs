using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MathIBook.Domain.Entities;

public class Lesson
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid ChapterId { get; set; }

    [Required, MaxLength(250)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string ContentBody { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? SimulationType { get; set; }

    public int OrderIndex { get; set; }

    public bool IsPublished { get; set; }

    public bool IsDeleted { get; set; }

    public Guid? CurriculumTopicId { get; set; }

    public int ContentVersion { get; set; } = 1;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? PublishedAt { get; set; }

    [ForeignKey(nameof(ChapterId))]
    public Chapter Chapter { get; set; } = null!;

    [ForeignKey(nameof(CurriculumTopicId))]
    public CurriculumTopic? CurriculumTopic { get; set; }

    public ICollection<Question> Questions { get; set; } = new List<Question>();
    public ICollection<Progress> Progresses { get; set; } = new List<Progress>();
    public ICollection<QuizAttempt> QuizAttempts { get; set; } = new List<QuizAttempt>();
    public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
}
