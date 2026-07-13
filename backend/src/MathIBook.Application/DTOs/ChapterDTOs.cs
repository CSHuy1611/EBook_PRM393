namespace MathIBook.Application.DTOs;

public class ChapterDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int OrderIndex { get; set; }
    public double CompletionPercentage { get; set; }
    public int LessonCount { get; set; }
}

public class ChapterCreateDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int OrderIndex { get; set; }
}

public class ChapterUpdateDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int OrderIndex { get; set; }
}
