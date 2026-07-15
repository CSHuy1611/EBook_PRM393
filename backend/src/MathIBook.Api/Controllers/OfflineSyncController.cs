using System.Security.Claims;
using MathIBook.Application.DTOs;
using MathIBook.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MathIBook.Api.Controllers;

[Route("api/sync")]
[ApiController]
[Authorize(Roles = "Student")]
public class OfflineSyncController : ControllerBase
{
    private readonly IQuizScoringService _quizScoringService;
    private readonly IProgressSyncService _progressSyncService;

    public OfflineSyncController(
        IQuizScoringService quizScoringService,
        IProgressSyncService progressSyncService)
    {
        _quizScoringService = quizScoringService;
        _progressSyncService = progressSyncService;
    }

    [HttpPost]
    public async Task<ActionResult<OfflineSyncResultDto>> Sync([FromBody] OfflineSyncDto dto)
    {
        if (dto.Attempts.Any(attempt => !attempt.ClientAttemptId.HasValue))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Mỗi attempt offline phải có ClientAttemptId.",
                Status = 400
            });
        }

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = new OfflineSyncResultDto();
        try
        {
            foreach (var attempt in dto.Attempts.OrderBy(item => item.ClientCreatedAt))
            {
                result.Attempts.Add(await _quizScoringService.ScoreQuizAsync(userId, attempt));
            }

            result.Progress = await _progressSyncService.SyncProgressAsync(userId, dto.Progress);
            return Ok(result);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Đồng bộ dữ liệu thất bại.",
                Detail = exception.Message,
                Status = 400
            });
        }
    }
}
