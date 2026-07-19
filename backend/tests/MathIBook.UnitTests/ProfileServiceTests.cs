using System.Linq.Expressions;
using FluentAssertions;
using MathIBook.Application.DTOs;
using MathIBook.Application.Services;
using MathIBook.Domain.Entities;
using MathIBook.Domain.Enums;
using MathIBook.Domain.Interfaces;
using Moq;

namespace MathIBook.UnitTests;

public class ProfileServiceTests
{
    [Fact]
    public async Task GetAsync_ReturnsGamificationAndLearningStatistics()
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Name = "An",
            Email = "an@example.com",
            Role = "Student",
            IsActive = true,
            Coins = 120
        };
        var users = new Mock<IRepository<User>>();
        users.Setup(repository => repository.GetByIdAsync(userId)).ReturnsAsync(user);
        users.Setup(repository => repository.FindAsync(
                It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync([user]);
        var quiz1Id = Guid.NewGuid();
        var quiz2Id = Guid.NewGuid();
        var quizzes = RepositoryWithFind(
        [
            new Quiz { Id = quiz1Id, QuizType = QuizType.Chapter, IsPublished = true },
            new Quiz { Id = quiz2Id, QuizType = QuizType.Chapter, IsPublished = true }
        ]);
        var attempts = RepositoryWithFind(
        [
            new QuizAttempt { UserId = userId, QuizId = quiz1Id, Score10 = 6 },
            new QuizAttempt { UserId = userId, QuizId = quiz2Id, Score10 = 10 }
        ]);
        var progress = RepositoryWithFind(
        [
            new Progress { UserId = userId, Status = LearningStatus.Passed },
            new Progress { UserId = userId, Status = LearningStatus.Passed }
        ]);
        var chapters = RepositoryWithFind(
        [
            new ChapterProgress { UserId = userId, Status = LearningStatus.Passed }
        ]);
        var userBadges = new Mock<IRepository<UserBadge>>();
        userBadges.Setup(repository => repository.GetAllAsync()).ReturnsAsync(
        [
            new UserBadge { UserId = userId, BadgeId = Guid.NewGuid() }
        ]);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.SetupGet(unit => unit.Users).Returns(users.Object);
        unitOfWork.SetupGet(unit => unit.Quizzes).Returns(quizzes.Object);
        unitOfWork.SetupGet(unit => unit.QuizAttempts).Returns(attempts.Object);
        unitOfWork.SetupGet(unit => unit.Progresses).Returns(progress.Object);
        unitOfWork.SetupGet(unit => unit.ChapterProgresses).Returns(chapters.Object);
        unitOfWork.SetupGet(unit => unit.UserBadges).Returns(userBadges.Object);

        var result = await new ProfileService(unitOfWork.Object).GetAsync(userId);

        result.Coins.Should().Be(120);
        result.BadgeCount.Should().Be(1);
        result.Rank.Should().Be(1);
        result.CompletedLessons.Should().Be(2);
        result.CompletedChapters.Should().Be(1);
        result.AverageScore.Should().Be(8);
        result.BestScore.Should().Be(10);
    }

    private static Mock<IRepository<T>> RepositoryWithFind<T>(List<T> items)
        where T : class
    {
        var repository = new Mock<IRepository<T>>();
        repository.Setup(item => item.FindAsync(
                It.IsAny<Expression<Func<T, bool>>>()))
            .ReturnsAsync((Expression<Func<T, bool>> predicate) =>
                items.Where(predicate.Compile()).ToList());
        return repository;
    }
}
