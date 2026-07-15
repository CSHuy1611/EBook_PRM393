using System.Security.Claims;
using MathIBook.Application.DTOs;
using MathIBook.Application.Interfaces;
using MathIBook.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MathIBook.Api.Controllers;

[Route("api/quiz-attempts")]
[ApiController]
[Authorize(Roles = "Student")]
public class QuizAttemptsController : ControllerBase
{
    private readonly IQuizScoringService _quizScoringService;
    private readonly IUnitOfWork _unitOfWork;

    public QuizAttemptsController(
        IQuizScoringService quizScoringService,
        IUnitOfWork unitOfWork)
    {
        _quizScoringService = quizScoringService;
        _unitOfWork = unitOfWork;
    }

    [HttpPost]
    public async Task<ActionResult<QuizResultDto>> Submit([FromBody] QuizSubmitDto dto)
    {
        try
        {
            return Ok(await _quizScoringService.ScoreQuizAsync(CurrentUserId(), dto));
        }
        catch (UnauthorizedAccessException exception)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Không thể nộp quiz.",
                Detail = exception.Message,
                Status = 401
            });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Không thể nộp quiz.",
                Detail = exception.Message,
                Status = 400
            });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<QuizResultDto>> GetResult(Guid id)
    {
        var userId = CurrentUserId();
        var attempt = await _unitOfWork.QuizAttempts.Query()
            .Include(item => item.Quiz)
            .Include(item => item.Answers)
            .ThenInclude(answer => answer.Question)
            .FirstOrDefaultAsync(item => item.Id == id && item.UserId == userId);
        if (attempt is null)
        {
            return NotFound();
        }

        var attemptNumber = await _unitOfWork.QuizAttempts.Query().CountAsync(item =>
            item.UserId == userId
            && item.QuizId == attempt.QuizId
            && item.CreatedAt <= attempt.CreatedAt);
        return Ok(new QuizResultDto
        {
            Id = attempt.Id,
            QuizId = attempt.QuizId ?? Guid.Empty,
            ClientAttemptId = attempt.ClientAttemptId ?? attempt.Id,
            Score = (double)attempt.Score10,
            IsPassed = attempt.IsPassed,
            PassScore = (double)(attempt.Quiz?.PassScore ?? 5),
            CorrectCount = attempt.Score,
            TotalQuestions = attempt.TotalQuestions,
            AttemptNumber = Math.Max(1, attemptNumber),
            CoinsEarned = attempt.CoinsEarned,
            CorrectAnswers = attempt.Answers.Select(answer => new CorrectAnswerDto
            {
                QuestionId = answer.QuestionId,
                SelectedOption = answer.SelectedOption,
                CorrectOption = answer.Question.CorrectOption,
                IsCorrect = answer.IsCorrect,
                Explanation = answer.Question.Explanation
            }).ToList()
        });
    }

    private Guid CurrentUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
