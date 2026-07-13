using MathIBook.Application.DTOs;
using MathIBook.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var lesson = await _unitOfWork.Lessons.Query()
                .Where(l => l.Id == id && l.IsPublished)
                .Include(l => l.Questions.OrderBy(q => q.OrderIndex))
                .FirstOrDefaultAsync();

            if (lesson == null)
                return NotFound(new ProblemDetails { Title = "Lesson not found", Status = 404 });

            var progress = await _unitOfWork.Progresses.Query()
                .FirstOrDefaultAsync(p => p.UserId == userId && p.LessonId == id);

            var totalQ = lesson.Questions.Count;
            var rawBest = progress?.BestScore;

            var result = new LessonDto
            {
                Id = lesson.Id,
                ChapterId = lesson.ChapterId,
                Title = lesson.Title,
                ContentBody = lesson.ContentBody,
                SimulationType = lesson.SimulationType,
                OrderIndex = lesson.OrderIndex,
                IsPublished = lesson.IsPublished,
                IsCompleted = progress?.IsCompleted ?? false,
                BestScore = rawBest.HasValue && totalQ > 0
                    ? Math.Round(rawBest.Value * 10.0 / totalQ, 1)
                    : null,
                Questions = lesson.Questions.Select(q => new QuestionDto
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
}
