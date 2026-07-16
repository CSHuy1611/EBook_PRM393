using System.Security.Claims;
using System.Text.Json;
using MathIBook.Application.DTOs;
using MathIBook.Domain.Entities;
using MathIBook.Domain.Enums;
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
    public async Task<ActionResult<List<ChapterDto>>> GetAll()
    {
        var chapters = await _unitOfWork.Chapters.Query()
            .Where(chapter => !chapter.IsDeleted)
            .OrderBy(chapter => chapter.OrderIndex)
            .Include(chapter => chapter.Lessons.Where(lesson => !lesson.IsDeleted))
            .Include(chapter => chapter.Quizzes.Where(quiz =>
                quiz.QuizType == QuizType.Chapter && !quiz.IsDeleted))
            .ToListAsync();

        return Ok(chapters.Select(chapter => new ChapterDto
        {
            Id = chapter.Id,
            Title = chapter.Title,
            Description = chapter.Description,
            OrderIndex = chapter.OrderIndex,
            CurriculumTopicId = chapter.CurriculumTopicId,
            IsPublished = chapter.IsPublished,
            LessonCount = chapter.Lessons.Count,
            ChapterQuizId = chapter.Quizzes.FirstOrDefault()?.Id,
            ChapterQuizStatus = chapter.Quizzes.FirstOrDefault() is { } quiz
                ? quiz.IsPublished ? "Published" : "Draft"
                : "Unavailable"
        }).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<ChapterDto>> Create([FromBody] ChapterCreateDto dto)
    {
        var validation = await ValidateAsync(dto.Title, dto.CurriculumTopicId);
        if (validation is not null)
        {
            return BadRequest(validation);
        }

        var chapter = new Chapter
        {
            Title = dto.Title.Trim(),
            Description = dto.Description?.Trim(),
            OrderIndex = dto.OrderIndex,
            CurriculumTopicId = dto.CurriculumTopicId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _unitOfWork.Chapters.AddAsync(chapter);
        await AuditAsync(chapter.Id, "Create", null, chapter);
        await _unitOfWork.SaveChangesAsync();
        return Ok(new ChapterDto
        {
            Id = chapter.Id,
            Title = chapter.Title,
            Description = chapter.Description,
            OrderIndex = chapter.OrderIndex,
            CurriculumTopicId = chapter.CurriculumTopicId
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ChapterUpdateDto dto)
    {
        var chapter = await _unitOfWork.Chapters.GetByIdAsync(id);
        if (chapter is null || chapter.IsDeleted)
        {
            return NotFound();
        }

        var validation = await ValidateAsync(dto.Title, dto.CurriculumTopicId);
        if (validation is not null)
        {
            return BadRequest(validation);
        }

        var before = Snapshot(chapter);
        chapter.Title = dto.Title.Trim();
        chapter.Description = dto.Description?.Trim();
        chapter.OrderIndex = dto.OrderIndex;
        chapter.CurriculumTopicId = dto.CurriculumTopicId;
        chapter.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Chapters.Update(chapter);
        await AuditAsync(chapter.Id, "Update", before, chapter);
        await _unitOfWork.SaveChangesAsync();
        return NoContent();
    }

    [HttpPatch("{id}/publish")]
    public async Task<IActionResult> TogglePublish(Guid id)
    {
        var chapter = await _unitOfWork.Chapters.GetByIdAsync(id);
        if (chapter is null || chapter.IsDeleted)
        {
            return NotFound();
        }

        var before = Snapshot(chapter);
        if (chapter.IsPublished)
        {
            chapter.IsPublished = false;
            chapter.PublishedAt = null;
        }
        else
        {
            var validation = await ValidateAsync(chapter.Title, chapter.CurriculumTopicId);
            if (validation is not null || string.IsNullOrWhiteSpace(chapter.Description))
            {
                return BadRequest(validation ?? new ProblemDetails
                {
                    Title = "Chương phải có mô tả trước khi xuất bản.",
                    Status = 400
                });
            }

            chapter.IsPublished = true;
            chapter.PublishedAt = DateTime.UtcNow;
            await NotifyStudentsAsync(
                "Chương học mới",
                $"Chương “{chapter.Title}” đã được xuất bản.",
                $"/chapters/{chapter.Id}",
                "new_chapter",
                chapter.Id);
        }

        chapter.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Chapters.Update(chapter);
        await AuditAsync(
            chapter.Id,
            chapter.IsPublished ? "Publish" : "Unpublish",
            before,
            chapter);
        await _unitOfWork.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("reorder")]
    public async Task<IActionResult> Reorder([FromBody] Dictionary<Guid, int> order)
    {
        if (order.Values.Distinct().Count() != order.Count)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Thứ tự chương không được trùng.",
                Status = 400
            });
        }

        var chapters = await _unitOfWork.Chapters.Query()
            .Where(chapter => order.Keys.Contains(chapter.Id) && !chapter.IsDeleted)
            .ToListAsync();
        if (chapters.Count != order.Count)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Danh sách chứa chương không tồn tại.",
                Status = 400
            });
        }

        foreach (var chapter in chapters)
        {
            chapter.OrderIndex = order[chapter.Id];
            chapter.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Chapters.Update(chapter);
        }

        await _unitOfWork.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var chapter = await _unitOfWork.Chapters.Query().Include(c => c.Lessons).FirstOrDefaultAsync(c => c.Id == id);
        if (chapter is null || chapter.IsDeleted)
        {
            return NotFound();
        }

        if (chapter.Lessons.Any(l => !l.IsDeleted))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Không thể xóa chương đang chứa bài học.",
                Status = 400
            });
        }

        var before = Snapshot(chapter);
        chapter.IsDeleted = true;
        chapter.IsPublished = false;
        chapter.PublishedAt = null;
        chapter.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Chapters.Update(chapter);
        await AuditAsync(chapter.Id, "SoftDelete", before, chapter);
        await _unitOfWork.SaveChangesAsync();
        return NoContent();
    }

    private async Task<ProblemDetails?> ValidateAsync(string title, Guid? topicId)
    {
        if (string.IsNullOrWhiteSpace(title) || !topicId.HasValue)
        {
            return new ProblemDetails
            {
                Title = "Tiêu đề và taxonomy Toán lớp 8 là bắt buộc.",
                Status = 400
            };
        }

        var validTopic = await _unitOfWork.CurriculumTopics.Query().AnyAsync(
            topic => topic.Id == topicId && topic.Grade == 8 && topic.IsActive);
        return validTopic
            ? null
            : new ProblemDetails
            {
                Title = "Taxonomy phải thuộc Toán lớp 8 và đang hoạt động.",
                Status = 400
            };
    }

    private async Task NotifyStudentsAsync(
        string title,
        string body,
        string link,
        string type,
        Guid relatedId)
    {
        var students = await _unitOfWork.Users.Query()
            .Where(user => user.Role == "Student" && user.IsActive)
            .Select(user => user.Id)
            .ToListAsync();
        foreach (var studentId in students)
        {
            await _unitOfWork.Notifications.AddAsync(new Notification
            {
                UserId = studentId,
                Title = title,
                Body = body,
                Link = link,
                Type = type,
                RelatedEntityId = relatedId,
                CreatedAt = DateTime.UtcNow
            });
        }
    }

    private async Task AuditAsync(Guid id, string action, object? before, object? after)
    {
        await _unitOfWork.ContentAuditLogs.AddAsync(new ContentAuditLog
        {
            AdminUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
            EntityType = "Chapter",
            EntityId = id,
            Action = action,
            BeforeData = before is null ? null : JsonSerializer.Serialize(before),
            AfterData = after is null ? null : JsonSerializer.Serialize(after),
            CreatedAt = DateTime.UtcNow
        });
    }

    private static object Snapshot(Chapter chapter) => new
    {
        chapter.Title,
        chapter.Description,
        chapter.OrderIndex,
        chapter.CurriculumTopicId,
        chapter.IsPublished,
        chapter.IsDeleted
    };
}
