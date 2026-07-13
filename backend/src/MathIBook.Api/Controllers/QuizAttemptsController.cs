using MathIBook.Application.DTOs;
using MathIBook.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MathIBook.Api.Controllers;

[Route("api/quiz-attempts")]
[ApiController]
[Authorize(Roles = "Student")]
public class QuizAttemptsController : ControllerBase
{
    private readonly IQuizScoringService _quizScoringService;

    public QuizAttemptsController(IQuizScoringService quizScoringService)
    {
        _quizScoringService = quizScoringService;
    }

    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] QuizSubmitDto dto)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _quizScoringService.ScoreQuizAsync(userId, dto);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Quiz submission failed",
                Detail = ex.Message,
                Status = 400
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ProblemDetails
            {
                Title = "Error submitting quiz",
                Detail = ex.Message,
                Status = 500
            });
        }
    }
}
