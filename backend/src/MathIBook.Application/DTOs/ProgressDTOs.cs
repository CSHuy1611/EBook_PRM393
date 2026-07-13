namespace MathIBook.Application.DTOs;

public class ProgressSyncDto
{
    public List<ProgressItemDto> Items { get; set; } = new();
}

public class ProgressItemDto
{
    public Guid LessonId { get; set; }
    public bool IsCompleted { get; set; }
    public int BestScore { get; set; }
    public DateTime ClientUpdatedAt { get; set; }
}

public class ProgressResultDto
{
    public Guid LessonId { get; set; }
    public bool IsCompleted { get; set; }
    public int BestScore { get; set; }
    public DateTime UpdatedAt { get; set; }
}
