using System.Security.Claims;
using MathIBook.Application.DTOs;
using MathIBook.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MathIBook.Api.Controllers;

[Route("api/leaderboard")]
[ApiController]
[Authorize(Roles = "Student")]
public class LeaderboardController : ControllerBase
{
    // Controller đọc User và UserBadges thông qua UnitOfWork.
    private readonly IUnitOfWork _unitOfWork;

    public LeaderboardController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<LeaderboardDto>> Get()
    {
        // Dùng claim JWT để đánh dấu đúng dòng của Student đang đăng nhập.
        var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        // Chỉ tài khoản Student đang hoạt động mới được tham gia xếp hạng.
        var students = await _unitOfWork.Users.Query()
            .Where(user => user.Role == "Student" && user.IsActive)
            // Projection lấy User và số huy hiệu; EF Core tạo truy vấn COUNT tương ứng.
            .Select(user => new
            {
                User = user,
                BadgeCount = user.UserBadges.Count
            })
            // ToListAsync thực thi SQL; bước sort/rank phía dưới hiện chạy trong memory.
            .ToListAsync();

        var ranked = students
            // Tiêu chí chính: tổng xu cao hơn đứng trước.
            .OrderByDescending(item => item.User.Coins)
            // Nếu bằng xu, người có nhiều huy hiệu hơn đứng trước.
            .ThenByDescending(item => item.BadgeCount)
            // Name và Id là tie-breaker để thứ tự ổn định khi thành tích bằng nhau.
            .ThenBy(item => item.User.Name)
            .ThenBy(item => item.User.Id)
            // index bắt đầu từ 0 nên Rank phải cộng 1.
            .Select((item, index) => new LeaderboardEntryDto
            {
                Rank = index + 1,
                UserId = item.User.Id,
                Name = item.User.Name,
                AvatarUrl = item.User.AvatarUrl,
                Coins = item.User.Coins,
                BadgeCount = item.BadgeCount,
                // Cờ này cho frontend highlight chính Student đang xem.
                IsCurrentUser = item.User.Id == currentUserId
            })
            .ToList();

        // currentUser được trả riêng để UI vẫn hiển thị nếu người dùng ngoài Top 100.
        return Ok(new LeaderboardDto
        {
            Top100 = ranked.Take(100).ToList(),
            CurrentUser = ranked.FirstOrDefault(item => item.UserId == currentUserId),
            UpdatedAt = DateTime.UtcNow
        });
    }
}
