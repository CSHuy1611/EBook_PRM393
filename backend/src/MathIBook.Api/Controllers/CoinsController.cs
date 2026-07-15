using System.Security.Claims;
using MathIBook.Application.DTOs;
using MathIBook.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MathIBook.Api.Controllers;

[Route("api/coins")]
[ApiController]
[Authorize(Roles = "Student")]
public class CoinsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public CoinsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<CoinHistoryDto>> GetHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user is null)
        {
            return NotFound();
        }

        var query = _unitOfWork.CoinTransactions.Query()
            .Where(transaction => transaction.UserId == userId);
        var totalItems = await query.CountAsync();
        var transactions = await query
            .OrderByDescending(transaction => transaction.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(transaction => new CoinTransactionDto
            {
                Amount = transaction.Amount,
                SourceType = transaction.SourceType,
                SourceId = transaction.SourceId,
                BalanceAfter = transaction.BalanceAfter,
                Description = transaction.Description,
                CreatedAt = transaction.CreatedAt
            })
            .ToListAsync();

        return Ok(new CoinHistoryDto
        {
            TotalCoins = user.Coins,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            Items = transactions
        });
    }
}
