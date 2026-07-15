using MathIBook.Application.DTOs;

namespace MathIBook.Application.Interfaces;

public interface IProfileService
{
    Task<StudentProfileDto> GetAsync(Guid userId);
    Task<StudentProfileDto> UpdateAsync(Guid userId, UpdateProfileDto dto);
    Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto);
}
