namespace MathIBook.Application.DTOs;

public class QuizSubmitDto
{
    public Guid LessonId { get; set; }
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
    public double Score { get; set; }
    public int TotalQuestions { get; set; }
    public int CoinsEarned { get; set; }
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
