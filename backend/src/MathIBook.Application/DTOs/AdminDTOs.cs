namespace MathIBook.Application.DTOs;

public class AdminUserDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public bool IsActive { get; set; }
    public int Coins { get; set; }
    public int BadgeCount { get; set; }
    public int? Rank { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public int TotalQuizAttempts { get; set; }
    public double AverageScore { get; set; }
    public int CompletedLessons { get; set; }
    public int CompletedChapters { get; set; }
}

public class UserHistoryDto
{
    public List<QuizAttemptHistoryDto> QuizAttempts { get; set; } = new();
    public List<BadgeEarnedDto> Badges { get; set; } = new();
    public List<CoinTransactionDto> CoinTransactions { get; set; } = new();
    public List<ProgressHistoryDto> LessonProgress { get; set; } = new();
    public List<ProgressHistoryDto> ChapterProgress { get; set; } = new();
}

public class ProgressHistoryDto
{
    public Guid TargetId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public double BestScore { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class QuizAttemptHistoryDto
{
    public Guid Id { get; set; }
    public Guid? QuizId { get; set; }
    public string LessonTitle { get; set; } = string.Empty;
    public string ChapterTitle { get; set; } = string.Empty;
    public double Score { get; set; }
    public bool IsPassed { get; set; }
    public int TotalQuestions { get; set; }
    public int CoinsEarned { get; set; }
    public int DurationSeconds { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CoinTransactionDto
{
    public int Amount { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public Guid? SourceId { get; set; }
    public int BalanceAfter { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class ReportOverviewDto
{
    public int TotalUsers { get; set; }
    public int TotalQuizAttempts { get; set; }
    public double OverallAverageScore { get; set; }
    public int TotalCoinsAwarded { get; set; }
    public int TotalBadgesAwarded { get; set; }
    public List<ChapterReportDto> ChapterReports { get; set; } = new();
    public List<DailyActivityDto> DailyActivities { get; set; } = new();
    public List<TopStudentDto> TopStudents { get; set; } = new();
    public List<FailedQuestionDto> MostFailedQuestions { get; set; } = new();
}

public class TopStudentDto
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Coins { get; set; }
    public int BadgeCount { get; set; }
}

public class FailedQuestionDto
{
    public Guid QuestionId { get; set; }
    public string Content { get; set; } = string.Empty;
    public int TotalAttempts { get; set; }
    public int FailedAttempts { get; set; }
    public double FailureRate { get; set; }
}

public class ChapterReportDto
{
    public Guid ChapterId { get; set; }
    public string ChapterTitle { get; set; } = string.Empty;
    public int TotalAttempts { get; set; }
    public double AverageScore { get; set; }
    public double CompletionRate { get; set; }
}

public class DailyActivityDto
{
    public DateTime Date { get; set; }
    public int QuizCount { get; set; }
    public int NewUsers { get; set; }
}
