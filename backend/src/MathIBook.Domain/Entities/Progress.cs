using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MathIBook.Domain.Enums;

namespace MathIBook.Domain.Entities;

public class Progress
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid LessonId { get; set; }

    public bool IsCompleted { get; set; }

    public LearningStatus Status { get; set; } = LearningStatus.NotStarted;

    public bool ContentViewed { get; set; }

    public int BestScore { get; set; }

    public decimal BestScore10 { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? LastViewedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ClientUpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [ForeignKey(nameof(LessonId))]
    public Lesson Lesson { get; set; } = null!;

    public void MarkContentViewed(DateTime occurredAt)
    {
        ContentViewed = true;
        StartedAt ??= occurredAt;
        LastViewedAt = occurredAt;
        if (Status == LearningStatus.NotStarted)
        {
            Status = LearningStatus.InProgress;
        }

        UpdatedAt = occurredAt;
    }

    public void ApplyQuizResult(decimal score10, decimal passScore, DateTime occurredAt)
    {
        if (score10 is < 0 or > 10)
        {
            throw new ArgumentOutOfRangeException(nameof(score10), "Score must be from 0 to 10.");
        }

        if (passScore is < 0 or > 10)
        {
            throw new ArgumentOutOfRangeException(nameof(passScore), "Pass score must be from 0 to 10.");
        }

        BestScore10 = Math.Max(BestScore10, score10);
        StartedAt ??= occurredAt;
        Status = BestScore10 >= passScore ? LearningStatus.Passed : LearningStatus.InProgress;
        IsCompleted = Status == LearningStatus.Passed;
        CompletedAt = IsCompleted ? CompletedAt ?? occurredAt : null;
        UpdatedAt = occurredAt;
    }
}
