using System.ComponentModel.DataAnnotations;
using MathIBook.Domain.Enums;

namespace MathIBook.Domain.Entities;

public class CurriculumTopic
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public CurriculumStrand Strand { get; set; }

    public int Grade { get; set; } = 8;

    public int OrderIndex { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Chapter> Chapters { get; set; } = new List<Chapter>();
    public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
}
