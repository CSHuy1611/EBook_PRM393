namespace MathIBook.Application.DTOs;

public class ContentValidationResultDto
{
    public bool IsValid => Errors.Count == 0;
    public List<ContentValidationErrorDto> Errors { get; set; } = new();
}

public class ContentValidationErrorDto
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
}
