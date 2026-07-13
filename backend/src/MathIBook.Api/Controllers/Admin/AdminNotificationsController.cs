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
    public async Task<IActionResult> GetAll([FromQuery] Guid? userId)
    {
        try
        {
            var query = _unitOfWork.Notifications.Query();

            if (userId.HasValue)
                query = query.Where(n => n.UserId == userId.Value);

            var notifications = await query
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Body = n.Body,
                    Link = n.Link,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();

            return Ok(notifications);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ProblemDetails
            {
                Title = "Error fetching notifications",
                Detail = ex.Message,
                Status = 500
            });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateNotificationDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
                return BadRequest(new ProblemDetails
                {
                    Title = "Validation error",
                    Detail = "Title is required",
                    Status = 400
                });

            if (dto.UserId.HasValue)
            {
                var user = await _unitOfWork.Users.GetByIdAsync(dto.UserId.Value);
                if (user == null)
                    return NotFound(new ProblemDetails { Title = "User not found", Status = 404 });

                var notification = new Notification
                {
                    UserId = dto.UserId.Value,
                    Title = dto.Title,
                    Body = dto.Body,
                    Link = dto.Link
                };

                await _unitOfWork.Notifications.AddAsync(notification);
            }
            else
            {
                var students = await _unitOfWork.Users.Query()
                    .Where(u => u.Role == "Student")
                    .ToListAsync();

                foreach (var student in students)
                {
                    var notification = new Notification
                    {
                        UserId = student.Id,
                        Title = dto.Title,
                        Body = dto.Body,
                        Link = dto.Link
                    };

                    await _unitOfWork.Notifications.AddAsync(notification);
                }
            }

            await _unitOfWork.SaveChangesAsync();

            return Ok(new { message = "Notification(s) created successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ProblemDetails
            {
                Title = "Error creating notification",
                Detail = ex.Message,
                Status = 500
            });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var notification = await _unitOfWork.Notifications.GetByIdAsync(id);
            if (notification == null)
                return NotFound(new ProblemDetails { Title = "Notification not found", Status = 404 });

            _unitOfWork.Notifications.Remove(notification);
            await _unitOfWork.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ProblemDetails
            {
                Title = "Error deleting notification",
                Detail = ex.Message,
                Status = 500
            });
        }
    }
}
