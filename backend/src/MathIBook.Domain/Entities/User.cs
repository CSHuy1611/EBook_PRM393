using System.ComponentModel.DataAnnotations;

namespace MathIBook.Domain.Entities;

public class User
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Role { get; set; } = "Student";

    public int Coins { get; set; }

    [MaxLength(500)]
    public string? AvatarUrl { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? LastLoginAt { get; set; }

    public DateTime? CoinsUpdatedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<Progress> Progresses { get; set; } = new List<Progress>();
    public ICollection<ChapterProgress> ChapterProgresses { get; set; } = new List<ChapterProgress>();
    public ICollection<QuizAttempt> QuizAttempts { get; set; } = new List<QuizAttempt>();
    public ICollection<UserBadge> UserBadges { get; set; } = new List<UserBadge>();
    public ICollection<CoinTransaction> CoinTransactions { get; set; } = new List<CoinTransaction>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<ContentAuditLog> ContentAuditLogs { get; set; } = new List<ContentAuditLog>();
}
