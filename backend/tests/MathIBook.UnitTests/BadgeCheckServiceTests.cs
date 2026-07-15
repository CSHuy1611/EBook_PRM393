using System.Linq.Expressions;
using FluentAssertions;
using MathIBook.Application.Services;
using MathIBook.Domain.Entities;
using MathIBook.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MathIBook.UnitTests;

public class BadgeCheckServiceTests
{
    [Fact]
    public async Task CheckAndAwardBadgesAsync_UsesConfiguredRewardAndCreatesNotification()
    {
        var userId = Guid.NewGuid();
        var badgeId = Guid.NewGuid();
        var user = new User { Id = userId, Coins = 100 };
        var badge = new Badge
        {
            Id = badgeId,
            Title = "Vua cày coin",
            Description = "Đạt đủ xu",
            IconUrl = "/badges/coin-king.png",
            ConditionType = "total_coins",
            ConditionValue = "100",
            RewardCoins = 25,
            IsActive = true
        };
        var setup = CreateSetup(userId, user, [badge], []);
        CoinTransaction? transaction = null;
        Notification? notification = null;
        setup.Transactions.Setup(repository => repository.AddAsync(It.IsAny<CoinTransaction>()))
            .Callback<CoinTransaction>(item => transaction = item)
            .Returns(Task.CompletedTask);
        setup.Notifications.Setup(repository => repository.AddAsync(It.IsAny<Notification>()))
            .Callback<Notification>(item => notification = item)
            .Returns(Task.CompletedTask);

        var service = new BadgeCheckService(
            setup.UnitOfWork.Object,
            Mock.Of<ILogger<BadgeCheckService>>());
        var earned = await service.CheckAndAwardBadgesAsync(userId);

        earned.Should().ContainSingle(item => item.BadgeId == badgeId);
        user.Coins.Should().Be(125);
        transaction!.IdempotencyKey.Should().Be($"badge_unlock:{userId:N}:{badgeId:N}");
        transaction.BalanceAfter.Should().Be(125);
        notification!.Type.Should().Be("badge_awarded");
        setup.UserBadges.Verify(
            repository => repository.AddAsync(It.IsAny<UserBadge>()),
            Times.Once);
    }

    [Fact]
    public async Task CheckAndAwardBadgesAsync_EvaluatesStructuredPassedQuizRule()
    {
        var userId = Guid.NewGuid();
        var badge = new Badge
        {
            Id = Guid.NewGuid(),
            Title = "Chuyên gia quiz",
            Description = "Vượt qua hai quiz",
            IconUrl = "/badges/quiz.png",
            ConditionType = "structured",
            IsActive = true
        };
        var rules = new List<BadgeRule>
        {
            new()
            {
                BadgeId = badge.Id,
                RuleType = "passed_quizzes",
                ThresholdValue = 2,
                OrderIndex = 1
            }
        };
        var setup = CreateSetup(
            userId,
            new User { Id = userId },
            [badge],
            rules);
        var attempts = new Mock<IRepository<QuizAttempt>>();
        attempts.Setup(repository => repository.FindAsync(
                It.IsAny<Expression<Func<QuizAttempt, bool>>>()))
            .ReturnsAsync(
            [
                new QuizAttempt { UserId = userId, QuizId = Guid.NewGuid(), IsPassed = true },
                new QuizAttempt { UserId = userId, QuizId = Guid.NewGuid(), IsPassed = true }
            ]);
        setup.UnitOfWork.SetupGet(unit => unit.QuizAttempts).Returns(attempts.Object);
        setup.Notifications.Setup(repository => repository.AddAsync(It.IsAny<Notification>()))
            .Returns(Task.CompletedTask);

        var service = new BadgeCheckService(
            setup.UnitOfWork.Object,
            Mock.Of<ILogger<BadgeCheckService>>());
        var earned = await service.CheckAndAwardBadgesAsync(userId);

        earned.Should().ContainSingle(item => item.BadgeId == badge.Id);
    }

    private static BadgeTestSetup CreateSetup(
        Guid userId,
        User user,
        List<Badge> badges,
        List<BadgeRule> rules)
    {
        var badgeRepository = new Mock<IRepository<Badge>>();
        badgeRepository.Setup(repository => repository.GetAllAsync()).ReturnsAsync(badges);
        var ruleRepository = new Mock<IRepository<BadgeRule>>();
        ruleRepository.Setup(repository => repository.FindAsync(
                It.IsAny<Expression<Func<BadgeRule, bool>>>()))
            .ReturnsAsync((Expression<Func<BadgeRule, bool>> predicate) =>
                rules.Where(predicate.Compile()).ToList());
        var userBadges = new Mock<IRepository<UserBadge>>();
        userBadges.Setup(repository => repository.FindAsync(
                It.IsAny<Expression<Func<UserBadge, bool>>>()))
            .ReturnsAsync([]);
        userBadges.Setup(repository => repository.AddAsync(It.IsAny<UserBadge>()))
            .Returns(Task.CompletedTask);
        var users = new Mock<IRepository<User>>();
        users.Setup(repository => repository.GetByIdAsync(userId)).ReturnsAsync(user);
        var transactions = new Mock<IRepository<CoinTransaction>>();
        transactions.Setup(repository => repository.AddAsync(It.IsAny<CoinTransaction>()))
            .Returns(Task.CompletedTask);
        var notifications = new Mock<IRepository<Notification>>();
        notifications.Setup(repository => repository.AddAsync(It.IsAny<Notification>()))
            .Returns(Task.CompletedTask);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.SetupGet(unit => unit.Badges).Returns(badgeRepository.Object);
        unitOfWork.SetupGet(unit => unit.BadgeRules).Returns(ruleRepository.Object);
        unitOfWork.SetupGet(unit => unit.UserBadges).Returns(userBadges.Object);
        unitOfWork.SetupGet(unit => unit.Users).Returns(users.Object);
        unitOfWork.SetupGet(unit => unit.CoinTransactions).Returns(transactions.Object);
        unitOfWork.SetupGet(unit => unit.Notifications).Returns(notifications.Object);
        unitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        return new BadgeTestSetup(
            unitOfWork,
            userBadges,
            transactions,
            notifications);
    }

    private sealed record BadgeTestSetup(
        Mock<IUnitOfWork> UnitOfWork,
        Mock<IRepository<UserBadge>> UserBadges,
        Mock<IRepository<CoinTransaction>> Transactions,
        Mock<IRepository<Notification>> Notifications);
}
