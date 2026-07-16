using MathIBook.Application.DTOs;
using MathIBook.Domain.Entities;
using MathIBook.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MathIBook.Api.Controllers.Admin;

[Route("api/admin/curriculum-topics")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminCurriculumTopicsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public AdminCurriculumTopicsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _unitOfWork.CurriculumTopics.Query()
            .OrderBy(topic => topic.Strand)
            .ThenBy(topic => topic.OrderIndex)
            .ToListAsync());
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CurriculumTopicUpsertDto dto)
    {
        var validation = await ValidateAsync(dto, null);
        if (validation is not null)
        {
            return BadRequest(validation);
        }

        var topic = new CurriculumTopic
        {
            Code = dto.Code.Trim().ToUpperInvariant(),
            Name = dto.Name.Trim(),
            Strand = dto.Strand,
            Grade = 8,
            OrderIndex = dto.OrderIndex,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };
        await _unitOfWork.CurriculumTopics.AddAsync(topic);
        await _unitOfWork.SaveChangesAsync();
        return Ok(topic);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CurriculumTopicUpsertDto dto)
    {
        var topic = await _unitOfWork.CurriculumTopics.GetByIdAsync(id);
        if (topic is null)
        {
            return NotFound();
        }

        var validation = await ValidateAsync(dto, id);
        if (validation is not null)
        {
            return BadRequest(validation);
        }

        topic.Code = dto.Code.Trim().ToUpperInvariant();
        topic.Name = dto.Name.Trim();
        topic.Strand = dto.Strand;
        topic.Grade = 8;
        topic.OrderIndex = dto.OrderIndex;
        topic.IsActive = dto.IsActive;
        _unitOfWork.CurriculumTopics.Update(topic);
        await _unitOfWork.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var topic = await _unitOfWork.CurriculumTopics.GetByIdAsync(id);
        if (topic is null)
        {
            return NotFound();
        }

        topic.IsActive = false;
        _unitOfWork.CurriculumTopics.Update(topic);
        await _unitOfWork.SaveChangesAsync();
        return NoContent();
    }

    private async Task<ProblemDetails?> ValidateAsync(
        CurriculumTopicUpsertDto dto,
        Guid? currentId)
    {
        if (string.IsNullOrWhiteSpace(dto.Code)
            || string.IsNullOrWhiteSpace(dto.Name)
            || dto.Code.Length > 50
            || dto.Name.Length > 200)
        {
            return new ProblemDetails
            {
                Title = "Mã và tên taxonomy Toán lớp 8 là bắt buộc.",
                Status = 400
            };
        }

        var normalized = dto.Code.Trim().ToUpperInvariant();
        if (await _unitOfWork.CurriculumTopics.Query().AnyAsync(
            topic => topic.Id != currentId && topic.Code == normalized))
        {
            return new ProblemDetails
            {
                Title = "Mã taxonomy đã tồn tại.",
                Status = 409
            };
        }

        return null;
    }
}
