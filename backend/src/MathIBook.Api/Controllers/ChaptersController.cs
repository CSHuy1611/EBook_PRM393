using MathIBook.Application.DTOs;
using MathIBook.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MathIBook.Api.Controllers;

[Route("api/chapters")]
[ApiController]
[Authorize(Roles = "Student")]
public class ChaptersController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public ChaptersController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var chapters = await _unitOfWork.Chapters.Query()
                .OrderBy(c => c.OrderIndex)
                .Include(c => c.Lessons)
                .ToListAsync();

            var progressList = await _unitOfWork.Progresses.Query()
                .Where(p => p.UserId == userId)
                .ToListAsync();

            var result = chapters.Select(c =>
            {
                var totalLessons = c.Lessons.Count(l => l.IsPublished);
                var completedLessons = c.Lessons
                    .Count(l => l.IsPublished && progressList.Any(p => p.LessonId == l.Id && p.IsCompleted));

                return new ChapterDto
                {
                    Id = c.Id,
                    Title = c.Title,
                    Description = c.Description,
                    OrderIndex = c.OrderIndex,
                    LessonCount = totalLessons,
                    CompletionPercentage = totalLessons > 0
                        ? Math.Round((double)completedLessons / totalLessons * 100, 2)
                        : 0
                };
            }).ToList();

            return Ok(result);
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

    [HttpGet("{id}/lessons")]
    public async Task<IActionResult> GetLessons(Guid id)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var chapter = await _unitOfWork.Chapters.GetByIdAsync(id);

            if (chapter == null)
                return NotFound(new ProblemDetails { Title = "Chapter not found", Status = 404 });

            var lessons = await _unitOfWork.Lessons.Query()
                .Where(l => l.ChapterId == id && l.IsPublished)
                .OrderBy(l => l.OrderIndex)
                .Include(l => l.Questions)
                .ToListAsync();

            var progressList = await _unitOfWork.Progresses.Query()
                .Where(p => p.UserId == userId && lessons.Select(l => l.Id).Contains(p.LessonId))
                .ToListAsync();

            var result = lessons.Select(l =>
            {
                var progress = progressList.FirstOrDefault(p => p.LessonId == l.Id);
                var totalQ = l.Questions.Count;
                var rawBest = progress?.BestScore;

                return new LessonDto
                {
                    Id = l.Id,
                    ChapterId = l.ChapterId,
                    Title = l.Title,
                    ContentBody = l.ContentBody,
                    SimulationType = l.SimulationType,
                    OrderIndex = l.OrderIndex,
                    IsPublished = l.IsPublished,
                    IsCompleted = progress?.IsCompleted ?? false,
                    BestScore = rawBest.HasValue && totalQ > 0
                        ? Math.Round(rawBest.Value * 10.0 / totalQ, 1)
                        : null,
                    Questions = l.Questions.OrderBy(q => q.OrderIndex).Select(q => new QuestionDto
                    {
                        Id = q.Id,
                        LessonId = q.LessonId,
                        QuestionText = q.QuestionText,
                        Options = System.Text.Json.JsonSerializer.Deserialize<List<string>>(q.Options) ?? new(),
                        CorrectOption = null,
                        Explanation = q.Explanation,
                        OrderIndex = q.OrderIndex
                    }).ToList()
                };
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
}
