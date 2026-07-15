using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MathIBook.Domain.Entities;

public class QuizQuestion
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid QuizId { get; set; }

    public Guid QuestionId { get; set; }

    public int OrderIndex { get; set; }

    public int Weight { get; set; } = 1;

    [ForeignKey(nameof(QuizId))]
    public Quiz Quiz { get; set; } = null!;

    [ForeignKey(nameof(QuestionId))]
    public Question Question { get; set; } = null!;
}
