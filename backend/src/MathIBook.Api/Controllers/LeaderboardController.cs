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
    private readonly IUnitOfWork _unitOfWork;

    public LeaderboardController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<LeaderboardDto>> Get()
    {
        var currentUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var students = await _unitOfWork.Users.Query()
            .Where(user => user.Role == "Student" && user.IsActive)
            .Select(user => new
            {
                User = user,
                BadgeCount = user.UserBadges.Count
            })
            .ToListAsync();

        var ranked = students
            .OrderByDescending(item => item.User.Coins)
            .ThenByDescending(item => item.BadgeCount)
            .ThenBy(item => item.User.Name)
            .ThenBy(item => item.User.Id)
            .Select((item, index) => new LeaderboardEntryDto
            {
                Rank = index + 1,
                UserId = item.User.Id,
                Name = item.User.Name,
                AvatarUrl = item.User.AvatarUrl,
                Coins = item.User.Coins,
                BadgeCount = item.BadgeCount,
                IsCurrentUser = item.User.Id == currentUserId
            })
            .ToList();

        return Ok(new LeaderboardDto
        {
            Top100 = ranked.Take(100).ToList(),
            CurrentUser = ranked.FirstOrDefault(item => item.UserId == currentUserId),
            UpdatedAt = DateTime.UtcNow
        });
    }
}
