using System.Security.Claims;
using MathIBook.Application.DTOs;
using MathIBook.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MathIBook.Api.Controllers;

[Route("api/notifications")]
[ApiController]
[Authorize(Roles = "Student")]
public class NotificationsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public NotificationsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<List<NotificationDto>>> GetAll()
    {
        var userId = CurrentUserId();
        return Ok(await _unitOfWork.Notifications.Query()
            .Where(notification => notification.UserId == userId && notification.Type == "admin_message")
            .OrderByDescending(notification => notification.CreatedAt)
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

    [HttpGet("unread-count")]
    public async Task<ActionResult<UnreadCountDto>> GetUnreadCount()
    {
        var userId = CurrentUserId();
        return Ok(new UnreadCountDto
        {
            Count = await _unitOfWork.Notifications.Query()
                .CountAsync(notification =>
                    notification.UserId == userId && !notification.IsRead && notification.Type == "admin_message")
        });
    }

    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        var userId = CurrentUserId();
        var notification = await _unitOfWork.Notifications.Query()
            .FirstOrDefaultAsync(item => item.Id == id && item.UserId == userId);
        if (notification is null)
        {
            return NotFound();
        }

        notification.IsRead = true;
        _unitOfWork.Notifications.Update(notification);
        await _unitOfWork.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = CurrentUserId();
        var notifications = await _unitOfWork.Notifications.Query()
            .Where(item => item.UserId == userId && !item.IsRead)
            .ToListAsync();
        foreach (var notification in notifications)
        {
            notification.IsRead = true;
            _unitOfWork.Notifications.Update(notification);
        }

        await _unitOfWork.SaveChangesAsync();
        return NoContent();
    }

    private Guid CurrentUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
