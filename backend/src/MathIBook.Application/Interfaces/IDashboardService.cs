using MathIBook.Application.DTOs;

namespace MathIBook.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardDto> GetDashboardAsync(Guid userId);
}
