using MathIBook.Domain.Entities;
using MathIBook.Domain.Interfaces;

namespace MathIBook.Infrastructure.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IRepository<User>? _users;
    private IRepository<RefreshToken>? _refreshTokens;
    private IRepository<Chapter>? _chapters;
    private IRepository<Lesson>? _lessons;
    private IRepository<Question>? _questions;
    private IRepository<Progress>? _progresses;
    private IRepository<QuizAttempt>? _quizAttempts;
    private IRepository<QuizAttemptAnswer>? _quizAttemptAnswers;
    private IRepository<Badge>? _badges;
    private IRepository<UserBadge>? _userBadges;
    private IRepository<CoinTransaction>? _coinTransactions;
    private IRepository<Notification>? _notifications;
    private bool _disposed;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public IRepository<User> Users => _users ??= new Repository<User>(_context);
    public IRepository<RefreshToken> RefreshTokens => _refreshTokens ??= new Repository<RefreshToken>(_context);
    public IRepository<Chapter> Chapters => _chapters ??= new Repository<Chapter>(_context);
    public IRepository<Lesson> Lessons => _lessons ??= new Repository<Lesson>(_context);
    public IRepository<Question> Questions => _questions ??= new Repository<Question>(_context);
    public IRepository<Progress> Progresses => _progresses ??= new Repository<Progress>(_context);
    public IRepository<QuizAttempt> QuizAttempts => _quizAttempts ??= new Repository<QuizAttempt>(_context);
    public IRepository<QuizAttemptAnswer> QuizAttemptAnswers => _quizAttemptAnswers ??= new Repository<QuizAttemptAnswer>(_context);
    public IRepository<Badge> Badges => _badges ??= new Repository<Badge>(_context);
    public IRepository<UserBadge> UserBadges => _userBadges ??= new Repository<UserBadge>(_context);
    public IRepository<CoinTransaction> CoinTransactions => _coinTransactions ??= new Repository<CoinTransaction>(_context);
    public IRepository<Notification> Notifications => _notifications ??= new Repository<Notification>(_context);

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await _context.SaveChangesAsync(ct);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _context.Dispose();
            }
            _disposed = true;
        }
    }
}
