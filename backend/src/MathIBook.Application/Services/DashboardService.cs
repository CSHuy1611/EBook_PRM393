using MathIBook.Application.DTOs;
using MathIBook.Application.Interfaces;
using MathIBook.Domain.Enums;
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
        if (user is null || !user.IsActive)
        {
            throw new InvalidOperationException("User not found or inactive.");
        }

        var chapters = (await _unitOfWork.Chapters.FindAsync(
                chapter => chapter.IsPublished && !chapter.IsDeleted))
            .OrderBy(chapter => chapter.OrderIndex)
            .ToList();
        var lessons = (await _unitOfWork.Lessons.FindAsync(
                lesson => lesson.IsPublished && !lesson.IsDeleted))
            .Where(lesson => chapters.Any(chapter => chapter.Id == lesson.ChapterId))
            .OrderBy(lesson => lesson.OrderIndex)
            .ToList();
        var progresses = (await _unitOfWork.Progresses.FindAsync(
                progress => progress.UserId == userId))
            .ToList();
        var chapterProgresses = (await _unitOfWork.ChapterProgresses.FindAsync(
                progress => progress.UserId == userId))
            .ToList();
        var chapterQuizzes = (await _unitOfWork.Quizzes.FindAsync(
                quiz =>
                    quiz.QuizType == QuizType.Chapter
                    && quiz.IsPublished
                    && !quiz.IsDeleted))
            .ToList();
        var attempts = (await _unitOfWork.QuizAttempts.FindAsync(
                attempt => attempt.UserId == userId))
            .ToList();

        var completedLessons = progresses.Count(progress =>
            progress.Status == LearningStatus.Passed
            && lessons.Any(lesson => lesson.Id == progress.LessonId));
        var overallCompletion = lessons.Count > 0
            ? Math.Round((double)completedLessons / lessons.Count * 100, 2)
            : 0;
        var chapterQuizIds = chapterQuizzes.Select(q => q.Id).ToHashSet();
        var chapterAttempts = attempts.Where(a => a.QuizId.HasValue && chapterQuizIds.Contains(a.QuizId.Value)).ToList();
        var bestScores = chapterAttempts
            .GroupBy(a => a.QuizId!.Value)
            .Select(g => (double)g.Max(a => a.Score10))
            .ToList();
        var averageScore = bestScores.Count > 0
            ? Math.Round(bestScores.Average(), 2)
            : 0;

        var sortedChapters = chapters.OrderBy(chapter => chapter.OrderIndex).ToList();
        var chapterProgressDtos = sortedChapters.Select((chapter, index) =>
        {
            var chapterLessons = lessons.Where(lesson => lesson.ChapterId == chapter.Id).ToList();
            var passed = chapterLessons.Count(lesson =>
                progresses.Any(progress =>
                    progress.LessonId == lesson.Id
                    && progress.Status == LearningStatus.Passed));
            var quiz = chapterQuizzes.FirstOrDefault(item => item.ChapterId == chapter.Id);
            var chapterProgress = chapterProgresses.FirstOrDefault(item => item.ChapterId == chapter.Id);
            var quizStatus = quiz is null
                ? "Unavailable"
                : chapterProgress?.Status == LearningStatus.Passed
                    ? "Passed"
                    : passed == chapterLessons.Count && chapterLessons.Count > 0
                        ? "Unlocked"
                        : "Locked";

            var isUnlocked = index == 0;
            if (!isUnlocked)
            {
                var prevChapter = sortedChapters[index - 1];
                var prevQuiz = chapterQuizzes.FirstOrDefault(q => q.ChapterId == prevChapter.Id);
                if (prevQuiz is null)
                {
                    isUnlocked = true;
                }
                else
                {
                    var prevProgress = chapterProgresses.FirstOrDefault(p => p.ChapterId == prevChapter.Id);
                    isUnlocked = prevProgress?.Status == LearningStatus.Passed;
                }
            }

            var totalLessons = chapterLessons.Count;
            var totalItems = totalLessons + (quiz != null ? 1 : 0);
            var completedItems = passed + (quiz != null && chapterProgress?.Status == LearningStatus.Passed ? 1 : 0);

            return new ChapterProgressDto
            {
                ChapterId = chapter.Id,
                ChapterTitle = chapter.Title,
                CompletedLessons = completedItems,
                TotalLessons = totalItems,
                CompletionPercentage = totalItems > 0
                    ? Math.Round((double)completedItems / totalItems * 100, 2)
                    : 0,
                ChapterQuizStatus = isUnlocked ? quizStatus : "Locked",
                IsUnlocked = isUnlocked
            };
        }).ToList();

        var userBadges = (await _unitOfWork.UserBadges.FindAsync(
                userBadge => userBadge.UserId == userId))
            .OrderByDescending(userBadge => userBadge.EarnedAt)
            .ToList();
        var activeBadges = (await _unitOfWork.Badges.FindAsync(
                badge => badge.IsActive && !badge.IsDeleted))
            .ToList();
        var badges = userBadges.Select(userBadge =>
        {
            var badge = activeBadges.FirstOrDefault(item => item.Id == userBadge.BadgeId);
            return badge is null
                ? null
                : new BadgeEarnedDto
                {
                    BadgeId = badge.Id,
                    Title = badge.Title,
                    Description = badge.Description,
                    IconUrl = badge.IconUrl
                };
        }).Where(item => item is not null).Cast<BadgeEarnedDto>().ToList();

        var continueLearning = BuildContinueLearning(chapters, lessons, progresses, chapterQuizzes, chapterProgresses);
        var recentActivities = await BuildRecentActivitiesAsync(attempts, userBadges, activeBadges, userId);

        _logger.LogDebug("Dashboard loaded for user {UserId}", userId);
        return new DashboardDto
        {
            OverallCompletionPercentage = overallCompletion,
            TotalCoins = user.Coins,
            EarnedBadgeCount = badges.Count,
            TotalBadgeCount = activeBadges.Count,
            CompletedLessons = completedLessons,
            TotalLessons = lessons.Count,
            AverageScore = averageScore,
            ContinueLearning = continueLearning,
            ChapterProgress = chapterProgressDtos,
            Badges = badges,
            RecentActivities = recentActivities
        };
    }

    private static ContinueLearningDto? BuildContinueLearning(
        IReadOnlyCollection<Domain.Entities.Chapter> chapters,
        IReadOnlyCollection<Domain.Entities.Lesson> lessons,
        IReadOnlyCollection<Domain.Entities.Progress> progresses,
        IReadOnlyCollection<Domain.Entities.Quiz> chapterQuizzes,
        IReadOnlyCollection<Domain.Entities.ChapterProgress> chapterProgresses)
    {
        var sortedChapters = chapters.OrderBy(c => c.OrderIndex).ToList();
        var unlockedChapterIds = new HashSet<Guid>();
        for (int i = 0; i < sortedChapters.Count; i++)
        {
            var ch = sortedChapters[i];
            if (i == 0)
            {
                unlockedChapterIds.Add(ch.Id);
            }
            else
            {
                var prevChapter = sortedChapters[i - 1];
                var prevQuiz = chapterQuizzes.FirstOrDefault(q => q.ChapterId == prevChapter.Id);
                if (prevQuiz == null)
                {
                    unlockedChapterIds.Add(ch.Id);
                }
                else
                {
                    var prevProgress = chapterProgresses.FirstOrDefault(p => p.ChapterId == prevChapter.Id);
                    if (prevProgress?.Status == LearningStatus.Passed)
                    {
                        unlockedChapterIds.Add(ch.Id);
                    }
                }
            }
        }

        var startedChapterIds = new HashSet<Guid>();
        foreach (var ch in sortedChapters)
        {
            var hasChapterProgress = progresses.Any(p => lessons.Any(l => l.ChapterId == ch.Id && l.Id == p.LessonId));
            if (hasChapterProgress)
            {
                startedChapterIds.Add(ch.Id);
            }
        }

        var candidate = lessons
            .Where(lesson => unlockedChapterIds.Contains(lesson.ChapterId) && startedChapterIds.Contains(lesson.ChapterId))
            .Select(lesson => new
            {
                Lesson = lesson,
                Progress = progresses.FirstOrDefault(item => item.LessonId == lesson.Id)
            })
            .Where(item => item.Progress?.Status != LearningStatus.Passed)
            .OrderByDescending(item => item.Progress?.LastViewedAt ?? DateTime.MinValue)
            .ThenBy(item => chapters.First(chapter => chapter.Id == item.Lesson.ChapterId).OrderIndex)
            .ThenBy(item => item.Lesson.OrderIndex)
            .FirstOrDefault();

        if (candidate is null)
        {
            return null;
        }

        var parentChapter = chapters.First(item => item.Id == candidate.Lesson.ChapterId);
        return new ContinueLearningDto
        {
            ChapterId = parentChapter.Id,
            LessonId = candidate.Lesson.Id,
            ChapterTitle = parentChapter.Title,
            LessonTitle = candidate.Lesson.Title,
            Status = (candidate.Progress?.Status ?? LearningStatus.NotStarted).ToString()
        };
    }

    private async Task<List<RecentActivityDto>> BuildRecentActivitiesAsync(
        IReadOnlyCollection<Domain.Entities.QuizAttempt> attempts,
        IReadOnlyCollection<Domain.Entities.UserBadge> userBadges,
        IReadOnlyCollection<Domain.Entities.Badge> badges,
        Guid userId)
    {
        var activities = new List<RecentActivityDto>();
        foreach (var attempt in attempts.OrderByDescending(item => item.CreatedAt).Take(5))
        {
            var quiz = attempt.QuizId.HasValue
                ? await _unitOfWork.Quizzes.GetByIdAsync(attempt.QuizId.Value)
                : null;
            activities.Add(new RecentActivityDto
            {
                Type = "quiz_attempt",
                Description = $"Đạt {attempt.Score10:0.##}/10 ở “{quiz?.Title ?? "Quiz"}”",
                Timestamp = attempt.CreatedAt
            });
        }

        var transactions = (await _unitOfWork.CoinTransactions.FindAsync(
                transaction => transaction.UserId == userId))
            .OrderByDescending(transaction => transaction.CreatedAt)
            .Take(5);
        activities.AddRange(transactions.Select(transaction => new RecentActivityDto
        {
            Type = "coin_transaction",
            Description = transaction.Description,
            Timestamp = transaction.CreatedAt
        }));

        activities.AddRange(userBadges.Take(3).Select(userBadge => new RecentActivityDto
        {
            Type = "badge_earned",
            Description = $"Nhận huy hiệu: {badges.FirstOrDefault(badge => badge.Id == userBadge.BadgeId)?.Title ?? "Huy hiệu"}",
            Timestamp = userBadge.EarnedAt
        }));

        return activities.OrderByDescending(activity => activity.Timestamp).Take(3).ToList();
    }
}
