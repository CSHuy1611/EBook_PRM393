using MathIBook.Application.DTOs;
using MathIBook.Domain.Entities;
using MathIBook.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MathIBook.Api.Controllers.Admin;

[Route("api/admin/reward-policies")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminRewardPoliciesController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public AdminRewardPoliciesController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _unitOfWork.RewardPolicies.Query()
            .OrderByDescending(policy => policy.EffectiveFrom)
            .ToListAsync());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var policy = await _unitOfWork.RewardPolicies.GetByIdAsync(id);
        return policy is null ? NotFound() : Ok(policy);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] RewardPolicyUpsertDto dto)
    {
        var validation = Validate(dto);
        if (validation is not null)
        {
            return BadRequest(validation);
        }

        var policy = new RewardPolicy();
        Apply(policy, dto);
        policy.EffectiveFrom = dto.EffectiveFrom == default
            ? DateTime.UtcNow
            : dto.EffectiveFrom.ToUniversalTime();
        policy.CreatedAt = DateTime.UtcNow;
        policy.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.RewardPolicies.AddAsync(policy);
        await _unitOfWork.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = policy.Id }, policy);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] RewardPolicyUpsertDto dto)
    {
        var validation = Validate(dto);
        if (validation is not null)
        {
            return BadRequest(validation);
        }

        var policy = await _unitOfWork.RewardPolicies.GetByIdAsync(id);
        if (policy is null)
        {
            return NotFound();
        }

        var isUsed = await _unitOfWork.Quizzes.Query()
            .AnyAsync(quiz => quiz.RewardPolicyId == id && !quiz.IsDeleted);
        if (isUsed && policy.QuizType != dto.QuizType)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Không thể đổi loại của chính sách đang được quiz sử dụng.",
                Status = 409
            });
        }

        Apply(policy, dto);
        policy.EffectiveFrom = dto.EffectiveFrom == default
            ? policy.EffectiveFrom
            : dto.EffectiveFrom.ToUniversalTime();
        policy.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.RewardPolicies.Update(policy);
        await _unitOfWork.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var policy = await _unitOfWork.RewardPolicies.GetByIdAsync(id);
        if (policy is null)
        {
            return NotFound();
        }

        policy.IsActive = false;
        policy.EffectiveTo ??= DateTime.UtcNow;
        policy.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.RewardPolicies.Update(policy);
        await _unitOfWork.SaveChangesAsync();
        return NoContent();
    }

    private static ProblemDetails? Validate(RewardPolicyUpsertDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)
            || dto.CoinsPerCorrectAnswer < 0
            || dto.FirstPassBonusCoins < 0
            || dto.PerfectScoreBonusCoins < 0
            || dto.ChapterCompletionBonusCoins < 0
            || dto.RetryRewardPercent is < 0 or > 100
            || dto.DailyCoinLimit < 0
            || (dto.EffectiveTo.HasValue
                && dto.EffectiveFrom != default
                && dto.EffectiveTo <= dto.EffectiveFrom))
        {
            return new ProblemDetails
            {
                Title = "Dữ liệu chính sách thưởng không hợp lệ.",
                Status = 400
            };
        }

        return null;
    }

    private static void Apply(RewardPolicy policy, RewardPolicyUpsertDto dto)
    {
        policy.Name = dto.Name.Trim();
        policy.QuizType = dto.QuizType;
        policy.CoinsPerCorrectAnswer = dto.CoinsPerCorrectAnswer;
        policy.FirstPassBonusCoins = dto.FirstPassBonusCoins;
        policy.PerfectScoreBonusCoins = dto.PerfectScoreBonusCoins;
        policy.ChapterCompletionBonusCoins = dto.ChapterCompletionBonusCoins;
        policy.RetryRewardPercent = dto.RetryRewardPercent;
        policy.DailyCoinLimit = dto.DailyCoinLimit;
        policy.EffectiveTo = dto.EffectiveTo?.ToUniversalTime();
        policy.IsActive = dto.IsActive;
    }
}
