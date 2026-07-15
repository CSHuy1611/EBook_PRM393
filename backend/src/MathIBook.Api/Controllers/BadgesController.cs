using System.Security.Claims;
using System.Text.Json;
using MathIBook.Application.DTOs;
using MathIBook.Domain.Entities;
using MathIBook.Domain.Enums;
using MathIBook.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MathIBook.Api.Controllers;

[Route("api/badges")]
[ApiController]
[Authorize(Roles = "Student")]
public class BadgesController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public BadgesController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<BadgeCollectionDto>> GetAll()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user is null)
        {
            return NotFound();
        }

        var badges = await _unitOfWork.Badges.Query()
            .Where(badge => badge.IsActive && !badge.IsDeleted)
            .Include(badge => badge.Rules.OrderBy(rule => rule.OrderIndex))
            .OrderBy(badge => badge.CreatedAt)
            .ToListAsync();
        var earned = await _unitOfWork.UserBadges.Query()
            .Where(item => item.UserId == userId)
            .ToDictionaryAsync(item => item.BadgeId);
        var attempts = await _unitOfWork.QuizAttempts.Query()
            .Where(attempt => attempt.UserId == userId)
            .OrderByDescending(attempt => attempt.CreatedAt)
            .ToListAsync();
        var chapterProgress = await _unitOfWork.ChapterProgresses.Query()
            .Where(progress => progress.UserId == userId)
            .ToListAsync();
        var publishedChapterCount = await _unitOfWork.Chapters.Query()
            .CountAsync(chapter => chapter.IsPublished && !chapter.IsDeleted);

        var items = badges.Select(badge =>
        {
            var evaluations = badge.Rules.Count > 0
                ? badge.Rules.Select(rule => EvaluateRule(
                    rule,
                    user,
                    attempts,
                    chapterProgress,
                    publishedChapterCount)).ToList()
                : new List<RuleProgress>
                {
                    EvaluateLegacy(
                        badge,
                        user,
                        attempts,
                        chapterProgress,
                        publishedChapterCount)
                };
            var isAny = string.Equals(badge.RuleMatchMode, "ANY", StringComparison.OrdinalIgnoreCase);
            var progressPercentage = evaluations.Count == 0
                ? 0
                : isAny
                    ? evaluations.Max(item => item.Percentage)
                    : evaluations.Min(item => item.Percentage);
            earned.TryGetValue(badge.Id, out var userBadge);

            return new BadgeCollectionItemDto
            {
                Id = badge.Id,
                Title = badge.Title,
                Description = badge.Description,
                IconUrl = badge.IconUrl,
                Status = userBadge is not null
                    ? "Earned"
                    : progressPercentage > 0
                        ? "InProgress"
                        : "Locked",
                EarnedAt = userBadge?.EarnedAt,
                ProgressPercentage = userBadge is not null ? 100 : progressPercentage,
                Requirement = string.Join(
                    isAny ? " hoặc " : " và ",
                    evaluations.Select(item => item.Requirement)),
                CurrentValue = evaluations.Count == 1 ? evaluations[0].Current : 0,
                TargetValue = evaluations.Count == 1 ? evaluations[0].Target : 0
            };
        }).ToList();

        return Ok(new BadgeCollectionDto
        {
            EarnedCount = items.Count(item => item.Status == "Earned"),
            TotalCount = items.Count,
            Items = items
        });
    }

    private static RuleProgress EvaluateRule(
        BadgeRule rule,
        User user,
        IReadOnlyCollection<QuizAttempt> attempts,
        IReadOnlyCollection<ChapterProgress> chapterProgress,
        int publishedChapterCount)
    {
        return rule.RuleType.ToLowerInvariant() switch
        {
            "complete_chapter" => Binary(
                chapterProgress.Any(item =>
                    item.ChapterId == rule.TargetChapterId
                    && item.Status == LearningStatus.Passed),
                "Hoàn thành quiz của chương"),
            "total_coins" => Threshold(user.Coins, rule.ThresholdValue ?? 0, "Tích lũy xu"),
            "passed_quizzes" => Threshold(
                attempts.Where(item => item.IsPassed)
                    .Select(item => item.QuizId)
                    .Where(item => item.HasValue)
                    .Distinct()
                    .Count(),
                rule.ThresholdValue ?? 0,
                "Vượt qua quiz"),
            "perfect_quiz_streak" => Threshold(
                CurrentPerfectStreak(attempts),
                rule.ThresholdValue ?? 0,
                "Đạt điểm 10 liên tiếp"),
            "complete_book" => Threshold(
                chapterProgress.Count(item => item.Status == LearningStatus.Passed),
                publishedChapterCount,
                "Hoàn thành chương"),
            _ => Threshold(0, 1, $"Quy tắc {rule.RuleType}")
        };
    }

    private static RuleProgress EvaluateLegacy(
        Badge badge,
        User user,
        IReadOnlyCollection<QuizAttempt> attempts,
        IReadOnlyCollection<ChapterProgress> chapterProgress,
        int publishedChapterCount)
    {
        var threshold = ExtractInt(badge.ConditionValue);
        return badge.ConditionType.ToLowerInvariant() switch
        {
            "complete_chapter" => Binary(
                chapterProgress.Any(item =>
                    item.ChapterId == ExtractGuid(badge.ConditionValue)
                    && item.Status == LearningStatus.Passed),
                "Hoàn thành quiz của chương"),
            "total_coins" => Threshold(user.Coins, threshold, "Tích lũy xu"),
            "passed_quizzes" => Threshold(
                attempts.Count(item => item.IsPassed),
                threshold,
                "Vượt qua quiz"),
            "perfect_quiz_streak" => Threshold(
                CurrentPerfectStreak(attempts),
                threshold,
                "Đạt điểm 10 liên tiếp"),
            "complete_book" => Threshold(
                chapterProgress.Count(item => item.Status == LearningStatus.Passed),
                publishedChapterCount,
                "Hoàn thành chương"),
            _ => Threshold(0, 1, badge.Description)
        };
    }

    private static RuleProgress Binary(bool value, string requirement) =>
        Threshold(value ? 1 : 0, 1, requirement);

    private static RuleProgress Threshold(int current, int target, string requirement)
    {
        target = Math.Max(1, target);
        return new RuleProgress(
            current,
            target,
            Math.Round(Math.Min(100, (double)current / target * 100), 2),
            $"{requirement}: {Math.Min(current, target)}/{target}");
    }

    private static int CurrentPerfectStreak(IReadOnlyCollection<QuizAttempt> attempts)
    {
        var count = 0;
        foreach (var attempt in attempts.OrderByDescending(item => item.CreatedAt))
        {
            if (attempt.TotalQuestions == 0 || attempt.Score != attempt.TotalQuestions)
            {
                break;
            }

            count++;
        }

        return count;
    }

    private static int ExtractInt(string? value)
    {
        if (int.TryParse(value?.Trim('"'), out var direct))
        {
            return direct;
        }

        try
        {
            var values = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(value ?? "{}");
            foreach (var key in new[] { "value", "coins", "streak", "count" })
            {
                if (values?.TryGetValue(key, out var element) == true
                    && element.TryGetInt32(out var parsed))
                {
                    return parsed;
                }
            }
        }
        catch (JsonException)
        {
        }

        return 1;
    }

    private static Guid? ExtractGuid(string? value)
    {
        if (Guid.TryParse(value?.Trim('"'), out var direct))
        {
            return direct;
        }

        try
        {
            var values = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(value ?? "{}");
            if (values?.TryGetValue("chapterId", out var element) == true
                && Guid.TryParse(element.ToString(), out var parsed))
            {
                return parsed;
            }
        }
        catch (JsonException)
        {
        }

        return null;
    }

    private sealed record RuleProgress(
        int Current,
        int Target,
        double Percentage,
        string Requirement);
}
