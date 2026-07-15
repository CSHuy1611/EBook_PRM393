using System.Text.Json;
using MathIBook.Application.DTOs;
using MathIBook.Application.Interfaces;
using MathIBook.Domain.Entities;
using MathIBook.Domain.Enums;
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

    public async Task<List<BadgeEarnedDto>> CheckAndAwardBadgesAsync(
        Guid userId,
        Guid? contextChapterId = null)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId)
            ?? throw new InvalidOperationException("User not found.");
        var earnedBadges = new List<BadgeEarnedDto>();
        var badges = (await _unitOfWork.Badges.GetAllAsync())
            .Where(badge => badge.IsActive && !badge.IsDeleted)
            .ToList();
        var alreadyEarned = (await _unitOfWork.UserBadges.FindAsync(
                userBadge => userBadge.UserId == userId))
            .Select(userBadge => userBadge.BadgeId)
            .ToHashSet();

        foreach (var badge in badges)
        {
            if (alreadyEarned.Contains(badge.Id))
            {
                continue;
            }

            var rules = (await _unitOfWork.BadgeRules.FindAsync(rule => rule.BadgeId == badge.Id))
                .OrderBy(rule => rule.OrderIndex)
                .ToList();
            var conditionMet = rules.Count > 0
                ? await CheckStructuredRulesAsync(userId, badge.RuleMatchMode, rules)
                : await CheckLegacyConditionAsync(userId, badge);

            if (!conditionMet)
            {
                continue;
            }

            var occurredAt = DateTime.UtcNow;
            await _unitOfWork.UserBadges.AddAsync(new UserBadge
            {
                UserId = userId,
                BadgeId = badge.Id,
                BadgeRuleId = rules.Count == 1 ? rules[0].Id : null,
                SourceId = contextChapterId ?? badge.Id,
                EarnedAt = occurredAt
            });

            if (badge.RewardCoins > 0)
            {
                user.Coins += badge.RewardCoins;
                user.CoinsUpdatedAt = occurredAt;
                user.UpdatedAt = occurredAt;
                _unitOfWork.Users.Update(user);

                await _unitOfWork.CoinTransactions.AddAsync(new CoinTransaction
                {
                    UserId = userId,
                    Amount = badge.RewardCoins,
                    SourceType = "badge_unlock",
                    SourceId = badge.Id,
                    IdempotencyKey = $"badge_unlock:{userId:N}:{badge.Id:N}",
                    BalanceAfter = user.Coins,
                    Description = $"Nhận huy hiệu: {badge.Title}",
                    CreatedAt = occurredAt
                });
            }

            await _unitOfWork.Notifications.AddAsync(new Notification
            {
                UserId = userId,
                Title = "Bạn vừa nhận huy hiệu mới",
                Body = $"Chúc mừng! Bạn đã nhận huy hiệu “{badge.Title}”.",
                Link = "/badges",
                Type = "badge_awarded",
                RelatedEntityId = badge.Id,
                CreatedAt = occurredAt
            });
            await _unitOfWork.SaveChangesAsync();

            alreadyEarned.Add(badge.Id);
            earnedBadges.Add(new BadgeEarnedDto
            {
                BadgeId = badge.Id,
                Title = badge.Title,
                Description = badge.Description,
                IconUrl = badge.IconUrl
            });

            _logger.LogInformation(
                "Badge {BadgeId} awarded to user {UserId}",
                badge.Id,
                userId);
        }

        return earnedBadges;
    }

    private async Task<bool> CheckStructuredRulesAsync(
        Guid userId,
        string matchMode,
        IReadOnlyCollection<BadgeRule> rules)
    {
        var results = new List<bool>();
        foreach (var rule in rules)
        {
            results.Add(await CheckRuleAsync(userId, rule));
        }

        return string.Equals(matchMode, "ANY", StringComparison.OrdinalIgnoreCase)
            ? results.Any(result => result)
            : results.All(result => result);
    }

    private Task<bool> CheckRuleAsync(Guid userId, BadgeRule rule)
    {
        return rule.RuleType.ToLowerInvariant() switch
        {
            "complete_chapter" => CheckCompleteChapterAsync(userId, rule.TargetChapterId),
            "total_coins" => CheckTotalCoinsAsync(userId, rule.ThresholdValue),
            "passed_quizzes" => CheckPassedQuizCountAsync(userId, rule.ThresholdValue),
            "perfect_quiz_streak" => CheckPerfectQuizStreakAsync(userId, rule.ThresholdValue),
            "complete_book" => CheckCompleteBookAsync(userId),
            _ => Task.FromResult(false)
        };
    }

    private Task<bool> CheckLegacyConditionAsync(Guid userId, Badge badge)
    {
        return badge.ConditionType.ToLowerInvariant() switch
        {
            "complete_chapter" => CheckCompleteChapterAsync(userId, TryExtractGuid(badge.ConditionValue)),
            "complete_book" => CheckCompleteBookAsync(userId),
            "perfect_quiz_streak" => CheckPerfectQuizStreakAsync(
                userId,
                TryExtractInt(badge.ConditionValue)),
            "total_coins" => CheckTotalCoinsAsync(userId, TryExtractInt(badge.ConditionValue)),
            "passed_quizzes" => CheckPassedQuizCountAsync(userId, TryExtractInt(badge.ConditionValue)),
            _ => Task.FromResult(false)
        };
    }

    private async Task<bool> CheckCompleteChapterAsync(Guid userId, Guid? chapterId)
    {
        if (!chapterId.HasValue)
        {
            return false;
        }

        var progress = await _unitOfWork.ChapterProgresses.FirstOrDefaultAsync(
            item => item.UserId == userId && item.ChapterId == chapterId.Value);
        return progress?.Status == LearningStatus.Passed;
    }

    private async Task<bool> CheckCompleteBookAsync(Guid userId)
    {
        var chapters = (await _unitOfWork.Chapters.FindAsync(
                chapter => chapter.IsPublished && !chapter.IsDeleted))
            .Select(chapter => chapter.Id)
            .ToList();
        if (chapters.Count == 0)
        {
            return false;
        }

        var completed = (await _unitOfWork.ChapterProgresses.FindAsync(
                item => item.UserId == userId && item.Status == LearningStatus.Passed))
            .Select(item => item.ChapterId)
            .ToHashSet();
        return chapters.All(completed.Contains);
    }

    private async Task<bool> CheckPerfectQuizStreakAsync(Guid userId, int? requiredStreak)
    {
        if (!requiredStreak.HasValue || requiredStreak.Value <= 0)
        {
            return false;
        }

        var attempts = (await _unitOfWork.QuizAttempts.FindAsync(
                attempt => attempt.UserId == userId))
            .OrderByDescending(attempt => attempt.CreatedAt)
            .Take(requiredStreak.Value)
            .ToList();
        return attempts.Count == requiredStreak.Value
            && attempts.All(attempt =>
                attempt.TotalQuestions > 0
                && attempt.Score == attempt.TotalQuestions);
    }

    private async Task<bool> CheckPassedQuizCountAsync(Guid userId, int? requiredCount)
    {
        if (!requiredCount.HasValue || requiredCount.Value <= 0)
        {
            return false;
        }

        var attempts = await _unitOfWork.QuizAttempts.FindAsync(
            attempt => attempt.UserId == userId && attempt.IsPassed);
        return attempts
            .Select(attempt => attempt.QuizId)
            .Where(quizId => quizId.HasValue)
            .Distinct()
            .Count() >= requiredCount.Value;
    }

    private async Task<bool> CheckTotalCoinsAsync(Guid userId, int? requiredCoins)
    {
        if (!requiredCoins.HasValue || requiredCoins.Value < 0)
        {
            return false;
        }

        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        return user is not null && user.Coins >= requiredCoins.Value;
    }

    private static Guid? TryExtractGuid(string? conditionValue)
    {
        if (string.IsNullOrWhiteSpace(conditionValue))
        {
            return null;
        }

        if (Guid.TryParse(conditionValue.Trim('"'), out var direct))
        {
            return direct;
        }

        try
        {
            var values = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(conditionValue);
            if (values is not null
                && values.TryGetValue("chapterId", out var chapterId)
                && Guid.TryParse(chapterId.ToString(), out var parsed))
            {
                return parsed;
            }
        }
        catch (JsonException)
        {
        }

        return null;
    }

    private static int? TryExtractInt(string? conditionValue)
    {
        if (string.IsNullOrWhiteSpace(conditionValue))
        {
            return null;
        }

        if (int.TryParse(conditionValue.Trim('"'), out var direct))
        {
            return direct;
        }

        try
        {
            var values = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(conditionValue);
            if (values is null)
            {
                return null;
            }

            foreach (var key in new[] { "value", "streak", "coins", "count" })
            {
                if (values.TryGetValue(key, out var value) && value.TryGetInt32(out var parsed))
                {
                    return parsed;
                }
            }
        }
        catch (JsonException)
        {
        }

        return null;
    }
}
