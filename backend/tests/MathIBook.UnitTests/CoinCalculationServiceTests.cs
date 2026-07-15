using System.Linq.Expressions;
using FluentAssertions;
using MathIBook.Application.Services;
using MathIBook.Domain.Entities;
using MathIBook.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MathIBook.UnitTests;

public class CoinCalculationServiceTests
{
    [Fact]
    public async Task CalculateQuizCoinsAsync_StoresIdempotencyAndResultingBalance()
    {
        var userId = Guid.NewGuid();
        var attemptId = Guid.NewGuid();
        var user = new User { Id = userId, Coins = 100 };

        var users = new Mock<IRepository<User>>();
        users.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        CoinTransaction? savedTransaction = null;
        var transactions = new Mock<IRepository<CoinTransaction>>();
        transactions.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<CoinTransaction, bool>>>()))
            .ReturnsAsync((CoinTransaction?)null);
        transactions.Setup(r => r.AddAsync(It.IsAny<CoinTransaction>()))
            .Callback<CoinTransaction>(transaction => savedTransaction = transaction)
            .Returns(Task.CompletedTask);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.SetupGet(u => u.Users).Returns(users.Object);
        unitOfWork.SetupGet(u => u.CoinTransactions).Returns(transactions.Object);
        unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var service = new CoinCalculationService(
            unitOfWork.Object,
            Mock.Of<ILogger<CoinCalculationService>>());

        var earned = await service.CalculateQuizCoinsAsync(userId, 2, 3, attemptId);

        earned.Should().Be(20);
        user.Coins.Should().Be(120);
        user.CoinsUpdatedAt.Should().NotBeNull();

        savedTransaction.Should().NotBeNull();
        savedTransaction!.SourceId.Should().Be(attemptId);
        savedTransaction.ClientAttemptId.Should().Be(attemptId);
        savedTransaction.IdempotencyKey.Should().Be($"quiz_reward:{attemptId:N}");
        savedTransaction.BalanceAfter.Should().Be(120);
    }
}
