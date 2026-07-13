using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MathIBook.Domain.Entities;

public class Question
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid LessonId { get; set; }

    [Required]
    public string QuestionText { get; set; } = string.Empty;

    [Required]
    public string Options { get; set; } = "[]"; // JSONB array of 4 strings

    public int CorrectOption { get; set; } // 0-3

    public string? Explanation { get; set; }

    public int OrderIndex { get; set; }

    [ForeignKey(nameof(LessonId))]
    public Lesson Lesson { get; set; } = null!;
}
