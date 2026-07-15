using MathIBook.Domain.Entities;
using MathIBook.Domain.Interfaces;

namespace MathIBook.Infrastructure.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IRepository<User>? _users;
    private IRepository<RefreshToken>? _refreshTokens;
    private IRepository<CurriculumTopic>? _curriculumTopics;
    private IRepository<Chapter>? _chapters;
    private IRepository<Lesson>? _lessons;
    private IRepository<Question>? _questions;
    private IRepository<Quiz>? _quizzes;
    private IRepository<QuizQuestion>? _quizQuestions;
    private IRepository<Progress>? _progresses;
    private IRepository<ChapterProgress>? _chapterProgresses;
    private IRepository<QuizAttempt>? _quizAttempts;
    private IRepository<QuizAttemptAnswer>? _quizAttemptAnswers;
    private IRepository<RewardPolicy>? _rewardPolicies;
    private IRepository<Badge>? _badges;
    private IRepository<BadgeRule>? _badgeRules;
    private IRepository<UserBadge>? _userBadges;
    private IRepository<CoinTransaction>? _coinTransactions;
    private IRepository<Notification>? _notifications;
    private IRepository<ContentAuditLog>? _contentAuditLogs;
    private bool _disposed;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public IRepository<User> Users => _users ??= new Repository<User>(_context);
    public IRepository<RefreshToken> RefreshTokens => _refreshTokens ??= new Repository<RefreshToken>(_context);
    public IRepository<CurriculumTopic> CurriculumTopics => _curriculumTopics ??= new Repository<CurriculumTopic>(_context);
    public IRepository<Chapter> Chapters => _chapters ??= new Repository<Chapter>(_context);
    public IRepository<Lesson> Lessons => _lessons ??= new Repository<Lesson>(_context);
    public IRepository<Question> Questions => _questions ??= new Repository<Question>(_context);
    public IRepository<Quiz> Quizzes => _quizzes ??= new Repository<Quiz>(_context);
    public IRepository<QuizQuestion> QuizQuestions => _quizQuestions ??= new Repository<QuizQuestion>(_context);
    public IRepository<Progress> Progresses => _progresses ??= new Repository<Progress>(_context);
    public IRepository<ChapterProgress> ChapterProgresses => _chapterProgresses ??= new Repository<ChapterProgress>(_context);
    public IRepository<QuizAttempt> QuizAttempts => _quizAttempts ??= new Repository<QuizAttempt>(_context);
    public IRepository<QuizAttemptAnswer> QuizAttemptAnswers => _quizAttemptAnswers ??= new Repository<QuizAttemptAnswer>(_context);
    public IRepository<RewardPolicy> RewardPolicies => _rewardPolicies ??= new Repository<RewardPolicy>(_context);
    public IRepository<Badge> Badges => _badges ??= new Repository<Badge>(_context);
    public IRepository<BadgeRule> BadgeRules => _badgeRules ??= new Repository<BadgeRule>(_context);
    public IRepository<UserBadge> UserBadges => _userBadges ??= new Repository<UserBadge>(_context);
    public IRepository<CoinTransaction> CoinTransactions => _coinTransactions ??= new Repository<CoinTransaction>(_context);
    public IRepository<Notification> Notifications => _notifications ??= new Repository<Notification>(_context);
    public IRepository<ContentAuditLog> ContentAuditLogs => _contentAuditLogs ??= new Repository<ContentAuditLog>(_context);

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return _context.SaveChangesAsync(ct);
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
