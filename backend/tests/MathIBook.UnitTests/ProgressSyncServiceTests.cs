using System.Linq.Expressions;
using FluentAssertions;
using MathIBook.Application.DTOs;
using MathIBook.Application.Services;
using MathIBook.Domain.Entities;
using MathIBook.Domain.Enums;
using MathIBook.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MathIBook.UnitTests;

public class ProgressSyncServiceTests
{
    [Fact]
    public async Task SyncProgressAsync_DoesNotTrustForgedClientPassWithoutVerifiedAttempt()
    {
        var userId = Guid.NewGuid();
        var lessonId = Guid.NewGuid();
        var setup = CreateSetup(userId, lessonId, []);
        var service = new ProgressSyncService(
            setup.UnitOfWork.Object,
            Mock.Of<ILogger<ProgressSyncService>>());

        var result = await service.SyncProgressAsync(userId, new ProgressSyncDto
        {
            Items =
            [
                new ProgressItemDto
                {
                    LessonId = lessonId,
                    BestScore = 100,
                    IsCompleted = true,
                    ClientUpdatedAt = DateTime.UtcNow
                }
            ]
        });

        setup.SavedProgress.Should().NotBeNull();
        setup.SavedProgress!.Status.Should().Be(LearningStatus.InProgress);
        setup.SavedProgress.IsCompleted.Should().BeFalse();
        setup.SavedProgress.BestScore10.Should().Be(0);
        setup.SavedProgress.ContentViewed.Should().BeTrue();
        result.Should().ContainSingle(item => !item.IsCompleted);
    }

    [Fact]
    public async Task SyncProgressAsync_UsesBestServerVerifiedAttemptAndNeverRegresses()
    {
        var userId = Guid.NewGuid();
        var lessonId = Guid.NewGuid();
        var quizId = Guid.NewGuid();
        var attempts = new List<QuizAttempt>
        {
            new()
            {
                UserId = userId,
                LessonId = lessonId,
                QuizId = quizId,
                Score = 8,
                TotalQuestions = 10,
                Score10 = 8,
                IsPassed = true
            },
            new()
            {
                UserId = userId,
                LessonId = lessonId,
                QuizId = quizId,
                Score = 3,
                TotalQuestions = 10,
                Score10 = 3,
                IsPassed = false
            }
        };
        var setup = CreateSetup(userId, lessonId, attempts);
        var quizzes = new Mock<IRepository<Quiz>>();
        quizzes.Setup(repository => repository.GetByIdAsync(quizId))
            .ReturnsAsync(new Quiz { Id = quizId, PassScore = 5 });
        setup.UnitOfWork.SetupGet(unit => unit.Quizzes).Returns(quizzes.Object);

        var service = new ProgressSyncService(
            setup.UnitOfWork.Object,
            Mock.Of<ILogger<ProgressSyncService>>());
        var result = await service.SyncProgressAsync(userId, new ProgressSyncDto
        {
            Items =
            [
                new ProgressItemDto
                {
                    LessonId = lessonId,
                    ClientUpdatedAt = DateTime.UtcNow
                }
            ]
        });

        setup.SavedProgress!.BestScore10.Should().Be(8);
        setup.SavedProgress.Status.Should().Be(LearningStatus.Passed);
        result.Single().IsCompleted.Should().BeTrue();
    }

    private static TestSetup CreateSetup(
        Guid userId,
        Guid lessonId,
        List<QuizAttempt> attempts)
    {
        var lessons = new Mock<IRepository<Lesson>>();
        lessons.Setup(repository => repository.GetByIdAsync(lessonId))
            .ReturnsAsync(new Lesson
            {
                Id = lessonId,
                IsPublished = true,
                ChapterId = Guid.NewGuid()
            });

        Progress? savedProgress = null;
        var progresses = new Mock<IRepository<Progress>>();
        progresses.Setup(repository => repository.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<Progress, bool>>>()))
            .ReturnsAsync((Progress?)null);
        progresses.Setup(repository => repository.AddAsync(It.IsAny<Progress>()))
            .Callback<Progress>(progress => savedProgress = progress)
            .Returns(Task.CompletedTask);

        var attemptRepository = new Mock<IRepository<QuizAttempt>>();
        attemptRepository.Setup(repository => repository.FindAsync(
                It.IsAny<Expression<Func<QuizAttempt, bool>>>()))
            .ReturnsAsync((Expression<Func<QuizAttempt, bool>> predicate) =>
                attempts.Where(predicate.Compile()).ToList());

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.SetupGet(unit => unit.Lessons).Returns(lessons.Object);
        unitOfWork.SetupGet(unit => unit.Progresses).Returns(progresses.Object);
        unitOfWork.SetupGet(unit => unit.QuizAttempts).Returns(attemptRepository.Object);
        unitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        return new TestSetup(unitOfWork, () => savedProgress);
    }

    private sealed class TestSetup
    {
        private readonly Func<Progress?> _savedProgress;

        public TestSetup(Mock<IUnitOfWork> unitOfWork, Func<Progress?> savedProgress)
        {
            UnitOfWork = unitOfWork;
            _savedProgress = savedProgress;
        }

        public Mock<IUnitOfWork> UnitOfWork { get; }
        public Progress? SavedProgress => _savedProgress();
    }
}
