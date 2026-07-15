using System.Security.Claims;
using System.Text.Json;
using MathIBook.Application.DTOs;
using MathIBook.Application.Interfaces;
using MathIBook.Domain.Entities;
using MathIBook.Domain.Enums;
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
    private readonly IContentValidationService _validationService;

    public AdminLessonsController(
        IUnitOfWork unitOfWork,
        IContentValidationService validationService)
    {
        _unitOfWork = unitOfWork;
        _validationService = validationService;
    }

    [HttpGet("chapter/{chapterId}")]
    public async Task<ActionResult<List<LessonDto>>> GetByChapter(Guid chapterId)
    {
        var lessons = await LessonQuery()
            .Where(lesson => lesson.ChapterId == chapterId && !lesson.IsDeleted)
            .OrderBy(lesson => lesson.OrderIndex)
            .ToListAsync();
        return Ok(lessons.Select(Map).ToList());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<LessonDto>> GetById(Guid id)
    {
        var lesson = await LessonQuery()
            .FirstOrDefaultAsync(item => item.Id == id && !item.IsDeleted);
        return lesson is null ? NotFound() : Ok(Map(lesson));
    }

    [HttpPost]
    public async Task<ActionResult<LessonDto>> Create([FromBody] LessonCreateDto dto)
    {
        var relationError = await ValidateRelationsAsync(dto.ChapterId, dto.CurriculumTopicId);
        if (relationError is not null)
        {
            return BadRequest(relationError);
        }

        var lesson = new Lesson
        {
            ChapterId = dto.ChapterId,
            CurriculumTopicId = dto.CurriculumTopicId,
            Title = dto.Title.Trim(),
            ContentBody = dto.ContentBody,
            SimulationType = dto.SimulationType,
            OrderIndex = dto.OrderIndex,
            ContentVersion = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _unitOfWork.Lessons.AddAsync(lesson);
        await AuditAsync(lesson.Id, "Create", null, lesson);
        await _unitOfWork.SaveChangesAsync();
        return Ok(Map(lesson));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] LessonUpdateDto dto)
    {
        var lesson = await _unitOfWork.Lessons.GetByIdAsync(id);
        if (lesson is null || lesson.IsDeleted)
        {
            return NotFound();
        }

        var relationError = await ValidateRelationsAsync(
            lesson.ChapterId,
            dto.CurriculumTopicId);
        if (relationError is not null)
        {
            return BadRequest(relationError);
        }

        var before = Snapshot(lesson);
        var contentChanged = lesson.ContentBody != dto.ContentBody
            || lesson.Title != dto.Title
            || lesson.SimulationType != dto.SimulationType;
        lesson.CurriculumTopicId = dto.CurriculumTopicId;
        lesson.Title = dto.Title.Trim();
        lesson.ContentBody = dto.ContentBody;
        lesson.SimulationType = dto.SimulationType;
        lesson.OrderIndex = dto.OrderIndex;
        lesson.ContentVersion += contentChanged ? 1 : 0;
        lesson.UpdatedAt = DateTime.UtcNow;
        if (contentChanged)
        {
            lesson.IsPublished = false;
            lesson.PublishedAt = null;
        }

        _unitOfWork.Lessons.Update(lesson);
        await AuditAsync(lesson.Id, "Update", before, lesson);
        await _unitOfWork.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id}/validation")]
    public async Task<ActionResult<ContentValidationResultDto>> Validate(Guid id)
    {
        var lesson = await _unitOfWork.Lessons.Query()
            .Include(item => item.CurriculumTopic)
            .FirstOrDefaultAsync(item => item.Id == id && !item.IsDeleted);
        if (lesson is null)
        {
            return NotFound();
        }

        return Ok(_validationService.ValidateLesson(
            lesson,
            lesson.CurriculumTopic is { Grade: 8, IsActive: true }));
    }

    [HttpPatch("{id}/publish")]
    public async Task<IActionResult> TogglePublish(Guid id)
    {
        var lesson = await _unitOfWork.Lessons.Query()
            .Include(item => item.CurriculumTopic)
            .Include(item => item.Chapter)
            .FirstOrDefaultAsync(item => item.Id == id && !item.IsDeleted);
        if (lesson is null)
        {
            return NotFound();
        }

        var before = Snapshot(lesson);
        if (lesson.IsPublished)
        {
            lesson.IsPublished = false;
            lesson.PublishedAt = null;
        }
        else
        {
            var validation = _validationService.ValidateLesson(
                lesson,
                lesson.CurriculumTopic is { Grade: 8, IsActive: true });
            if (!validation.IsValid)
            {
                return BadRequest(validation);
            }

            if (!lesson.Chapter.IsPublished || lesson.Chapter.IsDeleted)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Chương phải được xuất bản trước bài học.",
                    Status = 400
                });
            }

            lesson.IsPublished = true;
            lesson.PublishedAt = DateTime.UtcNow;
            await NotifyStudentsAsync(lesson);
        }

        lesson.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Lessons.Update(lesson);
        await AuditAsync(
            lesson.Id,
            lesson.IsPublished ? "Publish" : "Unpublish",
            before,
            lesson);
        await _unitOfWork.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("chapter/{chapterId}/reorder")]
    public async Task<IActionResult> Reorder(
        Guid chapterId,
        [FromBody] Dictionary<Guid, int> order)
    {
        if (order.Values.Distinct().Count() != order.Count)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Thứ tự bài học không được trùng.",
                Status = 400
            });
        }

        var lessons = await _unitOfWork.Lessons.Query()
            .Where(lesson =>
                lesson.ChapterId == chapterId
                && order.Keys.Contains(lesson.Id)
                && !lesson.IsDeleted)
            .ToListAsync();
        if (lessons.Count != order.Count)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Danh sách chứa bài học không tồn tại.",
                Status = 400
            });
        }

        foreach (var lesson in lessons)
        {
            lesson.OrderIndex = order[lesson.Id];
            lesson.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Lessons.Update(lesson);
        }

        await _unitOfWork.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var lesson = await _unitOfWork.Lessons.GetByIdAsync(id);
        if (lesson is null || lesson.IsDeleted)
        {
            return NotFound();
        }

        var before = Snapshot(lesson);
        lesson.IsDeleted = true;
        lesson.IsPublished = false;
        lesson.PublishedAt = null;
        lesson.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Lessons.Update(lesson);
        await AuditAsync(lesson.Id, "SoftDelete", before, lesson);
        await _unitOfWork.SaveChangesAsync();
        return NoContent();
    }

    private IQueryable<Lesson> LessonQuery() =>
        _unitOfWork.Lessons.Query()
            .Include(lesson => lesson.Questions.Where(question => !question.IsDeleted))
            .Include(lesson => lesson.Quizzes.Where(quiz => !quiz.IsDeleted));

    private async Task<ProblemDetails?> ValidateRelationsAsync(
        Guid chapterId,
        Guid? topicId)
    {
        if (!await _unitOfWork.Chapters.Query().AnyAsync(
            chapter => chapter.Id == chapterId && !chapter.IsDeleted))
        {
            return new ProblemDetails { Title = "Không tìm thấy chương.", Status = 400 };
        }

        if (!topicId.HasValue
            || !await _unitOfWork.CurriculumTopics.Query().AnyAsync(topic =>
                topic.Id == topicId && topic.Grade == 8 && topic.IsActive))
        {
            return new ProblemDetails
            {
                Title = "Bài học phải thuộc taxonomy Toán lớp 8 đang hoạt động.",
                Status = 400
            };
        }

        return null;
    }

    private async Task NotifyStudentsAsync(Lesson lesson)
    {
        var studentIds = await _unitOfWork.Users.Query()
            .Where(user => user.Role == "Student" && user.IsActive)
            .Select(user => user.Id)
            .ToListAsync();
        foreach (var studentId in studentIds)
        {
            await _unitOfWork.Notifications.AddAsync(new Notification
            {
                UserId = studentId,
                Title = "Bài học mới",
                Body = $"Bài “{lesson.Title}” đã được xuất bản.",
                Link = $"/lessons/{lesson.Id}",
                Type = "new_lesson",
                RelatedEntityId = lesson.Id,
                CreatedAt = DateTime.UtcNow
            });
        }
    }

    private LessonDto Map(Lesson lesson) => new()
    {
        Id = lesson.Id,
        ChapterId = lesson.ChapterId,
        CurriculumTopicId = lesson.CurriculumTopicId,
        Title = lesson.Title,
        ContentBody = lesson.ContentBody,
        SimulationType = lesson.SimulationType,
        OrderIndex = lesson.OrderIndex,
        ContentVersion = lesson.ContentVersion,
        IsPublished = lesson.IsPublished,
        QuizId = lesson.Quizzes.FirstOrDefault()?.Id,
        Questions = lesson.Questions.OrderBy(question => question.OrderIndex)
            .Select(question => new QuestionDto
            {
                Id = question.Id,
                LessonId = question.LessonId,
                ChapterId = question.ChapterId,
                QuestionText = question.QuestionText,
                Options = JsonSerializer.Deserialize<List<string>>(question.Options) ?? new(),
                CorrectOption = question.CorrectOption,
                Explanation = question.Explanation,
                OrderIndex = question.OrderIndex
            }).ToList()
    };

    private async Task AuditAsync(Guid id, string action, object? before, object? after)
    {
        await _unitOfWork.ContentAuditLogs.AddAsync(new ContentAuditLog
        {
            AdminUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
            EntityType = "Lesson",
            EntityId = id,
            Action = action,
            BeforeData = before is null ? null : JsonSerializer.Serialize(before),
            AfterData = after is null ? null : JsonSerializer.Serialize(after),
            CreatedAt = DateTime.UtcNow
        });
    }

    private static object Snapshot(Lesson lesson) => new
    {
        lesson.Title,
        lesson.ContentBody,
        lesson.SimulationType,
        lesson.OrderIndex,
        lesson.CurriculumTopicId,
        lesson.ContentVersion,
        lesson.IsPublished,
        lesson.IsDeleted
    };
}
