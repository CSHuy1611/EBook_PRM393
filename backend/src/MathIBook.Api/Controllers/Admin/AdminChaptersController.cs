using MathIBook.Application.DTOs;
using MathIBook.Domain.Entities;
using MathIBook.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MathIBook.Api.Controllers.Admin;

[Route("api/admin/chapters")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminChaptersController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public AdminChaptersController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var chapters = await _unitOfWork.Chapters.Query()
                .OrderBy(c => c.OrderIndex)
                .Select(c => new ChapterDto
                {
                    Id = c.Id,
                    Title = c.Title,
                    Description = c.Description,
                    OrderIndex = c.OrderIndex,
                    LessonCount = c.Lessons.Count,
                    CompletionPercentage = 0
                })
                .ToListAsync();

            return Ok(chapters);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ProblemDetails
            {
                Title = "Error fetching chapters",
                Detail = ex.Message,
                Status = 500
            });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ChapterCreateDto dto)
    {
        try
        {
            var chapter = new Chapter
            {
                Title = dto.Title,
                Description = dto.Description,
                OrderIndex = dto.OrderIndex
            };

            await _unitOfWork.Chapters.AddAsync(chapter);
            await _unitOfWork.SaveChangesAsync();

            var result = new ChapterDto
            {
                Id = chapter.Id,
                Title = chapter.Title,
                Description = chapter.Description,
                OrderIndex = chapter.OrderIndex,
                LessonCount = 0,
                CompletionPercentage = 0
            };

            return CreatedAtAction(nameof(GetAll), new { id = chapter.Id }, result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ProblemDetails
            {
                Title = "Error creating chapter",
                Detail = ex.Message,
                Status = 500
            });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ChapterUpdateDto dto)
    {
        try
        {
            var chapter = await _unitOfWork.Chapters.GetByIdAsync(id);
            if (chapter == null)
                return NotFound(new ProblemDetails { Title = "Chapter not found", Status = 404 });

            chapter.Title = dto.Title;
            chapter.Description = dto.Description;
            chapter.OrderIndex = dto.OrderIndex;

            _unitOfWork.Chapters.Update(chapter);
            await _unitOfWork.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ProblemDetails
            {
                Title = "Error updating chapter",
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
            var chapter = await _unitOfWork.Chapters.GetByIdAsync(id);
            if (chapter == null)
                return NotFound(new ProblemDetails { Title = "Chapter not found", Status = 404 });

            _unitOfWork.Chapters.Remove(chapter);
            await _unitOfWork.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ProblemDetails
            {
                Title = "Error deleting chapter",
                Detail = ex.Message,
                Status = 500
            });
        }
    }
}
