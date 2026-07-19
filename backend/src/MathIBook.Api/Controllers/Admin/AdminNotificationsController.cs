using MathIBook.Application.DTOs;
using MathIBook.Domain.Entities;
using MathIBook.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MathIBook.Api.Controllers.Admin;

[Route("api/admin/notifications")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminNotificationsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public AdminNotificationsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<List<NotificationDto>>> GetAll([FromQuery] Guid? userId)
    {
        var query = _unitOfWork.Notifications.Query();
        if (userId.HasValue)
        {
            query = query.Where(notification => notification.UserId == userId);
            
            return Ok(await query.OrderByDescending(notification => notification.CreatedAt)
                .Select(notification => new NotificationDto
                {
                    Id = notification.Id,
                    Title = notification.Title,
                    Body = notification.Body,
                    Link = notification.Link,
                    Type = notification.Type,
                    RelatedEntityId = notification.RelatedEntityId,
                    IsRead = notification.IsRead,
                    CreatedAt = notification.CreatedAt
                })
                .ToListAsync());
        }

        var distinctGroups = await query
            .Select(n => new { n.Title, n.Body, n.CreatedAt })
            .Distinct()
            .OrderByDescending(g => g.CreatedAt)
            .Take(50)
            .ToListAsync();

        var result = new List<NotificationDto>();
        foreach(var g in distinctGroups)
        {
            var first = await query
                .Where(n => n.Title == g.Title && n.Body == g.Body && n.CreatedAt == g.CreatedAt)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Body = n.Body,
                    Link = n.Link,
                    Type = n.Type,
                    RelatedEntityId = n.RelatedEntityId,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (first != null)
            {
                result.Add(first);
            }
        }

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateNotificationDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title)
            || dto.Title.Length > 200
            || dto.Body.Length > 1000)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Tiêu đề hoặc nội dung thông báo không hợp lệ.",
                Status = 400
            });
        }

        List<Guid> recipients;
        if (dto.UserId.HasValue)
        {
            var exists = await _unitOfWork.Users.Query().AnyAsync(user =>
                user.Id == dto.UserId && user.Role == "Student");
            if (!exists)
            {
                return NotFound(new ProblemDetails { Title = "Không tìm thấy học sinh.", Status = 404 });
            }

            recipients = [dto.UserId.Value];
        }
        else
        {
            recipients = await _unitOfWork.Users.Query()
                .Where(user => user.Role == "Student" && user.IsActive)
                .Select(user => user.Id)
                .ToListAsync();
        }

        var now = DateTime.UtcNow;
        foreach (var recipient in recipients)
        {
            await _unitOfWork.Notifications.AddAsync(new Notification
            {
                UserId = recipient,
                Title = dto.Title.Trim(),
                Body = dto.Body.Trim(),
                Link = dto.Link,
                Type = string.IsNullOrWhiteSpace(dto.Type) ? "admin_message" : dto.Type,
                RelatedEntityId = dto.RelatedEntityId,
                CreatedAt = now
            });
        }

        await _unitOfWork.SaveChangesAsync();
        return Ok(new { recipientCount = recipients.Count });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var notification = await _unitOfWork.Notifications.GetByIdAsync(id);
        if (notification is null)
        {
            return NotFound();
        }

        var relatedNotifications = await _unitOfWork.Notifications.Query()
            .Where(n => n.Title == notification.Title 
                     && n.Body == notification.Body 
                     && n.CreatedAt == notification.CreatedAt)
            .ToListAsync();

        if (relatedNotifications.Any())
        {
            foreach(var n in relatedNotifications)
            {
                _unitOfWork.Notifications.Remove(n);
            }
        }
        else
        {
            _unitOfWork.Notifications.Remove(notification);
        }

        await _unitOfWork.SaveChangesAsync();
        return NoContent();
    }
}
