using MathIBook.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MathIBook.Api.Controllers;

[Route("api/dashboard")]
[ApiController]
[Authorize(Roles = "Student")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyDashboard()
    {
        try
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _dashboardService.GetDashboardAsync(userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ProblemDetails
            {
                Title = "Error fetching dashboard",
                Detail = ex.Message,
                Status = 500
            });
        }
    }
}
