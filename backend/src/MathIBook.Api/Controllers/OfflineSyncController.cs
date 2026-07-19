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
    // QuizScoringService xử lý attempt; ProgressSyncService merge tiến độ bài học.
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
        // Offline attempt bắt buộc có id ổn định để retry không tạo điểm/xu trùng.
        if (dto.Attempts.Any(attempt => !attempt.ClientAttemptId.HasValue))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Mỗi attempt offline phải có ClientAttemptId.",
                Status = 400
            });
        }

        // UserId luôn lấy từ JWT, không tin user_id trong SQLite/client body.
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = new OfflineSyncResultDto();
        try
        {
            // Xử lý theo thời gian người học làm offline để giữ thứ tự streak/tiến độ.
            foreach (var attempt in dto.Attempts.OrderBy(item => item.ClientCreatedAt))
            {
                result.Attempts.Add(await _quizScoringService.ScoreQuizAsync(userId, attempt));
            }

            // Attempts được chấm trước để ProgressSyncService có dữ liệu server xác minh pass.
            result.Progress = await _progressSyncService.SyncProgressAsync(userId, dto.Progress);
            return Ok(result);
        }
        catch (InvalidOperationException exception)
        {
            // Validation nghiệp vụ trả ProblemDetails 400 để client giữ queue và retry/sửa dữ liệu.
            return BadRequest(new ProblemDetails
            {
                Title = "Đồng bộ dữ liệu thất bại.",
                Detail = exception.Message,
                Status = 400
            });
        }
    }
}
