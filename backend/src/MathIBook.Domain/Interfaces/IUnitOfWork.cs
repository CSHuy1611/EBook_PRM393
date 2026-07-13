using MathIBook.Domain.Entities;

namespace MathIBook.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IRepository<User> Users { get; }
    IRepository<RefreshToken> RefreshTokens { get; }
    IRepository<Chapter> Chapters { get; }
    IRepository<Lesson> Lessons { get; }
    IRepository<Question> Questions { get; }
    IRepository<Progress> Progresses { get; }
    IRepository<QuizAttempt> QuizAttempts { get; }
    IRepository<QuizAttemptAnswer> QuizAttemptAnswers { get; }
    IRepository<Badge> Badges { get; }
    IRepository<UserBadge> UserBadges { get; }
    IRepository<CoinTransaction> CoinTransactions { get; }
    IRepository<Notification> Notifications { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
