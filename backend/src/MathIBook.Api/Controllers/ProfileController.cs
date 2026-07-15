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
    private readonly IProfileService _profileService;

    public ProfileController(IProfileService profileService)
    {
        _profileService = profileService;
    }

    [HttpGet("me")]
    public async Task<ActionResult<StudentProfileDto>> GetMe()
    {
        return Ok(await _profileService.GetAsync(CurrentUserId()));
    }

    [HttpPut("me")]
    public async Task<ActionResult<StudentProfileDto>> Update(
        [FromBody] UpdateProfileDto dto)
    {
        try
        {
            return Ok(await _profileService.UpdateAsync(CurrentUserId(), dto));
        }
        catch (InvalidOperationException exception)
        {
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
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
