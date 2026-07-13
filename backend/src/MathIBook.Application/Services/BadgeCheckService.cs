using System.Text.Json;
using MathIBook.Application.DTOs;
using MathIBook.Application.Interfaces;
using MathIBook.Domain.Entities;
using MathIBook.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MathIBook.Application.Services;

public class BadgeCheckService : IBadgeCheckService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<BadgeCheckService> _logger;

    public BadgeCheckService(IUnitOfWork unitOfWork, ILogger<BadgeCheckService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<List<BadgeEarnedDto>> CheckAndAwardBadgesAsync(Guid userId, Guid? lessonId = null)
    {
        var earnedBadges = new List<BadgeEarnedDto>();
        var allBadges = (await _unitOfWork.Badges.GetAllAsync()).ToList();

        var userBadges = (await _unitOfWork.UserBadges.FindAsync(ub => ub.UserId == userId))
            .Select(ub => ub.BadgeId)
            .ToHashSet();

        foreach (var badge in allBadges)
        {
            if (userBadges.Contains(badge.Id))
            {
                continue;
            }

            var conditionMet = badge.ConditionType switch
            {
                "complete_chapter" => await CheckCompleteChapterAsync(userId, badge.ConditionValue),
                "complete_book" => await CheckCompleteBookAsync(userId),
                "perfect_quiz_streak" => await CheckPerfectQuizStreakAsync(userId, badge.ConditionValue),
                "total_coins" => await CheckTotalCoinsAsync(userId, badge.ConditionValue),
                _ => false
            };

            if (conditionMet)
            {
                var userBadge = new UserBadge
                {
                    UserId = userId,
                    BadgeId = badge.Id,
                    EarnedAt = DateTime.UtcNow
                };

                await _unitOfWork.UserBadges.AddAsync(userBadge);

                var coinTransaction = new CoinTransaction
                {
                    UserId = userId,
                    Amount = 50,
                    SourceType = "badge_unlock",
                    SourceId = badge.Id,
                    Description = $"Unlocked badge: {badge.Title}",
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.CoinTransactions.AddAsync(coinTransaction);

                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user is not null)
                {
                    user.Coins += 50;
                    _unitOfWork.Users.Update(user);
                }

                await _unitOfWork.SaveChangesAsync();

                earnedBadges.Add(new BadgeEarnedDto
                {
                    BadgeId = badge.Id,
                    Title = badge.Title,
                    Description = badge.Description,
                    IconUrl = badge.IconUrl
                });

                _logger.LogInformation("Badge '{BadgeTitle}' awarded to user {UserId}", badge.Title, userId);
            }
        }

        return earnedBadges;
    }

    private async Task<bool> CheckCompleteChapterAsync(Guid userId, string? conditionValue)
    {
        var chapterId = TryExtractGuid(conditionValue);
        if (chapterId is null)
        {
            return false;
        }

        var chapter = await _unitOfWork.Chapters.GetByIdAsync(chapterId.Value);
        if (chapter is null)
        {
            return false;
        }

        var publishedLessons = (await _unitOfWork.Lessons.FindAsync(l => l.ChapterId == chapterId.Value && l.IsPublished)).ToList();
        if (publishedLessons.Count == 0)
        {
            return false;
        }

        var completedCount = 0;
        foreach (var lesson in publishedLessons)
        {
            var progress = await _unitOfWork.Progresses.FirstOrDefaultAsync(
                p => p.UserId == userId && p.LessonId == lesson.Id);
            if (progress is not null && progress.IsCompleted)
            {
                completedCount++;
            }
        }

        return completedCount >= publishedLessons.Count;
    }

    private async Task<bool> CheckCompleteBookAsync(Guid userId)
    {
        var chapters = (await _unitOfWork.Chapters.GetAllAsync()).ToList();
        if (chapters.Count == 0)
        {
            return false;
        }

        foreach (var chapter in chapters)
        {
            var publishedLessons = (await _unitOfWork.Lessons.FindAsync(l => l.ChapterId == chapter.Id && l.IsPublished)).ToList();
            if (publishedLessons.Count == 0)
            {
                continue;
            }

            var completedCount = 0;
            foreach (var lesson in publishedLessons)
            {
                var progress = await _unitOfWork.Progresses.FirstOrDefaultAsync(
                    p => p.UserId == userId && p.LessonId == lesson.Id);
                if (progress is not null && progress.IsCompleted)
                {
                    completedCount++;
                }
            }

            if (completedCount < publishedLessons.Count)
            {
                return false;
            }
        }

        return true;
    }

    private async Task<bool> CheckPerfectQuizStreakAsync(Guid userId, string? conditionValue)
    {
        var requiredStreak = TryExtractInt(conditionValue);
        if (requiredStreak is null || requiredStreak.Value <= 0)
        {
            return false;
        }

        var attempts = (await _unitOfWork.QuizAttempts.FindAsync(qa => qa.UserId == userId))
            .OrderByDescending(qa => qa.CreatedAt)
            .Take(requiredStreak.Value)
            .ToList();

        if (attempts.Count < requiredStreak.Value)
        {
            return false;
        }

        return attempts.All(a => a.Score == a.TotalQuestions);
    }

    private async Task<bool> CheckTotalCoinsAsync(Guid userId, string? conditionValue)
    {
        var requiredCoins = TryExtractInt(conditionValue);
        if (requiredCoins is null || requiredCoins.Value <= 0)
        {
            return false;
        }

        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        return user is not null && user.Coins >= requiredCoins.Value;
    }

    private static Guid? TryExtractGuid(string? value)
    {
        if (Guid.TryParse(value, out var guid))
            return guid;

        if (value is not null)
        {
            try
            {
                var json = JsonSerializer.Deserialize<Dictionary<string, string>>(value);
                if (json is not null && json.TryGetValue("chapterId", out var id) && Guid.TryParse(id, out var parsed))
                    return parsed;
            }
            catch { }
        }

        return null;
    }

    private static int? TryExtractInt(string? value)
    {
        if (int.TryParse(value, out var result))
            return result;

        if (value is not null)
        {
            try
            {
                var json = JsonSerializer.Deserialize<Dictionary<string, int>>(value);
                if (json is not null)
                {
                    if (json.TryGetValue("streak", out var streak)) return streak;
                    if (json.TryGetValue("coins", out var coins)) return coins;
                }
            }
            catch { }
        }

        return null;
    }
}
