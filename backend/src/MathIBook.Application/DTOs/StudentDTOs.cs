namespace MathIBook.Application.DTOs;

public class LeaderboardEntryDto
{
    public int Rank { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public int Coins { get; set; }
    public int BadgeCount { get; set; }
    public bool IsCurrentUser { get; set; }
}

public class LeaderboardDto
{
    public List<LeaderboardEntryDto> Top100 { get; set; } = new();
    public LeaderboardEntryDto? CurrentUser { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CoinHistoryDto
{
    public int TotalCoins { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public List<CoinTransactionDto> Items { get; set; } = new();
}

public class BadgeCollectionItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconUrl { get; set; } = string.Empty;
    public string Status { get; set; } = "Locked";
    public DateTime? EarnedAt { get; set; }
    public double ProgressPercentage { get; set; }
    public string Requirement { get; set; } = string.Empty;
    public int CurrentValue { get; set; }
    public int TargetValue { get; set; }
    public List<BadgeRuleProgressDto> Rules { get; set; } = new();
}

public class BadgeRuleProgressDto
{
    public string Requirement { get; set; } = string.Empty;
    public int CurrentValue { get; set; }
    public int TargetValue { get; set; }
    public double Percentage { get; set; }
}

public class BadgeCollectionDto
{
    public int EarnedCount { get; set; }
    public int TotalCount { get; set; }
    public List<BadgeCollectionItemDto> Items { get; set; } = new();
}

public class StudentProfileDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public int Coins { get; set; }
    public int BadgeCount { get; set; }
    public int? Rank { get; set; }
    public int CompletedLessons { get; set; }
    public int CompletedChapters { get; set; }
    public double AverageScore { get; set; }
    public double BestScore { get; set; }
}

public class UpdateProfileDto
{
    public string Name { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}

public class ChangePasswordDto
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
