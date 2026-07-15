using MathIBook.Application.Interfaces;
using MathIBook.Domain.Enums;
using MathIBook.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MathIBook.Api.Controllers.Admin;

[Route("api/admin/dashboard")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminDashboardController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IContentValidationService _validationService;

    public AdminDashboardController(
        IUnitOfWork unitOfWork,
        IContentValidationService validationService)
    {
        _unitOfWork = unitOfWork;
        _validationService = validationService;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var students = await _unitOfWork.Users.Query()
            .Where(user => user.Role == "Student")
            .ToListAsync();
        var attempts = await _unitOfWork.QuizAttempts.Query()
            .Include(attempt => attempt.Quiz)
            .ThenInclude(quiz => quiz!.Lesson)
            .ToListAsync();
        var badgesAwarded = await _unitOfWork.UserBadges.Query().CountAsync();
        var coinsAwarded = await _unitOfWork.CoinTransactions.Query()
            .SumAsync(transaction => (int?)transaction.Amount) ?? 0;
        var lessons = await _unitOfWork.Lessons.Query()
            .Where(lesson => !lesson.IsDeleted)
            .ToListAsync();
        var quizzes = await _unitOfWork.Quizzes.Query()
            .Where(quiz => !quiz.IsDeleted)
            .Include(quiz => quiz.QuizQuestions)
            .ThenInclude(link => link.Question)
            .ToListAsync();

        var lowScoreContent = attempts
            .Where(attempt => attempt.QuizId.HasValue)
            .GroupBy(attempt => new
            {
                attempt.QuizId,
                Title = attempt.Quiz!.Title
            })
            .Select(group => new
            {
                quizId = group.Key.QuizId,
                title = group.Key.Title,
                attemptCount = group.Count(),
                averageScore = Math.Round(group.Average(item => (double)item.Score10), 2)
            })
            .Where(item => item.attemptCount >= 3 && item.averageScore < 5)
            .OrderBy(item => item.averageScore)
            .Take(10)
            .ToList();

        var invalidQuiz = quizzes
            .Select(quiz =>
            {
                var chapterLessonIds = quiz.ChapterId.HasValue
                    ? lessons.Where(lesson => lesson.ChapterId == quiz.ChapterId)
                        .Select(lesson => lesson.Id)
                        .ToList()
                    : new List<Guid>();
                var validation = _validationService.ValidateQuiz(
                    quiz,
                    quiz.QuizQuestions.Where(link => !link.Question.IsDeleted)
                        .Select(link => link.Question)
                        .ToList(),
                    chapterLessonIds);
                return new
                {
                    quiz.Id,
                    quiz.Title,
                    quiz.IsPublished,
                    validation.IsValid,
                    validation.Errors
                };
            })
            .Where(item => !item.IsValid || !item.IsPublished)
            .ToList();

        return Ok(new
        {
            totalStudents = students.Count,
            activeStudents = students.Count(user =>
                user.IsActive
                && user.LastLoginAt >= DateTime.UtcNow.AddDays(-30)),
            totalAttempts = attempts.Count,
            passRate = attempts.Count == 0
                ? 0
                : Math.Round((double)attempts.Count(attempt => attempt.IsPassed) / attempts.Count * 100, 2),
            averageScore = attempts.Count == 0
                ? 0
                : Math.Round(attempts.Average(attempt => (double)attempt.Score10), 2),
            coinsAwarded,
            badgesAwarded,
            lowScoreContent,
            unpublishedOrInvalidQuizzes = invalidQuiz
        });
    }
}
