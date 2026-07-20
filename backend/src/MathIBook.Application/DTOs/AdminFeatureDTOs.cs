using MathIBook.Domain.Enums;

namespace MathIBook.Application.DTOs;

public class AdminQuizDto
{
    public Guid Id { get; set; }
    public QuizType QuizType { get; set; }
    public Guid? LessonId { get; set; }
    public Guid? ChapterId { get; set; }
    public Guid? RewardPolicyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal PassScore { get; set; }
    public int DurationSeconds { get; set; }
    public int FirstPassCoins { get; set; }
    public bool IsPublished { get; set; }
    public int QuestionCount { get; set; }
    public DateTime? PublishedAt { get; set; }
    public List<QuestionDto> Questions { get; set; } = new();
}

public class AdminQuizGenerateDto
{
    public Guid? LessonId { get; set; }
    public Guid? ChapterId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int QuestionCount { get; set; } = 10;
    public int DurationSeconds { get; set; } = 1200;
    public decimal PassScore { get; set; } = 5.0m;
}

public class AdminQuizUpsertDto
{
    public QuizType QuizType { get; set; }
    public Guid? LessonId { get; set; }
    public Guid? ChapterId { get; set; }
    public Guid? RewardPolicyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal PassScore { get; set; } = 5;
    public int DurationSeconds { get; set; } = 900;
    public int FirstPassCoins { get; set; }
    public int? QuestionCount { get; set; }
}

public class TogglePublishDto
{
    public bool IsPublished { get; set; }
}

public class QuizQuestionUpsertDto
{
    public Guid? QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public int CorrectOption { get; set; }
    public string? Explanation { get; set; }
    public int OrderIndex { get; set; }
    public int Weight { get; set; } = 1;
}

public class QuizQuestionOrderDto
{
    public Guid QuestionId { get; set; }
    public int OrderIndex { get; set; }
}

public class RewardPolicyUpsertDto
{
    public string Name { get; set; } = string.Empty;
    public QuizType QuizType { get; set; }
    public int CoinsPerCorrectAnswer { get; set; }
    public int FirstPassBonusCoins { get; set; }
    public int PerfectScoreBonusCoins { get; set; }
    public int ChapterCompletionBonusCoins { get; set; }
    public int RetryRewardPercent { get; set; }
    public int? DailyCoinLimit { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
}

public class BadgeRuleUpsertDto
{
    public string RuleType { get; set; } = string.Empty;
    public Guid? TargetChapterId { get; set; }
    public Guid? TargetQuizId { get; set; }
    public int? ThresholdValue { get; set; }
    public int OrderIndex { get; set; }
    public string? Parameters { get; set; }
}

public class BadgeAdminUpsertDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconUrl { get; set; } = string.Empty;
    public string RuleMatchMode { get; set; } = "ALL";
    public int RewardCoins { get; set; }
    public bool IsActive { get; set; } = true;
    public List<BadgeRuleUpsertDto> Rules { get; set; } = new();
}

public class CurriculumTopicUpsertDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public CurriculumStrand Strand { get; set; }
    public int OrderIndex { get; set; }
    public bool IsActive { get; set; } = true;
}

public class AccountStatusDto
{
    public bool IsActive { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class LearningReportFilterDto
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public Guid? ChapterId { get; set; }
    public Guid? LessonId { get; set; }
}

public class MostMissedQuestionDto
{
    public Guid QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public int AnswerCount { get; set; }
    public int WrongCount { get; set; }
    public double WrongRate { get; set; }
}

public class StudentPerformanceDto
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Coins { get; set; }
    public int Badges { get; set; }
    public int Attempts { get; set; }
    public double AverageScore { get; set; }
}

public class LearningReportDto
{
    public int TotalAttempts { get; set; }
    public double PassRate { get; set; }
    public double AverageScore { get; set; }
    public int RetryCount { get; set; }
    public List<MostMissedQuestionDto> MostMissedQuestions { get; set; } = new();
    public List<ChapterReportDto> LowCompletionChapters { get; set; } = new();
    public List<StudentPerformanceDto> TopStudents { get; set; } = new();
    public List<DailyRewardDto> RewardsByDay { get; set; } = new();
}

public class DailyRewardDto
{
    public DateTime Date { get; set; }
    public int CoinsAwarded { get; set; }
    public int BadgesAwarded { get; set; }
}
