using MathIBook.Application.DTOs;
using MathIBook.Domain.Enums;
using MathIBook.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MathIBook.Api.Controllers.Admin;

[Route("api/admin/reports")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminReportsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public AdminReportsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet("overview")]
    public async Task<ActionResult<ReportOverviewDto>> GetOverview([FromQuery] Guid? userId)
    {
        var studentQuery = _unitOfWork.Users.Query()
            .Where(user => user.Role == "Student");
        if (userId.HasValue)
        {
            studentQuery = studentQuery.Where(user => user.Id == userId);
        }

        var studentIds = await studentQuery.Select(user => user.Id).ToListAsync();
        var attempts = await _unitOfWork.QuizAttempts.Query()
            .Where(attempt => studentIds.Contains(attempt.UserId))
            .Include(attempt => attempt.Quiz)
            .ThenInclude(quiz => quiz!.Lesson)
            .ToListAsync();
        var transactions = await _unitOfWork.CoinTransactions.Query()
            .Where(transaction => studentIds.Contains(transaction.UserId))
            .ToListAsync();
        var badgeCount = await _unitOfWork.UserBadges.Query()
            .CountAsync(item => studentIds.Contains(item.UserId));
        var chapters = await _unitOfWork.Chapters.Query()
            .Where(chapter => !chapter.IsDeleted)
            .Include(chapter => chapter.Lessons.Where(lesson => !lesson.IsDeleted))
            .OrderBy(chapter => chapter.OrderIndex)
            .ToListAsync();
        var progress = await _unitOfWork.Progresses.Query()
            .Where(item =>
                studentIds.Contains(item.UserId)
                && item.Status == LearningStatus.Passed)
            .ToListAsync();

        var chapterReports = chapters.Select(chapter =>
        {
            var chapterAttempts = attempts.Where(attempt =>
                attempt.Quiz?.ChapterId == chapter.Id
                || attempt.Quiz?.Lesson?.ChapterId == chapter.Id
                || chapter.Lessons.Any(lesson => lesson.Id == attempt.LessonId))
                .ToList();
            var possibleCompletions = studentIds.Count * chapter.Lessons.Count;
            var completed = progress.Count(item =>
                chapter.Lessons.Any(lesson => lesson.Id == item.LessonId));

            return new ChapterReportDto
            {
                ChapterId = chapter.Id,
                ChapterTitle = chapter.Title,
                TotalAttempts = chapterAttempts.Count,
                AverageScore = chapterAttempts.Count == 0
                    ? 0
                    : Math.Round(chapterAttempts.Average(item => (double)item.Score10), 2),
                CompletionRate = possibleCompletions == 0
                    ? 0
                    : Math.Round((double)completed / possibleCompletions * 100, 2)
            };
        }).ToList();

        var newUsers = await _unitOfWork.Users.Query()
            .Where(user => user.Role == "Student")
            .GroupBy(user => user.CreatedAt.Date)
            .Select(group => new { Date = group.Key, Count = group.Count() })
            .ToListAsync();
        var daily = attempts
            .GroupBy(attempt => attempt.CreatedAt.Date)
            .Select(group => new DailyActivityDto
            {
                Date = group.Key,
                QuizCount = group.Count(),
                NewUsers = newUsers.FirstOrDefault(item => item.Date == group.Key)?.Count ?? 0
            })
            .OrderByDescending(item => item.Date)
            .Take(30)
            .ToList();

        return Ok(new ReportOverviewDto
        {
            TotalUsers = studentIds.Count,
            TotalQuizAttempts = attempts.Count,
            OverallAverageScore = attempts.Count == 0
                ? 0
                : Math.Round(attempts.Average(attempt => (double)attempt.Score10), 2),
            TotalCoinsAwarded = transactions.Sum(transaction => transaction.Amount),
            TotalBadgesAwarded = badgeCount,
            ChapterReports = chapterReports,
            DailyActivities = daily
        });
    }
}
