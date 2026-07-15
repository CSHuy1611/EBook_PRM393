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

public class QuizRewardServiceTests
{
    [Fact]
    public async Task AwardAsync_AppliesRetryPolicyAndFirstChapterPassBonuses()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Coins = 10 };
        var policy = new RewardPolicy
        {
            Id = Guid.NewGuid(),
            Name = "Chapter policy",
            QuizType = QuizType.Chapter,
            CoinsPerCorrectAnswer = 10,
            FirstPassBonusCoins = 20,
            ChapterCompletionBonusCoins = 50,
            RetryRewardPercent = 50,
            IsActive = true,
            EffectiveFrom = DateTime.UtcNow.AddDays(-1)
        };
        var quiz = new Quiz
        {
            Id = Guid.NewGuid(),
            QuizType = QuizType.Chapter,
            ChapterId = Guid.NewGuid(),
            RewardPolicyId = policy.Id,
            FirstPassCoins = 5
        };
        var attempt = new QuizAttempt
        {
            Id = Guid.NewGuid(),
            ClientAttemptId = Guid.NewGuid(),
            Score = 3,
            TotalQuestions = 4,
            CreatedAt = DateTime.UtcNow
        };

        var users = new Mock<IRepository<User>>();
        users.Setup(repository => repository.GetByIdAsync(userId)).ReturnsAsync(user);
        var policies = new Mock<IRepository<RewardPolicy>>();
        policies.Setup(repository => repository.GetByIdAsync(policy.Id)).ReturnsAsync(policy);
        var transactions = new Mock<IRepository<CoinTransaction>>();
        transactions.Setup(repository => repository.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<CoinTransaction, bool>>>()))
            .ReturnsAsync((CoinTransaction?)null);
        CoinTransaction? saved = null;
        transactions.Setup(repository => repository.AddAsync(It.IsAny<CoinTransaction>()))
            .Callback<CoinTransaction>(item => saved = item)
            .Returns(Task.CompletedTask);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.SetupGet(unit => unit.Users).Returns(users.Object);
        unitOfWork.SetupGet(unit => unit.RewardPolicies).Returns(policies.Object);
        unitOfWork.SetupGet(unit => unit.CoinTransactions).Returns(transactions.Object);
        unitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var service = new QuizRewardService(
            unitOfWork.Object,
            Mock.Of<ILogger<QuizRewardService>>());
        var earned = await service.AwardAsync(userId, new QuizRewardContext
        {
            Quiz = quiz,
            Attempt = attempt,
            IsRetry = true,
            IsFirstPass = true
        });

        earned.Should().Be(90);
        user.Coins.Should().Be(100);
        saved!.SourceType.Should().Be("chapter_quiz");
        saved.RewardPolicyId.Should().Be(policy.Id);
        saved.IdempotencyKey.Should().Be($"quiz_reward:{userId:N}:{attempt.Id:N}");
    }

    [Fact]
    public async Task AwardAsync_ReturnsExistingAmountWithoutAwardingAgain()
    {
        var userId = Guid.NewGuid();
        var attempt = new QuizAttempt { Id = Guid.NewGuid(), CreatedAt = DateTime.UtcNow };
        var transactions = new Mock<IRepository<CoinTransaction>>();
        transactions.Setup(repository => repository.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<CoinTransaction, bool>>>()))
            .ReturnsAsync(new CoinTransaction { Amount = 42 });
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.SetupGet(unit => unit.CoinTransactions).Returns(transactions.Object);

        var service = new QuizRewardService(
            unitOfWork.Object,
            Mock.Of<ILogger<QuizRewardService>>());
        var earned = await service.AwardAsync(userId, new QuizRewardContext
        {
            Quiz = new Quiz { QuizType = QuizType.Lesson, LessonId = Guid.NewGuid() },
            Attempt = attempt
        });

        earned.Should().Be(42);
        transactions.Verify(
            repository => repository.AddAsync(It.IsAny<CoinTransaction>()),
            Times.Never);
    }
}
