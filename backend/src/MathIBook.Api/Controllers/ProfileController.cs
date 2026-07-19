using System.Security.Claims;
using MathIBook.Application.DTOs;
using MathIBook.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MathIBook.Api.Controllers;

[Route("api/profile")]
[ApiController]
[Authorize(Roles = "Student")]
public class ProfileController : ControllerBase
{
    // Controller chỉ xử lý HTTP/JWT; thống kê và validation nằm trong ProfileService.
    private readonly IProfileService _profileService;

    public ProfileController(IProfileService profileService)
    {
        _profileService = profileService;
    }

    [HttpGet("me")]
    public async Task<ActionResult<StudentProfileDto>> GetMe()
    {
        // Không nhận userId từ query/body để Student chỉ đọc được hồ sơ của chính mình.
        return Ok(await _profileService.GetAsync(CurrentUserId()));
    }

    [HttpPut("me")]
    public async Task<ActionResult<StudentProfileDto>> Update(
        [FromBody] UpdateProfileDto dto)
    {
        try
        {
            // Service trả lại DTO hoàn chỉnh để frontend cập nhật state ngay sau PUT.
            return Ok(await _profileService.UpdateAsync(CurrentUserId(), dto));
        }
        catch (InvalidOperationException exception)
        {
            // Lỗi validation nghiệp vụ được chuẩn hóa thành ProblemDetails 400.
            return BadRequest(new ProblemDetails
            {
                Title = "Không thể cập nhật hồ sơ.",
                Detail = exception.Message,
                Status = 400
            });
        }
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        try
        {
            // Đổi mật khẩu thành công đồng thời làm mất hiệu lực refresh token cũ.
            await _profileService.ChangePasswordAsync(CurrentUserId(), dto);
            return Ok(new { message = "Đổi mật khẩu thành công. Vui lòng đăng nhập lại." });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Không thể đổi mật khẩu.",
                Detail = exception.Message,
                Status = 400
            });
        }
    }

    private Guid CurrentUserId() =>
        // NameIdentifier được phát hành trong JWT lúc đăng nhập.
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
