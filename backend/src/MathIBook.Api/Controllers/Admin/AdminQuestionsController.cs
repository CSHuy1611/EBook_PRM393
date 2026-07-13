using MathIBook.Application.DTOs;
using MathIBook.Domain.Entities;
using MathIBook.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MathIBook.Api.Controllers.Admin;

[Route("api/admin/questions")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminQuestionsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public AdminQuestionsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet("lesson/{lessonId}")]
    public async Task<IActionResult> GetByLesson(Guid lessonId)
    {
        try
        {
            var questions = await _unitOfWork.Questions.Query()
                .Where(q => q.LessonId == lessonId)
                .OrderBy(q => q.OrderIndex)
                .ToListAsync();

            var result = questions.Select(q => new QuestionDto
            {
                Id = q.Id,
                LessonId = q.LessonId,
                QuestionText = q.QuestionText,
                Options = System.Text.Json.JsonSerializer.Deserialize<List<string>>(q.Options) ?? new(),
                CorrectOption = q.CorrectOption,
                Explanation = q.Explanation,
                OrderIndex = q.OrderIndex
            }).ToList();

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ProblemDetails
            {
                Title = "Error fetching questions",
                Detail = ex.Message,
                Status = 500
            });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] QuestionCreateDto dto)
    {
        try
        {
            if (dto.Options.Count != 4 || dto.Options.Any(string.IsNullOrWhiteSpace))
                return BadRequest(new ProblemDetails
                {
                    Title = "Validation error",
                    Detail = "Exactly 4 non-empty options are required",
                    Status = 400
                });

            if (dto.CorrectOption < 0 || dto.CorrectOption > 3)
                return BadRequest(new ProblemDetails
                {
                    Title = "Validation error",
                    Detail = "CorrectOption must be between 0 and 3",
                    Status = 400
                });

            var lesson = await _unitOfWork.Lessons.GetByIdAsync(dto.LessonId);
            if (lesson == null)
                return NotFound(new ProblemDetails { Title = "Lesson not found", Status = 404 });

            var question = new Question
            {
                LessonId = dto.LessonId,
                QuestionText = dto.QuestionText,
                Options = System.Text.Json.JsonSerializer.Serialize(dto.Options),
                CorrectOption = dto.CorrectOption,
                Explanation = dto.Explanation,
                OrderIndex = dto.OrderIndex
            };

            await _unitOfWork.Questions.AddAsync(question);
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
                        Title = "New Question Added",
                        Body = $"A new question has been added to a lesson.",
                        Link = $"/lessons/{question.LessonId}"
                    };

                    await _unitOfWork.Notifications.AddAsync(notification);
                }

                await _unitOfWork.SaveChangesAsync();
            }
            catch
            {
                // Notification failure should not block question creation
            }

            return CreatedAtAction(nameof(GetByLesson), new { lessonId = question.LessonId }, new QuestionDto
            {
                Id = question.Id,
                LessonId = question.LessonId,
                QuestionText = question.QuestionText,
                Options = dto.Options,
                CorrectOption = question.CorrectOption,
                Explanation = question.Explanation,
                OrderIndex = question.OrderIndex
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ProblemDetails
            {
                Title = "Error creating question",
                Detail = ex.Message,
                Status = 500
            });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] QuestionUpdateDto dto)
    {
        try
        {
            if (dto.Options.Count != 4 || dto.Options.Any(string.IsNullOrWhiteSpace))
                return BadRequest(new ProblemDetails
                {
                    Title = "Validation error",
                    Detail = "Exactly 4 non-empty options are required",
                    Status = 400
                });

            if (dto.CorrectOption < 0 || dto.CorrectOption > 3)
                return BadRequest(new ProblemDetails
                {
                    Title = "Validation error",
                    Detail = "CorrectOption must be between 0 and 3",
                    Status = 400
                });

            var question = await _unitOfWork.Questions.GetByIdAsync(id);
            if (question == null)
                return NotFound(new ProblemDetails { Title = "Question not found", Status = 404 });

            question.QuestionText = dto.QuestionText;
            question.Options = System.Text.Json.JsonSerializer.Serialize(dto.Options);
            question.CorrectOption = dto.CorrectOption;
            question.Explanation = dto.Explanation;
            question.OrderIndex = dto.OrderIndex;

            _unitOfWork.Questions.Update(question);
            await _unitOfWork.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ProblemDetails
            {
                Title = "Error updating question",
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
            var question = await _unitOfWork.Questions.GetByIdAsync(id);
            if (question == null)
                return NotFound(new ProblemDetails { Title = "Question not found", Status = 404 });

            _unitOfWork.Questions.Remove(question);
            await _unitOfWork.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ProblemDetails
            {
                Title = "Error deleting question",
                Detail = ex.Message,
                Status = 500
            });
        }
    }
}
