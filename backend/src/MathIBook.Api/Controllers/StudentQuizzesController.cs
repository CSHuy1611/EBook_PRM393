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

namespace MathIBook.Api.Controllers;

[Route("api/quizzes")]
[ApiController]
[Authorize(Roles = "Student")]
public class StudentQuizzesController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IQuizScoringService _quizScoringService;

    public StudentQuizzesController(
        IUnitOfWork unitOfWork,
        IQuizScoringService quizScoringService)
    {
        _unitOfWork = unitOfWork;
        _quizScoringService = quizScoringService;
    }

    [HttpGet("lesson/{lessonId}")]
    public async Task<ActionResult<QuizForStudentDto>> GetLessonQuiz(Guid lessonId)
    {
        var quiz = await QueryQuiz()
            .FirstOrDefaultAsync(item =>
                item.QuizType == QuizType.Lesson
                && item.LessonId == lessonId
                && item.IsPublished
                && !item.IsDeleted);
        if (quiz is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Bài học chưa có quiz đã xuất bản.",
                Status = 404
            });
        }

        return Ok(await MapAsync(quiz, isUnlocked: true, missingLessons: new()));
    }

    [HttpGet("chapter/{chapterId}")]
    public async Task<ActionResult<QuizForStudentDto>> GetChapterQuiz(Guid chapterId)
    {
        var quiz = await QueryQuiz()
            .FirstOrDefaultAsync(item =>
                item.QuizType == QuizType.Chapter
                && item.ChapterId == chapterId
                && item.IsPublished
                && !item.IsDeleted);
        if (quiz is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Chương chưa có quiz đã xuất bản.",
                Status = 404
            });
        }

        var userId = CurrentUserId();
        var publishedLessons = await _unitOfWork.Lessons.Query()
            .Where(lesson =>
                lesson.ChapterId == chapterId
                && lesson.IsPublished
                && !lesson.IsDeleted)
            .OrderBy(lesson => lesson.OrderIndex)
            .ToListAsync();
        var lessonIds = publishedLessons.Select(lesson => lesson.Id).ToList();
        var passedIds = await _unitOfWork.Progresses.Query()
            .Where(progress =>
                progress.UserId == userId
                && lessonIds.Contains(progress.LessonId)
                && progress.Status == LearningStatus.Passed)
            .Select(progress => progress.LessonId)
            .ToListAsync();
        var missing = publishedLessons
            .Where(lesson => !passedIds.Contains(lesson.Id))
            .Select(lesson => new MissingLessonDto { Id = lesson.Id, Title = lesson.Title })
            .ToList();

        return Ok(await MapAsync(
            quiz,
            publishedLessons.Count > 0 && missing.Count == 0,
            missing));
    }

    [HttpPost("{quizId}/submit")]
    public async Task<ActionResult<QuizResultDto>> Submit(
        Guid quizId,
        [FromBody] QuizSubmitDto dto)
    {
        if (dto.QuizId.HasValue && dto.QuizId.Value != quizId)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "QuizId trong URL và dữ liệu gửi lên không khớp.",
                Status = 400
            });
        }

        dto.QuizId = quizId;
        try
        {
            return Ok(await _quizScoringService.ScoreQuizAsync(CurrentUserId(), dto));
        }
        catch (UnauthorizedAccessException exception)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Không thể nộp bài.",
                Detail = exception.Message,
                Status = 401
            });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Không thể nộp bài.",
                Detail = exception.Message,
                Status = 400
            });
        }
    }

    private IQueryable<Quiz> QueryQuiz()
    {
        return _unitOfWork.Quizzes.Query()
            .Include(quiz => quiz.QuizQuestions.OrderBy(link => link.OrderIndex))
            .ThenInclude(link => link.Question);
    }

    private async Task<QuizForStudentDto> MapAsync(
        Quiz quiz,
        bool isUnlocked,
        List<MissingLessonDto> missingLessons)
    {
        var userId = CurrentUserId();
        var questions = quiz.QuizQuestions
            .Where(link => !link.Question.IsDeleted)
            .OrderBy(link => link.OrderIndex)
            .Select(link => link.Question)
            .ToList();

        if (questions.Count == 0)
        {
            questions = await _unitOfWork.Questions.Query()
                .Where(question =>
                    !question.IsDeleted
                    && (quiz.QuizType == QuizType.Lesson
                        ? question.LessonId == quiz.LessonId
                        : question.ChapterId == quiz.ChapterId))
                .OrderBy(question => question.OrderIndex)
                .ToListAsync();
        }

        var attempts = await _unitOfWork.QuizAttempts.Query()
            .Where(attempt => attempt.UserId == userId && attempt.QuizId == quiz.Id)
            .ToListAsync();
        var status = "NotStarted";
        double? bestScore = null;

        if (quiz.QuizType == QuizType.Lesson)
        {
            var progress = await _unitOfWork.Progresses.Query()
                .FirstOrDefaultAsync(item =>
                    item.UserId == userId && item.LessonId == quiz.LessonId);
            status = (progress?.Status ?? LearningStatus.NotStarted).ToString();
            bestScore = progress is null ? null : (double)progress.BestScore10;
        }
        else
        {
            var progress = await _unitOfWork.ChapterProgresses.Query()
                .FirstOrDefaultAsync(item =>
                    item.UserId == userId && item.ChapterId == quiz.ChapterId);
            status = !isUnlocked
                ? "Locked"
                : (progress?.Status ?? LearningStatus.InProgress).ToString();
            bestScore = progress is null ? null : (double)progress.BestScore10;
        }

        return new QuizForStudentDto
        {
            Id = quiz.Id,
            QuizType = quiz.QuizType.ToString(),
            LessonId = quiz.LessonId,
            ChapterId = quiz.ChapterId,
            Title = quiz.Title,
            PassScore = (double)quiz.PassScore,
            DurationSeconds = quiz.DurationSeconds,
            IsUnlocked = isUnlocked,
            Status = status,
            BestScore = bestScore,
            AttemptCount = attempts.Count,
            MissingLessons = missingLessons,
            Questions = isUnlocked
                ? questions.Select(question => new StudentQuestionDto
                {
                    Id = question.Id,
                    QuestionText = question.QuestionText,
                    Options = JsonSerializer.Deserialize<List<string>>(question.Options) ?? new(),
                    OrderIndex = question.OrderIndex
                }).ToList()
                : new()
        };
    }

    private Guid CurrentUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
