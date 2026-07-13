namespace MathIBook.Application.DTOs;

public class NotificationDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? Link { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UnreadCountDto
{
    public int Count { get; set; }
}

public class CreateNotificationDto
{
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? Link { get; set; }
    public Guid? UserId { get; set; }
}
