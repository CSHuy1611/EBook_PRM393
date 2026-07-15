using MathIBook.Application.DTOs;
using MathIBook.Domain.Entities;
using MathIBook.Domain.Enums;
using MathIBook.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace MathIBook.Api.Controllers;

[Route("api/lessons")]
[ApiController]
[Authorize(Roles = "Student")]
public class LessonsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public LessonsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<LessonDto>> GetById(Guid id)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var lesson = await _unitOfWork.Lessons.Query()
            .Where(item => item.Id == id && item.IsPublished && !item.IsDeleted)
            .Include(item => item.Questions.Where(question => !question.IsDeleted))
            .Include(item => item.Quizzes.Where(quiz =>
                quiz.QuizType == QuizType.Lesson && quiz.IsPublished && !quiz.IsDeleted))
            .FirstOrDefaultAsync();
        if (lesson is null)
        {
            return NotFound(new ProblemDetails { Title = "Không tìm thấy bài học.", Status = 404 });
        }

        var progress = await _unitOfWork.Progresses.Query()
            .FirstOrDefaultAsync(item => item.UserId == userId && item.LessonId == id);

        return Ok(new LessonDto
        {
            Id = lesson.Id,
            ChapterId = lesson.ChapterId,
            CurriculumTopicId = lesson.CurriculumTopicId,
            Title = lesson.Title,
            ContentBody = lesson.ContentBody,
            SimulationType = lesson.SimulationType,
            OrderIndex = lesson.OrderIndex,
            ContentVersion = lesson.ContentVersion,
            IsPublished = true,
            IsCompleted = progress?.Status == LearningStatus.Passed,
            Status = (progress?.Status ?? LearningStatus.NotStarted).ToString(),
            ContentViewed = progress?.ContentViewed ?? false,
            BestScore = progress is null ? null : (double)progress.BestScore10,
            QuizId = lesson.Quizzes.FirstOrDefault()?.Id,
            Questions = lesson.Questions.OrderBy(question => question.OrderIndex)
                .Select(question => new QuestionDto
                {
                    Id = question.Id,
                    LessonId = question.LessonId,
                    QuestionText = question.QuestionText,
                    Options = JsonSerializer.Deserialize<List<string>>(question.Options) ?? new(),
                    CorrectOption = null,
                    Explanation = null,
                    OrderIndex = question.OrderIndex
                }).ToList()
        });
    }

    [HttpPost("{id}/viewed")]
    public async Task<IActionResult> MarkContentViewed(Guid id)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var lessonExists = await _unitOfWork.Lessons.Query()
            .AnyAsync(lesson => lesson.Id == id && lesson.IsPublished && !lesson.IsDeleted);
        if (!lessonExists)
        {
            return NotFound(new ProblemDetails { Title = "Không tìm thấy bài học.", Status = 404 });
        }

        var now = DateTime.UtcNow;
        var progress = await _unitOfWork.Progresses.Query()
            .FirstOrDefaultAsync(item => item.UserId == userId && item.LessonId == id);
        if (progress is null)
        {
            progress = new Progress
            {
                UserId = userId,
                LessonId = id,
                ClientUpdatedAt = now
            };
            progress.MarkContentViewed(now);
            await _unitOfWork.Progresses.AddAsync(progress);
        }
        else
        {
            progress.ClientUpdatedAt = now;
            progress.MarkContentViewed(now);
            _unitOfWork.Progresses.Update(progress);
        }

        await _unitOfWork.SaveChangesAsync();
        return Ok(new
        {
            lessonId = id,
            status = progress.Status.ToString(),
            contentViewed = progress.ContentViewed,
            lastViewedAt = progress.LastViewedAt
        });
    }
}
