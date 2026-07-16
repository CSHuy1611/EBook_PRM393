using MathIBook.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MathIBook.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<CurriculumTopic> CurriculumTopics => Set<CurriculumTopic>();
    public DbSet<Chapter> Chapters => Set<Chapter>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<QuizQuestion> QuizQuestions => Set<QuizQuestion>();
    public DbSet<Progress> Progresses => Set<Progress>();
    public DbSet<ChapterProgress> ChapterProgresses => Set<ChapterProgress>();
    public DbSet<QuizAttempt> QuizAttempts => Set<QuizAttempt>();
    public DbSet<QuizAttemptAnswer> QuizAttemptAnswers => Set<QuizAttemptAnswer>();
    public DbSet<RewardPolicy> RewardPolicies => Set<RewardPolicy>();
    public DbSet<Badge> Badges => Set<Badge>();
    public DbSet<BadgeRule> BadgeRules => Set<BadgeRule>();
    public DbSet<UserBadge> UserBadges => Set<UserBadge>();
    public DbSet<CoinTransaction> CoinTransactions => Set<CoinTransaction>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<ContentAuditLog> ContentAuditLogs => Set<ContentAuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureUsers(modelBuilder);
        ConfigureCurriculum(modelBuilder);
        ConfigureQuizzesAndProgress(modelBuilder);
        ConfigureRewardsAndBadges(modelBuilder);
        ConfigureSupportingData(modelBuilder);
    }

    private static void ConfigureUsers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.HasIndex(u => new { u.Role, u.IsActive, u.Coins });
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Users_Role", "\"Role\" IN ('Student', 'Admin')");
                t.HasCheckConstraint("CK_Users_Coins", "\"Coins\" >= 0");
            });
        });

        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.HasIndex(rt => rt.Token).IsUnique();
            e.HasOne(rt => rt.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureCurriculum(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CurriculumTopic>(e =>
        {
            e.HasIndex(t => t.Code).IsUnique();
            e.HasIndex(t => new { t.Strand, t.OrderIndex });
            e.ToTable(t => t.HasCheckConstraint("CK_CurriculumTopics_Grade", "\"Grade\" = 8"));
        });

        modelBuilder.Entity<Chapter>(e =>
        {
            e.HasIndex(c => c.OrderIndex);
            e.HasIndex(c => new { c.IsPublished, c.IsDeleted, c.OrderIndex });
            e.HasOne(c => c.CurriculumTopic)
                .WithMany(t => t.Chapters)
                .HasForeignKey(c => c.CurriculumTopicId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Lesson>(e =>
        {
            e.HasOne(l => l.Chapter)
                .WithMany(c => c.Lessons)
                .HasForeignKey(l => l.ChapterId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(l => l.CurriculumTopic)
                .WithMany(t => t.Lessons)
                .HasForeignKey(l => l.CurriculumTopicId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(l => new { l.ChapterId, l.OrderIndex });
            e.HasIndex(l => new { l.IsPublished, l.IsDeleted });
            e.ToTable(t => t.HasCheckConstraint("CK_Lessons_ContentVersion", "\"ContentVersion\" > 0"));
        });

        modelBuilder.Entity<Question>(e =>
        {
            e.HasOne(q => q.Lesson)
                .WithMany(l => l.Questions)
                .HasForeignKey(q => q.LessonId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(q => q.Chapter)
                .WithMany(c => c.Questions)
                .HasForeignKey(q => q.ChapterId)
                .OnDelete(DeleteBehavior.Restrict);
            e.Property(q => q.Options).HasColumnType("jsonb");
            e.HasIndex(q => new { q.LessonId, q.OrderIndex });
            e.HasIndex(q => new { q.ChapterId, q.OrderIndex });
            e.ToTable(t =>
            {
                t.HasCheckConstraint(
                    "CK_Questions_SingleScope",
                    "(\"LessonId\" IS NOT NULL AND \"ChapterId\" IS NULL) OR (\"LessonId\" IS NULL AND \"ChapterId\" IS NOT NULL)");
                t.HasCheckConstraint("CK_Questions_CorrectOption", "\"CorrectOption\" BETWEEN 0 AND 3");
            });
        });
    }

    private static void ConfigureQuizzesAndProgress(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RewardPolicy>(e =>
        {
            e.HasIndex(p => new { p.QuizType, p.IsActive, p.EffectiveFrom });
            e.ToTable(t =>
            {
                t.HasCheckConstraint(
                    "CK_RewardPolicies_NonNegative",
                    "\"CoinsPerCorrectAnswer\" >= 0 AND \"FirstPassBonusCoins\" >= 0 AND \"PerfectScoreBonusCoins\" >= 0 AND \"ChapterCompletionBonusCoins\" >= 0 AND (\"DailyCoinLimit\" IS NULL OR \"DailyCoinLimit\" >= 0)");
                t.HasCheckConstraint("CK_RewardPolicies_RetryPercent", "\"RetryRewardPercent\" BETWEEN 0 AND 100");
                t.HasCheckConstraint("CK_RewardPolicies_EffectiveRange", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" > \"EffectiveFrom\"");
            });
        });

        modelBuilder.Entity<Quiz>(e =>
        {
            e.Property(q => q.PassScore).HasPrecision(4, 2);
            e.HasOne(q => q.Lesson)
                .WithMany(l => l.Quizzes)
                .HasForeignKey(q => q.LessonId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(q => q.Chapter)
                .WithMany(c => c.Quizzes)
                .HasForeignKey(q => q.ChapterId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(q => q.RewardPolicy)
                .WithMany(p => p.Quizzes)
                .HasForeignKey(q => q.RewardPolicyId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(q => q.LessonId)
                .IsUnique()
                .HasFilter("\"LessonId\" IS NOT NULL AND \"IsDeleted\" = FALSE");
            e.HasIndex(q => q.ChapterId)
                .IsUnique()
                .HasFilter("\"ChapterId\" IS NOT NULL AND \"IsDeleted\" = FALSE");
            e.HasIndex(q => new { q.QuizType, q.IsPublished, q.IsDeleted });
            e.ToTable(t =>
            {
                t.HasCheckConstraint(
                    "CK_Quizzes_Target",
                    "(\"QuizType\" = 1 AND \"LessonId\" IS NOT NULL AND \"ChapterId\" IS NULL) OR (\"QuizType\" = 2 AND \"ChapterId\" IS NOT NULL AND \"LessonId\" IS NULL)");
                t.HasCheckConstraint("CK_Quizzes_PassScore", "\"PassScore\" BETWEEN 0 AND 10");
                t.HasCheckConstraint("CK_Quizzes_Duration", "\"DurationSeconds\" > 0");
                t.HasCheckConstraint("CK_Quizzes_FirstPassCoins", "\"FirstPassCoins\" >= 0");
            });
        });

        modelBuilder.Entity<QuizQuestion>(e =>
        {
            e.HasIndex(qq => new { qq.QuizId, qq.QuestionId }).IsUnique();
            e.HasIndex(qq => new { qq.QuizId, qq.OrderIndex }).IsUnique();
            e.HasOne(qq => qq.Quiz)
                .WithMany(q => q.QuizQuestions)
                .HasForeignKey(qq => qq.QuizId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(qq => qq.Question)
                .WithMany(q => q.QuizQuestions)
                .HasForeignKey(qq => qq.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);
            e.ToTable(t => t.HasCheckConstraint("CK_QuizQuestions_Weight", "\"Weight\" > 0"));
        });

        modelBuilder.Entity<Progress>(e =>
        {
            e.Property(p => p.BestScore10).HasPrecision(4, 2);
            e.HasIndex(p => new { p.UserId, p.LessonId }).IsUnique();
            e.HasOne(p => p.User)
                .WithMany(u => u.Progresses)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(p => p.Lesson)
                .WithMany(l => l.Progresses)
                .HasForeignKey(p => p.LessonId)
                .OnDelete(DeleteBehavior.Restrict);
            e.ToTable(t => t.HasCheckConstraint("CK_Progresses_BestScore10", "\"BestScore10\" BETWEEN 0 AND 10"));
        });

        modelBuilder.Entity<ChapterProgress>(e =>
        {
            e.Property(p => p.BestScore10).HasPrecision(4, 2);
            e.HasIndex(p => new { p.UserId, p.ChapterId }).IsUnique();
            e.HasOne(p => p.User)
                .WithMany(u => u.ChapterProgresses)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(p => p.Chapter)
                .WithMany(c => c.Progresses)
                .HasForeignKey(p => p.ChapterId)
                .OnDelete(DeleteBehavior.Restrict);
            e.ToTable(t => t.HasCheckConstraint("CK_ChapterProgresses_BestScore10", "\"BestScore10\" BETWEEN 0 AND 10"));
        });

        modelBuilder.Entity<QuizAttempt>(e =>
        {
            e.Property(a => a.Score10).HasPrecision(4, 2);
            e.HasOne(a => a.User)
                .WithMany(u => u.QuizAttempts)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(a => a.Lesson)
                .WithMany(l => l.QuizAttempts)
                .HasForeignKey(a => a.LessonId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(a => a.Quiz)
                .WithMany(q => q.Attempts)
                .HasForeignKey(a => a.QuizId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(a => new { a.UserId, a.CreatedAt });
            e.HasIndex(a => new { a.QuizId, a.UserId, a.CreatedAt });
            e.HasIndex(a => a.ClientAttemptId)
                .IsUnique()
                .HasFilter("\"ClientAttemptId\" IS NOT NULL");
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_QuizAttempts_Target", "\"QuizId\" IS NOT NULL OR \"LessonId\" IS NOT NULL");
                t.HasCheckConstraint("CK_QuizAttempts_Score10", "\"Score10\" BETWEEN 0 AND 10");
                t.HasCheckConstraint("CK_QuizAttempts_CoinsEarned", "\"CoinsEarned\" >= 0");
            });
        });

        modelBuilder.Entity<QuizAttemptAnswer>(e =>
        {
            e.HasIndex(a => new { a.AttemptId, a.QuestionId }).IsUnique();
            e.HasOne(a => a.Attempt)
                .WithMany(qa => qa.Answers)
                .HasForeignKey(a => a.AttemptId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(a => a.Question)
                .WithMany()
                .HasForeignKey(a => a.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);
            e.ToTable(t => t.HasCheckConstraint("CK_QuizAttemptAnswers_SelectedOption", "\"SelectedOption\" BETWEEN -1 AND 3"));
        });
    }

    private static void ConfigureRewardsAndBadges(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Badge>(e =>
        {
            e.Property(b => b.ConditionValue).HasColumnType("jsonb");
            e.HasIndex(b => new { b.IsActive, b.IsDeleted });
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Badges_RuleMatchMode", "\"RuleMatchMode\" IN ('ALL', 'ANY')");
                t.HasCheckConstraint("CK_Badges_RewardCoins", "\"RewardCoins\" >= 0");
            });
        });

        modelBuilder.Entity<BadgeRule>(e =>
        {
            e.Property(r => r.Parameters).HasColumnType("jsonb");
            e.HasIndex(r => new { r.BadgeId, r.OrderIndex }).IsUnique();
            e.HasOne(r => r.Badge)
                .WithMany(b => b.Rules)
                .HasForeignKey(r => r.BadgeId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(r => r.TargetChapter)
                .WithMany(c => c.BadgeRules)
                .HasForeignKey(r => r.TargetChapterId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(r => r.TargetQuiz)
                .WithMany(q => q.BadgeRules)
                .HasForeignKey(r => r.TargetQuizId)
                .OnDelete(DeleteBehavior.Restrict);
            e.ToTable(t => t.HasCheckConstraint("CK_BadgeRules_Threshold", "\"ThresholdValue\" IS NULL OR \"ThresholdValue\" >= 0"));
        });

        modelBuilder.Entity<UserBadge>(e =>
        {
            e.HasIndex(ub => new { ub.UserId, ub.BadgeId }).IsUnique();
            e.HasIndex(ub => new { ub.UserId, ub.EarnedAt });
            e.HasOne(ub => ub.User)
                .WithMany(u => u.UserBadges)
                .HasForeignKey(ub => ub.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(ub => ub.Badge)
                .WithMany(b => b.UserBadges)
                .HasForeignKey(ub => ub.BadgeId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(ub => ub.BadgeRule)
                .WithMany()
                .HasForeignKey(ub => ub.BadgeRuleId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<CoinTransaction>(e =>
        {
            e.HasIndex(ct => new { ct.UserId, ct.CreatedAt });
            e.HasIndex(ct => ct.IdempotencyKey)
                .IsUnique()
                .HasFilter("\"IdempotencyKey\" IS NOT NULL");
            e.HasOne(ct => ct.User)
                .WithMany(u => u.CoinTransactions)
                .HasForeignKey(ct => ct.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(ct => ct.RewardPolicy)
                .WithMany(p => p.CoinTransactions)
                .HasForeignKey(ct => ct.RewardPolicyId)
                .OnDelete(DeleteBehavior.SetNull);
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_CoinTransactions_Amount", "\"Amount\" >= 0");
                t.HasCheckConstraint("CK_CoinTransactions_BalanceAfter", "\"BalanceAfter\" >= 0");
            });
        });
    }

    private static void ConfigureSupportingData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>(e =>
        {
            e.HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt });
            e.HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ContentAuditLog>(e =>
        {
            e.Property(a => a.BeforeData).HasColumnType("jsonb");
            e.Property(a => a.AfterData).HasColumnType("jsonb");
            e.HasIndex(a => new { a.EntityType, a.EntityId, a.CreatedAt });
            e.HasIndex(a => new { a.AdminUserId, a.CreatedAt });
            e.HasOne(a => a.AdminUser)
                .WithMany(u => u.ContentAuditLogs)
                .HasForeignKey(a => a.AdminUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
