using MathIBook.Application.DTOs;
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
    public async Task<IActionResult> GetOverview([FromQuery] Guid? userId)
    {
        try
        {
            var totalUsers = await _unitOfWork.Users.Query().CountAsync();

            var quizAttemptsQuery = _unitOfWork.QuizAttempts.Query();
            if (userId.HasValue)
                quizAttemptsQuery = quizAttemptsQuery.Where(qa => qa.UserId == userId.Value);

            var totalQuizAttempts = await quizAttemptsQuery.CountAsync();

            var coinTransactionsQuery = _unitOfWork.CoinTransactions.Query()
                .Where(ct => ct.Amount > 0);
            if (userId.HasValue)
                coinTransactionsQuery = coinTransactionsQuery.Where(ct => ct.UserId == userId.Value);
            var totalCoinsAwarded = await coinTransactionsQuery
                .SumAsync(ct => (int?)ct.Amount) ?? 0;

            var userBadgesQuery = _unitOfWork.UserBadges.Query();
            if (userId.HasValue)
                userBadgesQuery = userBadgesQuery.Where(ub => ub.UserId == userId.Value);
            var totalBadgesAwarded = await userBadgesQuery.CountAsync();

            var averageScoreData = await quizAttemptsQuery
                .Where(qa => qa.TotalQuestions > 0)
                .Select(qa => (double)qa.Score / qa.TotalQuestions * 100)
                .ToListAsync();

            var overallAverageScore = averageScoreData.Any()
                ? Math.Round(averageScoreData.Average(), 2)
                : 0;

            var chapters = await _unitOfWork.Chapters.Query()
                .Include(c => c.Lessons)
                .ThenInclude(l => l.QuizAttempts)
                .ToListAsync();

            var chapterReports = chapters.Select(c =>
            {
                var chapterAttempts = userId.HasValue
                    ? c.Lessons.SelectMany(l => l.QuizAttempts.Where(qa => qa.UserId == userId.Value)).ToList()
                    : c.Lessons.SelectMany(l => l.QuizAttempts).ToList();

                var totalAttempts = chapterAttempts.Count;
                var avgScore = chapterAttempts.Where(qa => qa.TotalQuestions > 0)
                    .Select(qa => (double)qa.Score / qa.TotalQuestions * 100)
                    .DefaultIfEmpty()
                    .Average();

                var completedLessons = userId.HasValue
                    ? c.Lessons.Count(l => l.QuizAttempts.Any(qa => qa.UserId == userId.Value))
                    : c.Lessons.Count(l => l.QuizAttempts.Any());

                return new ChapterReportDto
                {
                    ChapterId = c.Id,
                    ChapterTitle = c.Title,
                    TotalAttempts = totalAttempts,
                    AverageScore = Math.Round(avgScore, 2),
                    CompletionRate = c.Lessons.Any()
                        ? Math.Round((double)completedLessons / c.Lessons.Count * 100, 2)
                        : 0
                };
            }).ToList();

            var dailyActivities = await quizAttemptsQuery
                .GroupBy(qa => qa.CreatedAt.Date)
                .Select(g => new DailyActivityDto
                {
                    Date = g.Key,
                    QuizCount = g.Count()
                })
                .OrderByDescending(d => d.Date)
                .Take(30)
                .ToListAsync();

            var newUsersByDay = await _unitOfWork.Users.Query()
                .GroupBy(u => u.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync();

            foreach (var daily in dailyActivities)
            {
                var newUserCount = newUsersByDay
                    .Where(n => n.Date == daily.Date)
                    .Sum(n => n.Count);
                daily.NewUsers = newUserCount;
            }

            var result = new ReportOverviewDto
            {
                TotalUsers = totalUsers,
                TotalQuizAttempts = totalQuizAttempts,
                OverallAverageScore = overallAverageScore,
                TotalCoinsAwarded = totalCoinsAwarded,
                TotalBadgesAwarded = totalBadgesAwarded,
                ChapterReports = chapterReports,
                DailyActivities = dailyActivities
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ProblemDetails
            {
                Title = "Error fetching reports",
                Detail = ex.Message,
                Status = 500
            });
        }
    }
}
