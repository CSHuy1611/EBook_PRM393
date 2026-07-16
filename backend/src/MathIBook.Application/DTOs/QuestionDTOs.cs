namespace MathIBook.Application.DTOs;

public class QuestionDto
{
    public Guid Id { get; set; }
    public Guid? LessonId { get; set; }
    public Guid? ChapterId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public int? CorrectOption { get; set; }
    public string? Explanation { get; set; }
    public int OrderIndex { get; set; }
}

public class QuestionCreateDto
{
    public Guid LessonId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public int CorrectOption { get; set; }
    public string? Explanation { get; set; }
    public int OrderIndex { get; set; }
}

public class QuestionUpdateDto
{
    public string QuestionText { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public int CorrectOption { get; set; }
    public string? Explanation { get; set; }
    public int OrderIndex { get; set; }
}

public class AutoGenerateQuestionsDto
{
    public Guid ChapterId { get; set; }
    public Guid LessonId { get; set; }
    public int Count { get; set; }
}
