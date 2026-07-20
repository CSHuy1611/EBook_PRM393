namespace MathIBook.Application.DTOs;

public class LessonDto
{
    public Guid Id { get; set; }
    public Guid ChapterId { get; set; }
    public Guid? CurriculumTopicId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ContentBody { get; set; } = string.Empty;
    public string? SimulationType { get; set; }
    public int OrderIndex { get; set; }
    public int ContentVersion { get; set; }
    public bool IsPublished { get; set; }
    public bool IsCompleted { get; set; }
    public string Status { get; set; } = "NotStarted";
    public bool ContentViewed { get; set; }
    public double? BestScore { get; set; }
    public Guid? QuizId { get; set; }
    public int? QuizDurationSeconds { get; set; }
    public List<QuestionDto> Questions { get; set; } = new();
}

public class LessonCreateDto
{
    public Guid ChapterId { get; set; }
    public Guid? CurriculumTopicId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ContentBody { get; set; } = string.Empty;
    public string? SimulationType { get; set; }
    public int OrderIndex { get; set; }
}

public class LessonUpdateDto
{
    public Guid? CurriculumTopicId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ContentBody { get; set; } = string.Empty;
    public string? SimulationType { get; set; }
    public int OrderIndex { get; set; }
}
