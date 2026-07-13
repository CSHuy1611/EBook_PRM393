using MathIBook.Application.DTOs;
using MathIBook.Domain.Entities;
using MathIBook.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MathIBook.Api.Controllers.Admin;

[Route("api/admin/badges")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminBadgesController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public AdminBadgesController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var badges = await _unitOfWork.Badges.Query()
                .OrderBy(b => b.Title)
                .Select(b => new BadgeDto
                {
                    Id = b.Id,
                    Title = b.Title,
                    Description = b.Description,
                    IconUrl = b.IconUrl,
                    ConditionType = b.ConditionType,
                    ConditionValue = b.ConditionValue
                })
                .ToListAsync();

            return Ok(badges);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ProblemDetails
            {
                Title = "Error fetching badges",
                Detail = ex.Message,
                Status = 500
            });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] BadgeCreateDto dto)
    {
        try
        {
            var badge = new Badge
            {
                Title = dto.Title,
                Description = dto.Description,
                IconUrl = dto.IconUrl,
                ConditionType = dto.ConditionType,
                ConditionValue = dto.ConditionValue
            };

            await _unitOfWork.Badges.AddAsync(badge);
            await _unitOfWork.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAll), new { id = badge.Id }, new BadgeDto
            {
                Id = badge.Id,
                Title = badge.Title,
                Description = badge.Description,
                IconUrl = badge.IconUrl,
                ConditionType = badge.ConditionType,
                ConditionValue = badge.ConditionValue
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ProblemDetails
            {
                Title = "Error creating badge",
                Detail = ex.Message,
                Status = 500
            });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] BadgeUpdateDto dto)
    {
        try
        {
            var badge = await _unitOfWork.Badges.GetByIdAsync(id);
            if (badge == null)
                return NotFound(new ProblemDetails { Title = "Badge not found", Status = 404 });

            badge.Title = dto.Title;
            badge.Description = dto.Description;
            badge.IconUrl = dto.IconUrl;
            badge.ConditionType = dto.ConditionType;
            badge.ConditionValue = dto.ConditionValue;

            _unitOfWork.Badges.Update(badge);
            await _unitOfWork.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ProblemDetails
            {
                Title = "Error updating badge",
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
            var badge = await _unitOfWork.Badges.GetByIdAsync(id);
            if (badge == null)
                return NotFound(new ProblemDetails { Title = "Badge not found", Status = 404 });

            _unitOfWork.Badges.Remove(badge);
            await _unitOfWork.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ProblemDetails
            {
                Title = "Error deleting badge",
                Detail = ex.Message,
                Status = 500
            });
        }
    }
}
