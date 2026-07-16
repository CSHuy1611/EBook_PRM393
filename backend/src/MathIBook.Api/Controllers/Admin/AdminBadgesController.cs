using MathIBook.Application.DTOs;
using MathIBook.Domain.Entities;
using MathIBook.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MathIBook.Api.Controllers.Admin;

[Route("api/admin/badges")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminBadgesController : ControllerBase
{
    private static readonly HashSet<string> SupportedRuleTypes =
    [
        "complete_chapter",
        "complete_book",
        "total_coins",
        "passed_quizzes",
        "perfect_quiz_streak"
    ];

    private static readonly HashSet<string> ThresholdRuleTypes =
    [
        "total_coins",
        "passed_quizzes",
        "perfect_quiz_streak"
    ];

    private readonly IUnitOfWork _unitOfWork;

    public AdminBadgesController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var badges = await _unitOfWork.Badges.Query()
            .Where(badge => !badge.IsDeleted)
            .Include(badge => badge.Rules.OrderBy(rule => rule.OrderIndex))
            .Select(badge => new
            {
                badge.Id,
                badge.Title,
                badge.Description,
                badge.IconUrl,
                badge.RuleMatchMode,
                badge.RewardCoins,
                badge.IsActive,
                earnedCount = badge.UserBadges.Count,
                rules = badge.Rules.Select(rule => new
                {
                    rule.Id,
                    rule.RuleType,
                    rule.TargetChapterId,
                    rule.TargetQuizId,
                    rule.ThresholdValue,
                    rule.OrderIndex,
                    rule.Parameters
                })
            })
            .ToListAsync();
        return Ok(badges);
    }

    [HttpGet("{id}/preview")]
    public async Task<IActionResult> Preview(Guid id)
    {
        var badge = await _unitOfWork.Badges.Query()
            .Include(item => item.Rules.OrderBy(rule => rule.OrderIndex))
            .FirstOrDefaultAsync(item => item.Id == id && !item.IsDeleted);
        if (badge is null)
        {
            return NotFound();
        }

        return Ok(new
        {
            badge.Id,
            badge.Title,
            badge.Description,
            badge.RuleMatchMode,
            badge.RewardCoins,
            badge.IsActive,
            earnedCount = await _unitOfWork.UserBadges.Query()
                .CountAsync(item => item.BadgeId == id),
            badge.Rules
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] BadgeAdminUpsertDto dto)
    {
        var validation = await ValidateAsync(dto);
        if (validation is not null)
        {
            return BadRequest(validation);
        }

        var badge = new Badge
        {
            Title = dto.Title.Trim(),
            Description = dto.Description.Trim(),
            IconUrl = dto.IconUrl.Trim(),
            ConditionType = dto.Rules.FirstOrDefault()?.RuleType ?? "structured",
            ConditionValue = null,
            RuleMatchMode = dto.RuleMatchMode.ToUpperInvariant(),
            RewardCoins = dto.RewardCoins,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _unitOfWork.Badges.AddAsync(badge);
        foreach (var rule in dto.Rules)
        {
            await _unitOfWork.BadgeRules.AddAsync(MapRule(badge.Id, rule));
        }

        await _unitOfWork.SaveChangesAsync();
        return Ok(new { badge.Id });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] BadgeAdminUpsertDto dto)
    {
        var validation = await ValidateAsync(dto);
        if (validation is not null)
        {
            return BadRequest(validation);
        }

        var badge = await _unitOfWork.Badges.GetByIdAsync(id);
        if (badge is null || badge.IsDeleted)
        {
            return NotFound();
        }

        badge.Title = dto.Title.Trim();
        badge.Description = dto.Description.Trim();
        badge.IconUrl = dto.IconUrl.Trim();
        badge.ConditionType = dto.Rules.FirstOrDefault()?.RuleType ?? "structured";
        badge.ConditionValue = null;
        badge.RuleMatchMode = dto.RuleMatchMode.ToUpperInvariant();
        badge.RewardCoins = dto.RewardCoins;
        badge.IsActive = dto.IsActive;
        badge.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Badges.Update(badge);

        var oldRules = await _unitOfWork.BadgeRules.Query()
            .Where(rule => rule.BadgeId == id)
            .ToListAsync();
        foreach (var rule in oldRules)
        {
            _unitOfWork.BadgeRules.Remove(rule);
        }

        foreach (var rule in dto.Rules)
        {
            await _unitOfWork.BadgeRules.AddAsync(MapRule(id, rule));
        }

        await _unitOfWork.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var badge = await _unitOfWork.Badges.GetByIdAsync(id);
        if (badge is null || badge.IsDeleted)
        {
            return NotFound();
        }

        badge.IsDeleted = true;
        badge.IsActive = false;
        badge.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Badges.Update(badge);
        await _unitOfWork.SaveChangesAsync();
        return NoContent();
    }

    private async Task<ProblemDetails?> ValidateAsync(BadgeAdminUpsertDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title)
            || string.IsNullOrWhiteSpace(dto.Description)
            || string.IsNullOrWhiteSpace(dto.IconUrl)
            || dto.RewardCoins < 0
            || dto.Rules.Count == 0
            || dto.RuleMatchMode.ToUpperInvariant() is not ("ALL" or "ANY")
            || dto.Rules.Select(rule => rule.OrderIndex).Distinct().Count() != dto.Rules.Count)
        {
            return new ProblemDetails
            {
                Title = "Thông tin huy hiệu hoặc danh sách quy tắc không hợp lệ.",
                Status = 400
            };
        }

        foreach (var rule in dto.Rules)
        {
            var type = rule.RuleType.ToLowerInvariant();
            if (!SupportedRuleTypes.Contains(type)
                || rule.ThresholdValue < 0
                || (ThresholdRuleTypes.Contains(type)
                    && (!rule.ThresholdValue.HasValue || rule.ThresholdValue.Value <= 0)))
            {
                return new ProblemDetails
                {
                    Title = $"Quy tắc huy hiệu không được hỗ trợ: {rule.RuleType}.",
                    Status = 400
                };
            }

            if (type == "complete_chapter"
                && (!rule.TargetChapterId.HasValue
                    || !await _unitOfWork.Chapters.Query().AnyAsync(chapter =>
                        chapter.Id == rule.TargetChapterId && !chapter.IsDeleted)))
            {
                return new ProblemDetails
                {
                    Title = "Quy tắc hoàn thành chương phải trỏ đến chương hợp lệ.",
                    Status = 400
                };
            }

            if (rule.TargetQuizId.HasValue
                && !await _unitOfWork.Quizzes.Query().AnyAsync(quiz =>
                    quiz.Id == rule.TargetQuizId && !quiz.IsDeleted))
            {
                return new ProblemDetails
                {
                    Title = "TargetQuizId không hợp lệ.",
                    Status = 400
                };
            }
        }

        return null;
    }

    private static BadgeRule MapRule(Guid badgeId, BadgeRuleUpsertDto dto) => new()
    {
        BadgeId = badgeId,
        RuleType = dto.RuleType.ToLowerInvariant(),
        TargetChapterId = dto.TargetChapterId,
        TargetQuizId = dto.TargetQuizId,
        ThresholdValue = dto.ThresholdValue,
        OrderIndex = dto.OrderIndex,
        Parameters = dto.Parameters
    };
}
