namespace MathIBook.Application.DTOs;

public class AdminUserDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int Coins { get; set; }
    public DateTime CreatedAt { get; set; }
    public int TotalQuizAttempts { get; set; }
    public double AverageScore { get; set; }
}

public class UserHistoryDto
{
    public List<QuizAttemptHistoryDto> QuizAttempts { get; set; } = new();
    public List<BadgeEarnedDto> Badges { get; set; } = new();
    public List<CoinTransactionDto> CoinTransactions { get; set; } = new();
}

public class QuizAttemptHistoryDto
{
    public Guid Id { get; set; }
    public string LessonTitle { get; set; } = string.Empty;
    public string ChapterTitle { get; set; } = string.Empty;
    public int Score { get; set; }
    public int TotalQuestions { get; set; }
    public int DurationSeconds { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CoinTransactionDto
{
    public int Amount { get; set; }
    public string SourceType { get; set; } = string.Empty;
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
