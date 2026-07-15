using MathIBook.Application.DTOs;
using MathIBook.Domain.Enums;
using MathIBook.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MathIBook.Api.Controllers.Admin;

[Route("api/admin/reports/learning")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminLearningReportsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public AdminLearningReportsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<LearningReportDto>> Get(
        [FromQuery] LearningReportFilterDto filter)
    {
        var query = _unitOfWork.QuizAttempts.Query()
            .Include(attempt => attempt.User)
            .Include(attempt => attempt.Quiz)
            .ThenInclude(quiz => quiz!.Lesson)
            .Include(attempt => attempt.Quiz)
            .ThenInclude(quiz => quiz!.Chapter)
            .AsQueryable();

        if (filter.From.HasValue)
        {
            query = query.Where(attempt => attempt.CreatedAt >= filter.From.Value.ToUniversalTime());
        }

        if (filter.To.HasValue)
        {
            query = query.Where(attempt => attempt.CreatedAt <= filter.To.Value.ToUniversalTime());
        }

        if (filter.LessonId.HasValue)
        {
            query = query.Where(attempt =>
                attempt.LessonId == filter.LessonId
                || attempt.Quiz!.LessonId == filter.LessonId);
        }

        if (filter.ChapterId.HasValue)
        {
            query = query.Where(attempt =>
                attempt.Quiz!.ChapterId == filter.ChapterId
                || attempt.Quiz.Lesson!.ChapterId == filter.ChapterId);
        }

        var attempts = await query.ToListAsync();
        var attemptIds = attempts.Select(attempt => attempt.Id).ToList();
        var answers = await _unitOfWork.QuizAttemptAnswers.Query()
            .Where(answer => attemptIds.Contains(answer.AttemptId))
            .Include(answer => answer.Question)
            .ToListAsync();

        var retryCount = attempts
            .GroupBy(attempt => new { attempt.UserId, attempt.QuizId })
            .Sum(group => Math.Max(0, group.Count() - 1));
        var mostMissed = answers
            .GroupBy(answer => new
            {
                answer.QuestionId,
                answer.Question.QuestionText
            })
            .Select(group => new MostMissedQuestionDto
            {
                QuestionId = group.Key.QuestionId,
                QuestionText = group.Key.QuestionText,
                AnswerCount = group.Count(),
                WrongCount = group.Count(answer => !answer.IsCorrect),
                WrongRate = Math.Round(
                    (double)group.Count(answer => !answer.IsCorrect) / group.Count() * 100,
                    2)
            })
            .OrderByDescending(item => item.WrongRate)
            .ThenByDescending(item => item.AnswerCount)
            .Take(20)
            .ToList();

        var activeStudents = await _unitOfWork.Users.Query()
            .Where(user => user.Role == "Student" && user.IsActive)
            .ToListAsync();
        var chapters = await _unitOfWork.Chapters.Query()
            .Where(chapter => chapter.IsPublished && !chapter.IsDeleted)
            .Include(chapter => chapter.Lessons.Where(lesson =>
                lesson.IsPublished && !lesson.IsDeleted))
            .ToListAsync();
        var progresses = await _unitOfWork.Progresses.Query()
            .Where(progress => progress.Status == LearningStatus.Passed)
            .ToListAsync();
        var lowCompletion = chapters.Select(chapter =>
        {
            var possible = activeStudents.Count * chapter.Lessons.Count;
            var completed = progresses.Count(progress =>
                chapter.Lessons.Any(lesson => lesson.Id == progress.LessonId)
                && activeStudents.Any(student => student.Id == progress.UserId));
            var chapterAttempts = attempts.Where(attempt =>
                attempt.Quiz?.ChapterId == chapter.Id
                || attempt.Quiz?.Lesson?.ChapterId == chapter.Id).ToList();

            return new ChapterReportDto
            {
                ChapterId = chapter.Id,
                ChapterTitle = chapter.Title,
                TotalAttempts = chapterAttempts.Count,
                AverageScore = chapterAttempts.Count == 0
                    ? 0
                    : Math.Round(chapterAttempts.Average(attempt => (double)attempt.Score10), 2),
                CompletionRate = possible == 0
                    ? 0
                    : Math.Round((double)completed / possible * 100, 2)
            };
        })
        .OrderBy(item => item.CompletionRate)
        .Take(10)
        .ToList();

        var badgeCounts = await _unitOfWork.UserBadges.Query()
            .GroupBy(item => item.UserId)
            .Select(group => new { UserId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.UserId, item => item.Count);
        var topStudents = attempts
            .GroupBy(attempt => attempt.UserId)
            .Select(group =>
            {
                var first = group.First();
                badgeCounts.TryGetValue(group.Key, out var badges);
                return new StudentPerformanceDto
                {
                    UserId = group.Key,
                    Name = first.User.Name,
                    Coins = first.User.Coins,
                    Badges = badges,
                    Attempts = group.Count(),
                    AverageScore = Math.Round(group.Average(item => (double)item.Score10), 2)
                };
            })
            .OrderByDescending(item => item.Coins)
            .ThenByDescending(item => item.Badges)
            .Take(20)
            .ToList();

        var transactionsQuery = _unitOfWork.CoinTransactions.Query().AsQueryable();
        var badgesQuery = _unitOfWork.UserBadges.Query().AsQueryable();
        if (filter.From.HasValue)
        {
            var from = filter.From.Value.ToUniversalTime();
            transactionsQuery = transactionsQuery.Where(item => item.CreatedAt >= from);
            badgesQuery = badgesQuery.Where(item => item.EarnedAt >= from);
        }

        if (filter.To.HasValue)
        {
            var to = filter.To.Value.ToUniversalTime();
            transactionsQuery = transactionsQuery.Where(item => item.CreatedAt <= to);
            badgesQuery = badgesQuery.Where(item => item.EarnedAt <= to);
        }

        var coinsByDay = await transactionsQuery
            .GroupBy(item => item.CreatedAt.Date)
            .Select(group => new { Date = group.Key, Coins = group.Sum(item => item.Amount) })
            .ToListAsync();
        var badgesByDay = await badgesQuery
            .GroupBy(item => item.EarnedAt.Date)
            .Select(group => new { Date = group.Key, Badges = group.Count() })
            .ToListAsync();
        var dates = coinsByDay.Select(item => item.Date)
            .Union(badgesByDay.Select(item => item.Date))
            .OrderBy(item => item);
        var rewardsByDay = dates.Select(date => new DailyRewardDto
        {
            Date = date,
            CoinsAwarded = coinsByDay.FirstOrDefault(item => item.Date == date)?.Coins ?? 0,
            BadgesAwarded = badgesByDay.FirstOrDefault(item => item.Date == date)?.Badges ?? 0
        }).ToList();

        return Ok(new LearningReportDto
        {
            TotalAttempts = attempts.Count,
            PassRate = attempts.Count == 0
                ? 0
                : Math.Round((double)attempts.Count(attempt => attempt.IsPassed) / attempts.Count * 100, 2),
            AverageScore = attempts.Count == 0
                ? 0
                : Math.Round(attempts.Average(attempt => (double)attempt.Score10), 2),
            RetryCount = retryCount,
            MostMissedQuestions = mostMissed,
            LowCompletionChapters = lowCompletion,
            TopStudents = topStudents,
            RewardsByDay = rewardsByDay
        });
    }
}
