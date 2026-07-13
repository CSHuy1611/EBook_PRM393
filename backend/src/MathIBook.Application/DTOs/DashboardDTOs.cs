namespace MathIBook.Application.DTOs;

public class DashboardDto
{
    public double OverallCompletionPercentage { get; set; }
    public int TotalCoins { get; set; }
    public double AverageScore { get; set; }
    public List<ChapterProgressDto> ChapterProgress { get; set; } = new();
    public List<BadgeEarnedDto> Badges { get; set; } = new();
    public List<RecentActivityDto> RecentActivities { get; set; } = new();
}

public class ChapterProgressDto
{
    public Guid ChapterId { get; set; }
    public string ChapterTitle { get; set; } = string.Empty;
    public int CompletedLessons { get; set; }
    public int TotalLessons { get; set; }
    public double CompletionPercentage { get; set; }
}

public class RecentActivityDto
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
