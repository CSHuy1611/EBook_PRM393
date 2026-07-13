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

    public bool IsPublished { get; set; } = false;

    [ForeignKey(nameof(ChapterId))]
    public Chapter Chapter { get; set; } = null!;

    public ICollection<Question> Questions { get; set; } = new List<Question>();
    public ICollection<Progress> Progresses { get; set; } = new List<Progress>();
    public ICollection<QuizAttempt> QuizAttempts { get; set; } = new List<QuizAttempt>();
}
