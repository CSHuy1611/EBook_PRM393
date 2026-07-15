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
        }

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
                CreatedAt = DateTime.UtcNow
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

        _unitOfWork.Notifications.Remove(notification);
        await _unitOfWork.SaveChangesAsync();
        return NoContent();
    }
}
