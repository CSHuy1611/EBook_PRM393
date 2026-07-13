using System.ComponentModel.DataAnnotations;

namespace MathIBook.Domain.Entities;

public class Chapter
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int OrderIndex { get; set; }

    public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
}
