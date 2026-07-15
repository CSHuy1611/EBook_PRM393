using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MathIBook.Domain.Entities;

public class QuizAttempt
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }

    public Guid? LessonId { get; set; }

    public Guid? QuizId { get; set; }

    public Guid? ClientAttemptId { get; set; }

    public int Score { get; set; }

    public int TotalQuestions { get; set; }

    public decimal Score10 { get; set; }

    public bool IsPassed { get; set; }

    public int CoinsEarned { get; set; }

    public int DurationSeconds { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ClientCreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? RewardProcessedAt { get; set; }

    public DateTime? SyncedAt { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [ForeignKey(nameof(LessonId))]
    public Lesson? Lesson { get; set; }

    [ForeignKey(nameof(QuizId))]
    public Quiz? Quiz { get; set; }

    public ICollection<QuizAttemptAnswer> Answers { get; set; } = new List<QuizAttemptAnswer>();
}
