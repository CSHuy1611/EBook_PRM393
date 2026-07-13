using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MathIBook.Domain.Entities;

public class QuizAttemptAnswer
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid AttemptId { get; set; }

    [Required]
    public Guid QuestionId { get; set; }

    public int SelectedOption { get; set; }

    public bool IsCorrect { get; set; }

    [ForeignKey(nameof(AttemptId))]
    public QuizAttempt Attempt { get; set; } = null!;

    [ForeignKey(nameof(QuestionId))]
    public Question Question { get; set; } = null!;
}
