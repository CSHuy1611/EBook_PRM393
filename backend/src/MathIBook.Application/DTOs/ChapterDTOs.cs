namespace MathIBook.Application.DTOs;

public class ChapterDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int OrderIndex { get; set; }
    public Guid? CurriculumTopicId { get; set; }
    public bool IsPublished { get; set; }
    public bool IsUnlocked { get; set; } = true;
    public double CompletionPercentage { get; set; }
    public int LessonCount { get; set; }
    public int PassedLessonCount { get; set; }
    public string ChapterQuizStatus { get; set; } = "Unavailable";
    public Guid? ChapterQuizId { get; set; }
    public Guid? RelatedBadgeId { get; set; }
    public string? RelatedBadgeTitle { get; set; }
}

public class ChapterCreateDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int OrderIndex { get; set; }
    public Guid? CurriculumTopicId { get; set; }
}

public class ChapterUpdateDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int OrderIndex { get; set; }
    public Guid? CurriculumTopicId { get; set; }
}
