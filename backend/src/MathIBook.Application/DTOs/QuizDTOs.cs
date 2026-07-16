namespace MathIBook.Application.DTOs;

public class QuizSubmitDto
{
    public Guid? QuizId { get; set; }
    public Guid? LessonId { get; set; }
    public Guid? ClientAttemptId { get; set; }
    public int DurationSeconds { get; set; }
    public List<AnswerDto> Answers { get; set; } = new();
    public DateTime ClientCreatedAt { get; set; }
}

public class AnswerDto
{
    public Guid QuestionId { get; set; }
    public int SelectedOption { get; set; }
}

public class QuizResultDto
{
    public Guid Id { get; set; }
    public Guid QuizId { get; set; }
    public Guid ClientAttemptId { get; set; }
    public double Score { get; set; }
    public bool IsPassed { get; set; }
    public double PassScore { get; set; }
    public int CorrectCount { get; set; }
    public int TotalQuestions { get; set; }
    public int AttemptNumber { get; set; }
    public int CoinsEarned { get; set; }
    public bool IsDuplicate { get; set; }
    public List<CorrectAnswerDto> CorrectAnswers { get; set; } = new();
    public List<BadgeEarnedDto> NewBadges { get; set; } = new();
}

public class CorrectAnswerDto
{
    public Guid QuestionId { get; set; }
    public int SelectedOption { get; set; }
    public int CorrectOption { get; set; }
    public bool IsCorrect { get; set; }
    public string? Explanation { get; set; }
}

public class BadgeEarnedDto
{
    public Guid BadgeId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconUrl { get; set; } = string.Empty;
}

public class QuizForStudentDto
{
    public Guid Id { get; set; }
    public string QuizType { get; set; } = string.Empty;
    public Guid? LessonId { get; set; }
    public Guid? ChapterId { get; set; }
    public string Title { get; set; } = string.Empty;
    public double PassScore { get; set; }
    public int DurationSeconds { get; set; }
    public bool IsUnlocked { get; set; } = true;
    public string Status { get; set; } = "NotStarted";
    public double? BestScore { get; set; }
    public int AttemptCount { get; set; }
    public List<MissingLessonDto> MissingLessons { get; set; } = new();
    public List<StudentQuestionDto> Questions { get; set; } = new();
}

public class StudentQuestionDto
{
    public Guid Id { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public int OrderIndex { get; set; }
}

public class MissingLessonDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
}

public class OfflineSyncDto
{
    public List<QuizSubmitDto> Attempts { get; set; } = new();
    public ProgressSyncDto Progress { get; set; } = new();
}

public class OfflineSyncResultDto
{
    public List<QuizResultDto> Attempts { get; set; } = new();
    public List<ProgressResultDto> Progress { get; set; } = new();
}
