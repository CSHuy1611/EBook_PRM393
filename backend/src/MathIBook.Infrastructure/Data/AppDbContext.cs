using MathIBook.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MathIBook.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Chapter> Chapters => Set<Chapter>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<Progress> Progresses => Set<Progress>();
    public DbSet<QuizAttempt> QuizAttempts => Set<QuizAttempt>();
    public DbSet<QuizAttemptAnswer> QuizAttemptAnswers => Set<QuizAttemptAnswer>();
    public DbSet<Badge> Badges => Set<Badge>();
    public DbSet<UserBadge> UserBadges => Set<UserBadge>();
    public DbSet<CoinTransaction> CoinTransactions => Set<CoinTransaction>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.HasIndex(rt => rt.Token).IsUnique();
            e.HasOne(rt => rt.User).WithMany(u => u.RefreshTokens).HasForeignKey(rt => rt.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Chapter>(e =>
        {
            e.HasIndex(c => c.OrderIndex);
        });

        modelBuilder.Entity<Lesson>(e =>
        {
            e.HasOne(l => l.Chapter).WithMany(c => c.Lessons).HasForeignKey(l => l.ChapterId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(l => l.OrderIndex);
        });

        modelBuilder.Entity<Question>(e =>
        {
            e.HasOne(q => q.Lesson).WithMany(l => l.Questions).HasForeignKey(q => q.LessonId).OnDelete(DeleteBehavior.Cascade);
            e.Property(q => q.Options).HasColumnType("jsonb");
        });

        modelBuilder.Entity<Progress>(e =>
        {
            e.HasIndex(p => new { p.UserId, p.LessonId }).IsUnique();
            e.HasOne(p => p.User).WithMany(u => u.Progresses).HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(p => p.Lesson).WithMany(l => l.Progresses).HasForeignKey(p => p.LessonId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<QuizAttempt>(e =>
        {
            e.HasOne(qa => qa.User).WithMany(u => u.QuizAttempts).HasForeignKey(qa => qa.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(qa => qa.Lesson).WithMany(l => l.QuizAttempts).HasForeignKey(qa => qa.LessonId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<QuizAttemptAnswer>(e =>
        {
            e.HasOne(qaa => qaa.Attempt).WithMany(qa => qa.Answers).HasForeignKey(qaa => qaa.AttemptId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(qaa => qaa.Question).WithMany().HasForeignKey(qaa => qaa.QuestionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Badge>(e =>
        {
            e.Property(b => b.ConditionValue).HasColumnType("jsonb");
        });

        modelBuilder.Entity<UserBadge>(e =>
        {
            e.HasIndex(ub => new { ub.UserId, ub.BadgeId }).IsUnique();
            e.HasOne(ub => ub.User).WithMany(u => u.UserBadges).HasForeignKey(ub => ub.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(ub => ub.Badge).WithMany(b => b.UserBadges).HasForeignKey(ub => ub.BadgeId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CoinTransaction>(e =>
        {
            e.HasOne(ct => ct.User).WithMany(u => u.CoinTransactions).HasForeignKey(ct => ct.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Notification>(e =>
        {
            e.HasIndex(n => new { n.UserId, n.IsRead });
            e.HasOne(n => n.User).WithMany(u => u.Notifications).HasForeignKey(n => n.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
