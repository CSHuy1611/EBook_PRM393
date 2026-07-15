using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MathIBook.Domain.Entities;

public class ContentAuditLog
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid AdminUserId { get; set; }

    [Required, MaxLength(50)]
    public string EntityType { get; set; } = string.Empty;

    public Guid EntityId { get; set; }

    [Required, MaxLength(50)]
    public string Action { get; set; } = string.Empty;

    public string? BeforeData { get; set; }

    public string? AfterData { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(AdminUserId))]
    public User AdminUser { get; set; } = null!;
}
