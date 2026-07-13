using MathIBook.Application.DTOs;
using MathIBook.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MathIBook.Api.Controllers;

[Route("api/progress")]
[ApiController]
[Authorize(Roles = "Student")]
public class ProgressController : ControllerBase
{
    private readonly IProgressSyncService _progressSyncService;

    public ProgressController(IProgressSyncService progressSyncService)
    {
        _progressSyncService = progressSyncService;
    }

    [HttpPost("sync")]
    public async Task<IActionResult> Sync([FromBody] ProgressSyncDto dto)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _progressSyncService.SyncProgressAsync(userId, dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ProblemDetails
            {
                Title = "Error syncing progress",
                Detail = ex.Message,
                Status = 500
            });
        }
    }
}
