using System.Linq.Expressions;
using FluentAssertions;
using MathIBook.Application.Services;
using MathIBook.Domain.Entities;
using MathIBook.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MathIBook.UnitTests;

public class DashboardServiceTests
{
    [Fact]
    public async Task GetDashboardAsync_ReturnsEmptyButConsistentDashboard()
    {
        var userId = Guid.NewGuid();
        var unitOfWork = new Mock<IUnitOfWork>();
        var users = new Mock<IRepository<User>>();
        users.Setup(repository => repository.GetByIdAsync(userId))
            .ReturnsAsync(new User
            {
                Id = userId,
                Role = "Student",
                IsActive = true,
                Coins = 15
            });
        unitOfWork.SetupGet(unit => unit.Users).Returns(users.Object);
        unitOfWork.SetupGet(unit => unit.Chapters).Returns(EmptyRepository<Chapter>());
        unitOfWork.SetupGet(unit => unit.Lessons).Returns(EmptyRepository<Lesson>());
        unitOfWork.SetupGet(unit => unit.Progresses).Returns(EmptyRepository<Progress>());
        unitOfWork.SetupGet(unit => unit.ChapterProgresses).Returns(EmptyRepository<ChapterProgress>());
        unitOfWork.SetupGet(unit => unit.Quizzes).Returns(EmptyRepository<Quiz>());
        unitOfWork.SetupGet(unit => unit.QuizAttempts).Returns(EmptyRepository<QuizAttempt>());
        unitOfWork.SetupGet(unit => unit.UserBadges).Returns(EmptyRepository<UserBadge>());
        unitOfWork.SetupGet(unit => unit.Badges).Returns(EmptyRepository<Badge>());
        unitOfWork.SetupGet(unit => unit.CoinTransactions).Returns(EmptyRepository<CoinTransaction>());

        var result = await new DashboardService(
            unitOfWork.Object,
            Mock.Of<ILogger<DashboardService>>()).GetDashboardAsync(userId);

        result.TotalCoins.Should().Be(15);
        result.TotalLessons.Should().Be(0);
        result.CompletedLessons.Should().Be(0);
        result.TotalBadgeCount.Should().Be(0);
        result.ContinueLearning.Should().BeNull();
    }

    private static IRepository<T> EmptyRepository<T>() where T : class
    {
        var repository = new Mock<IRepository<T>>();
        repository.Setup(item => item.FindAsync(
                It.IsAny<Expression<Func<T, bool>>>()))
            .ReturnsAsync([]);
        return repository.Object;
    }
}
