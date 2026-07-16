using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MathIBook.Domain.Enums;

namespace MathIBook.Domain.Entities;

public class ChapterProgress
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public Guid ChapterId { get; set; }

    public LearningStatus Status { get; set; } = LearningStatus.NotStarted;

    public decimal BestScore10 { get; set; }

    public DateTime? QuizUnlockedAt { get; set; }

    public DateTime? FirstPassedAt { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ClientUpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [ForeignKey(nameof(ChapterId))]
    public Chapter Chapter { get; set; } = null!;

    public void Unlock(DateTime unlockedAt)
    {
        if (Status == LearningStatus.NotStarted)
            Status = LearningStatus.InProgress;
        QuizUnlockedAt ??= unlockedAt;
        UpdatedAt = unlockedAt;
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
        if (score10 >= passScore)
        {
            Status = LearningStatus.Passed;
            FirstPassedAt ??= occurredAt;
        }
        else if (Status == LearningStatus.NotStarted)
        {
            Status = LearningStatus.InProgress;
        }
        UpdatedAt = occurredAt;
    }
}
