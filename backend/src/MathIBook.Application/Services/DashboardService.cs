using MathIBook.Application.DTOs;
using MathIBook.Application.Interfaces;
using MathIBook.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MathIBook.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(IUnitOfWork unitOfWork, ILogger<DashboardService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<DashboardDto> GetDashboardAsync(Guid userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user is null)
        {
            throw new InvalidOperationException("User not found.");
        }

        var chapters = (await _unitOfWork.Chapters.GetAllAsync()).OrderBy(c => c.OrderIndex).ToList();
        var allPublishedLessons = new List<Domain.Entities.Lesson>();

        foreach (var chapter in chapters)
        {
            var lessons = (await _unitOfWork.Lessons.FindAsync(l => l.ChapterId == chapter.Id && l.IsPublished))
                .OrderBy(l => l.OrderIndex)
                .ToList();
            allPublishedLessons.AddRange(lessons);
        }

        var progresses = (await _unitOfWork.Progresses.FindAsync(p => p.UserId == userId)).ToList();
        var totalPublishedLessons = allPublishedLessons.Count;
        var completedLessonsCount = progresses.Count(p => p.IsCompleted);
        var overallCompletion = totalPublishedLessons > 0
            ? Math.Round((double)completedLessonsCount / totalPublishedLessons * 100, 2)
            : 0;

        var attempts = (await _unitOfWork.QuizAttempts.FindAsync(qa => qa.UserId == userId)).ToList();
        var averageScore = attempts.Count > 0
            ? Math.Round(attempts.Average(a => (double)a.Score / a.TotalQuestions * 100), 2)
            : 0;

        var chapterProgressList = new List<ChapterProgressDto>();
        foreach (var chapter in chapters)
        {
            var publishedLessons = (await _unitOfWork.Lessons.FindAsync(l => l.ChapterId == chapter.Id && l.IsPublished))
                .OrderBy(l => l.OrderIndex)
                .ToList();

            var totalInChapter = publishedLessons.Count;
            var completedInChapter = 0;

            foreach (var lesson in publishedLessons)
            {
                var progress = progresses.FirstOrDefault(p => p.LessonId == lesson.Id);
                if (progress is not null && progress.IsCompleted)
                {
                    completedInChapter++;
                }
            }

            var chapterPercentage = totalInChapter > 0
                ? Math.Round((double)completedInChapter / totalInChapter * 100, 2)
                : 0;

            chapterProgressList.Add(new ChapterProgressDto
            {
                ChapterId = chapter.Id,
                ChapterTitle = chapter.Title,
                CompletedLessons = completedInChapter,
                TotalLessons = totalInChapter,
                CompletionPercentage = chapterPercentage
            });
        }

        var userBadges = await _unitOfWork.UserBadges.FindAsync(ub => ub.UserId == userId);
        var badges = new List<BadgeEarnedDto>();
        foreach (var ub in userBadges)
        {
            var badge = await _unitOfWork.Badges.GetByIdAsync(ub.BadgeId);
            if (badge is not null)
            {
                badges.Add(new BadgeEarnedDto
                {
                    BadgeId = badge.Id,
                    Title = badge.Title,
                    Description = badge.Description,
                    IconUrl = badge.IconUrl
                });
            }
        }

        var recentActivities = new List<RecentActivityDto>();

        foreach (var attempt in attempts.OrderByDescending(a => a.CreatedAt).Take(5))
        {
            var lesson = await _unitOfWork.Lessons.GetByIdAsync(attempt.LessonId);
            var lessonTitle = lesson?.Title ?? "Unknown Lesson";

            var scaledScore = attempt.TotalQuestions > 0
                ? (int)Math.Round((double)attempt.Score / attempt.TotalQuestions * 10)
                : 0;

            recentActivities.Add(new RecentActivityDto
            {
                Type = "quiz_attempt",
                Description = $"Scored {scaledScore}/10 on \"{lessonTitle}\"",
                Timestamp = attempt.CreatedAt
            });
        }

        var coinTransactions = (await _unitOfWork.CoinTransactions.FindAsync(ct => ct.UserId == userId))
            .OrderByDescending(ct => ct.CreatedAt)
            .Take(5)
            .ToList();

        foreach (var transaction in coinTransactions)
        {
            recentActivities.Add(new RecentActivityDto
            {
                Type = "coin_transaction",
                Description = transaction.Description,
                Timestamp = transaction.CreatedAt
            });
        }

        foreach (var ub in userBadges.OrderByDescending(ub => ub.EarnedAt).Take(3))
        {
            var badge = await _unitOfWork.Badges.GetByIdAsync(ub.BadgeId);
            if (badge is not null)
            {
                recentActivities.Add(new RecentActivityDto
                {
                    Type = "badge_earned",
                    Description = $"Earned badge: {badge.Title}",
                    Timestamp = ub.EarnedAt
                });
            }
        }

        recentActivities = recentActivities.OrderByDescending(r => r.Timestamp).Take(10).ToList();

        return new DashboardDto
        {
            OverallCompletionPercentage = overallCompletion,
            TotalCoins = user.Coins,
            AverageScore = averageScore,
            ChapterProgress = chapterProgressList,
            Badges = badges,
            RecentActivities = recentActivities
        };
    }
}
