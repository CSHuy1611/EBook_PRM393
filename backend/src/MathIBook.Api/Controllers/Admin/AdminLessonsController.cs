using MathIBook.Application.DTOs;
using MathIBook.Domain.Entities;
using MathIBook.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MathIBook.Api.Controllers.Admin;

[Route("api/admin/lessons")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminLessonsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public AdminLessonsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet("chapter/{chapterId}")]
    public async Task<IActionResult> GetByChapter(Guid chapterId)
    {
        try
        {
            var lessons = await _unitOfWork.Lessons.Query()
                .Where(l => l.ChapterId == chapterId)
                .OrderBy(l => l.OrderIndex)
                .Include(l => l.Questions)
                .ToListAsync();

            var result = lessons.Select(l => new LessonDto
            {
                Id = l.Id,
                ChapterId = l.ChapterId,
                Title = l.Title,
                ContentBody = l.ContentBody,
                SimulationType = l.SimulationType,
                OrderIndex = l.OrderIndex,
                IsPublished = l.IsPublished,
                IsCompleted = false,
                Questions = l.Questions.OrderBy(q => q.OrderIndex).Select(q => new QuestionDto
                {
                    Id = q.Id,
                    LessonId = q.LessonId,
                    QuestionText = q.QuestionText,
                    Options = System.Text.Json.JsonSerializer.Deserialize<List<string>>(q.Options) ?? new(),
                    CorrectOption = q.CorrectOption,
                    Explanation = q.Explanation,
                    OrderIndex = q.OrderIndex
                }).ToList()
            }).ToList();

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ProblemDetails
            {
                Title = "Error fetching lessons",
                Detail = ex.Message,
                Status = 500
            });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var lesson = await _unitOfWork.Lessons.Query()
                .Include(l => l.Questions.OrderBy(q => q.OrderIndex))
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lesson == null)
                return NotFound(new ProblemDetails { Title = "Lesson not found", Status = 404 });

            var result = new LessonDto
            {
                Id = lesson.Id,
                ChapterId = lesson.ChapterId,
                Title = lesson.Title,
                ContentBody = lesson.ContentBody,
                SimulationType = lesson.SimulationType,
                OrderIndex = lesson.OrderIndex,
                IsPublished = lesson.IsPublished,
                Questions = lesson.Questions.Select(q => new QuestionDto
                {
                    Id = q.Id,
                    LessonId = q.LessonId,
                    QuestionText = q.QuestionText,
                    Options = System.Text.Json.JsonSerializer.Deserialize<List<string>>(q.Options) ?? new(),
                    CorrectOption = q.CorrectOption,
                    Explanation = q.Explanation,
                    OrderIndex = q.OrderIndex
                }).ToList()
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ProblemDetails
            {
                Title = "Error fetching lesson",
                Detail = ex.Message,
                Status = 500
            });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] LessonCreateDto dto)
    {
        try
        {
            var chapter = await _unitOfWork.Chapters.GetByIdAsync(dto.ChapterId);
            if (chapter == null)
                return NotFound(new ProblemDetails { Title = "Chapter not found", Status = 404 });

            var lesson = new Lesson
            {
                ChapterId = dto.ChapterId,
                Title = dto.Title,
                ContentBody = dto.ContentBody,
                SimulationType = dto.SimulationType,
                OrderIndex = dto.OrderIndex
            };

            await _unitOfWork.Lessons.AddAsync(lesson);
            await _unitOfWork.SaveChangesAsync();

            try
            {
                var students = await _unitOfWork.Users.Query()
                    .Where(u => u.Role == "Student")
                    .ToListAsync();

                foreach (var student in students)
                {
                    var notification = new Notification
                    {
                        UserId = student.Id,
                        Title = "New Lesson Available",
                        Body = $"A new lesson \"{lesson.Title}\" has been added.",
                        Link = $"/lessons/{lesson.Id}"
                    };

                    await _unitOfWork.Notifications.AddAsync(notification);
                }

                await _unitOfWork.SaveChangesAsync();
            }
            catch
            {
                // Notification failure should not block lesson creation
            }

            return CreatedAtAction(nameof(GetByChapter), new { chapterId = lesson.ChapterId }, new LessonDto
            {
                Id = lesson.Id,
                ChapterId = lesson.ChapterId,
                Title = lesson.Title,
                ContentBody = lesson.ContentBody,
                SimulationType = lesson.SimulationType,
                OrderIndex = lesson.OrderIndex,
                IsPublished = lesson.IsPublished
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ProblemDetails
            {
                Title = "Error creating lesson",
                Detail = ex.Message,
                Status = 500
            });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] LessonUpdateDto dto)
    {
        try
        {
            var lesson = await _unitOfWork.Lessons.GetByIdAsync(id);
            if (lesson == null)
                return NotFound(new ProblemDetails { Title = "Lesson not found", Status = 404 });

            lesson.Title = dto.Title;
            lesson.ContentBody = dto.ContentBody;
            lesson.SimulationType = dto.SimulationType;
            lesson.OrderIndex = dto.OrderIndex;

            _unitOfWork.Lessons.Update(lesson);
            await _unitOfWork.SaveChangesAsync();

            try
            {
                var students = await _unitOfWork.Users.Query()
                    .Where(u => u.Role == "Student")
                    .ToListAsync();

                foreach (var student in students)
                {
                    var notification = new Notification
                    {
                        UserId = student.Id,
                        Title = "Lesson Updated",
                        Body = $"The lesson \"{lesson.Title}\" has been updated.",
                        Link = $"/lessons/{lesson.Id}"
                    };

                    await _unitOfWork.Notifications.AddAsync(notification);
                }

                await _unitOfWork.SaveChangesAsync();
            }
            catch
            {
                // Notification failure should not block lesson update
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ProblemDetails
            {
                Title = "Error updating lesson",
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
            var lesson = await _unitOfWork.Lessons.GetByIdAsync(id);
            if (lesson == null)
                return NotFound(new ProblemDetails { Title = "Lesson not found", Status = 404 });

            _unitOfWork.Lessons.Remove(lesson);
            await _unitOfWork.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ProblemDetails
            {
                Title = "Error deleting lesson",
                Detail = ex.Message,
                Status = 500
            });
        }
    }

    [HttpPatch("{id}/publish")]
    public async Task<IActionResult> TogglePublish(Guid id)
    {
        try
        {
            var lesson = await _unitOfWork.Lessons.GetByIdAsync(id);
            if (lesson == null)
                return NotFound(new ProblemDetails { Title = "Lesson not found", Status = 404 });

            lesson.IsPublished = !lesson.IsPublished;
            _unitOfWork.Lessons.Update(lesson);
            await _unitOfWork.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ProblemDetails
            {
                Title = "Error toggling publish status",
                Detail = ex.Message,
                Status = 500
            });
        }
    }
}
