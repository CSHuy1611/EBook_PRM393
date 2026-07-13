namespace MathIBook.Application.DTOs;

public class BadgeDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconUrl { get; set; } = string.Empty;
    public string ConditionType { get; set; } = string.Empty;
    public string? ConditionValue { get; set; }
}

public class BadgeCreateDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconUrl { get; set; } = string.Empty;
    public string ConditionType { get; set; } = string.Empty;
    public string? ConditionValue { get; set; }
}

public class BadgeUpdateDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconUrl { get; set; } = string.Empty;
    public string ConditionType { get; set; } = string.Empty;
    public string? ConditionValue { get; set; }
}
