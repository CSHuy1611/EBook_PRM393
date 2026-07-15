using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MathIBook.Domain.Entities;

public class Question
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? LessonId { get; set; }

    public Guid? ChapterId { get; set; }

    [Required]
    public string QuestionText { get; set; } = string.Empty;

    [Required]
    public string Options { get; set; } = "[]";

    public int CorrectOption { get; set; }

    public string? Explanation { get; set; }

    public int OrderIndex { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(LessonId))]
    public Lesson? Lesson { get; set; }

    [ForeignKey(nameof(ChapterId))]
    public Chapter? Chapter { get; set; }

    public ICollection<QuizQuestion> QuizQuestions { get; set; } = new List<QuizQuestion>();
}
