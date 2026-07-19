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
    // UnitOfWork cung cấp Users và CoinTransactions trên cùng DbContext/request.
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
        // Không cho page âm/0 và giới hạn pageSize để tránh request tải quá nhiều dòng.
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        // NameIdentifier do JWT tạo; client không được truyền userId để xem xu người khác.
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        // User.Coins là số dư hiện tại, tách với bảng lịch sử CoinTransactions.
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user is null)
        {
            return NotFound();
        }

        // Tạo IQueryable nhưng chưa chạy SQL để tiếp tục ghép count/order/pagination.
        var query = _unitOfWork.CoinTransactions.Query()
            .Where(transaction => transaction.UserId == userId);
        // Tổng số dòng giúp frontend biết khi nào đã tải hết trang.
        var totalItems = await query.CountAsync();
        var transactions = await query
            // Lịch sử mới nhất luôn xuất hiện trước.
            .OrderByDescending(transaction => transaction.CreatedAt)
            // Skip/Take được EF Core dịch thành OFFSET/LIMIT của PostgreSQL.
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            // Chỉ chọn trường DTO cần trả, không serialize cả entity/navigation property.
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

        // DTO trả cả số dư hiện tại và metadata phân trang trong một response.
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
