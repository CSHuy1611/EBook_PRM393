using System.Linq.Expressions;
using FluentAssertions;
using MathIBook.Application.DTOs;
using MathIBook.Application.Services;
using MathIBook.Domain.Entities;
using MathIBook.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace MathIBook.UnitTests;

public class AuthServiceTests
{
    [Fact]
    public async Task LoginAsync_RejectsInactiveAccountEvenWithCorrectPassword()
    {
        var password = "Student1";
        var users = new Mock<IRepository<User>>();
        users.Setup(repository => repository.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(new User
            {
                Email = "student@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = "Student",
                IsActive = false
            });
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.SetupGet(unit => unit.Users).Returns(users.Object);
        var service = new AuthService(
            unitOfWork.Object,
            Mock.Of<IConfiguration>(),
            Mock.Of<ILogger<AuthService>>());

        var action = () => service.LoginAsync(new LoginRequest
        {
            Email = " STUDENT@example.com ",
            Password = password
        });

        await action.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*khóa*");
    }
}
