using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MathIBook.Domain.Entities;

public class Progress
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid LessonId { get; set; }

    public bool IsCompleted { get; set; } = false;

    public int BestScore { get; set; } = 0;

    public DateTime? CompletedAt { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ClientUpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [ForeignKey(nameof(LessonId))]
    public Lesson Lesson { get; set; } = null!;
}
