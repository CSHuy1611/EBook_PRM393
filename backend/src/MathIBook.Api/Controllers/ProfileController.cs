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
    private readonly IWebHostEnvironment _env;

    public ProfileController(IProfileService profileService, IWebHostEnvironment env)
    {
        _profileService = profileService;
        _env = env;
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

    [HttpPost("avatar")]
    public async Task<ActionResult<StudentProfileDto>> UploadAvatar(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Tệp không hợp lệ",
                Detail = "Vui lòng chọn một tệp hình ảnh.",
                Status = 400
            });
        }

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Định dạng không hỗ trợ",
                Detail = "Chỉ chấp nhận các định dạng .jpg, .jpeg, .png, .gif.",
                Status = 400
            });
        }

        // Giới hạn 5MB
        if (file.Length > 5 * 1024 * 1024)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Kích thước tệp quá lớn",
                Detail = "Kích thước tệp không được vượt quá 5MB.",
                Status = 400
            });
        }

        var uploadsFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "avatars");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var uniqueFileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // URL tương đối cho Frontend
        var relativeUrl = $"/uploads/avatars/{uniqueFileName}";
        
        try
        {
            return Ok(await _profileService.UpdateAvatarAsync(CurrentUserId(), relativeUrl));
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Không thể cập nhật ảnh đại diện.",
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
