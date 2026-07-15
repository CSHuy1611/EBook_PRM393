using FluentAssertions;
using MathIBook.Domain.Entities;
using MathIBook.Domain.Enums;

namespace MathIBook.UnitTests;

public class DomainModelTests
{
    [Fact]
    public void LessonProgress_Passes_WhenScoreMeetsConfiguredThreshold()
    {
        var occurredAt = DateTime.UtcNow;
        var progress = new Progress();

        progress.ApplyQuizResult(5.0m, 5.0m, occurredAt);

        progress.Status.Should().Be(LearningStatus.Passed);
        progress.IsCompleted.Should().BeTrue();
        progress.BestScore10.Should().Be(5.0m);
        progress.CompletedAt.Should().Be(occurredAt);
    }

    [Fact]
    public void LessonProgress_RemainsInProgress_WhenScoreIsBelowThreshold()
    {
        var progress = new Progress();

        progress.ApplyQuizResult(4.9m, 5.0m, DateTime.UtcNow);

        progress.Status.Should().Be(LearningStatus.InProgress);
        progress.IsCompleted.Should().BeFalse();
        progress.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void LessonProgress_DoesNotRegress_AfterPassing()
    {
        var progress = new Progress();
        var firstPassAt = DateTime.UtcNow;

        progress.ApplyQuizResult(8.0m, 5.0m, firstPassAt);
        progress.ApplyQuizResult(3.0m, 5.0m, firstPassAt.AddMinutes(5));

        progress.Status.Should().Be(LearningStatus.Passed);
        progress.IsCompleted.Should().BeTrue();
        progress.BestScore10.Should().Be(8.0m);
        progress.CompletedAt.Should().Be(firstPassAt);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(10.1)]
    public void LessonProgress_RejectsScoreOutsideTenPointScale(double score)
    {
        var progress = new Progress();

        var action = () => progress.ApplyQuizResult((decimal)score, 5.0m, DateTime.UtcNow);

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ChapterProgress_Unlock_IsIdempotent()
    {
        var progress = new ChapterProgress();
        var firstUnlockAt = DateTime.UtcNow;

        progress.Unlock(firstUnlockAt);
        progress.Unlock(firstUnlockAt.AddMinutes(5));

        progress.Status.Should().Be(LearningStatus.InProgress);
        progress.QuizUnlockedAt.Should().Be(firstUnlockAt);
    }

    [Fact]
    public void ChapterProgress_PassesChapterQuiz_AndKeepsBestScore()
    {
        var progress = new ChapterProgress();
        var firstPassAt = DateTime.UtcNow;

        progress.ApplyQuizResult(7.5m, 5.0m, firstPassAt);
        progress.ApplyQuizResult(4.0m, 5.0m, firstPassAt.AddMinutes(5));

        progress.Status.Should().Be(LearningStatus.Passed);
        progress.BestScore10.Should().Be(7.5m);
        progress.FirstPassedAt.Should().Be(firstPassAt);
    }

    [Fact]
    public void ChapterProgress_RejectsScoreOutsideTenPointScale()
    {
        var progress = new ChapterProgress();

        var action = () => progress.ApplyQuizResult(10.1m, 5.0m, DateTime.UtcNow);

        action.Should().Throw<ArgumentOutOfRangeException>();
    }
    [Theory]
    [InlineData(QuizType.Lesson, true, false, true)]
    [InlineData(QuizType.Chapter, false, true, true)]
    [InlineData(QuizType.Lesson, true, true, false)]
    [InlineData(QuizType.Chapter, false, false, false)]
    public void Quiz_ValidatesExactlyOneTarget(
        QuizType quizType,
        bool hasLesson,
        bool hasChapter,
        bool expected)
    {
        var quiz = new Quiz
        {
            QuizType = quizType,
            LessonId = hasLesson ? Guid.NewGuid() : null,
            ChapterId = hasChapter ? Guid.NewGuid() : null
        };

        quiz.HasValidTarget().Should().Be(expected);
    }
}
