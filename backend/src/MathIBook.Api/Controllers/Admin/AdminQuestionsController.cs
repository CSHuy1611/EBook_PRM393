using System.Text.Json;
using MathIBook.Application.DTOs;
using MathIBook.Domain.Entities;
using MathIBook.Domain.Enums;
using MathIBook.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using MathIBook.Application.Services;

namespace MathIBook.Api.Controllers.Admin;

[Route("api/admin/questions")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminQuestionsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IQuestionGeneratorService _generatorService;

    public AdminQuestionsController(IUnitOfWork unitOfWork, IQuestionGeneratorService generatorService)
    {
        _unitOfWork = unitOfWork;
        _generatorService = generatorService;
    }

    [HttpGet("lesson/{lessonId}")]
    public async Task<ActionResult<List<QuestionDto>>> GetByLesson(Guid lessonId)
    {
        var questions = await _unitOfWork.Questions.Query()
            .Where(question => question.LessonId == lessonId && !question.IsDeleted)
            .OrderBy(question => question.OrderIndex)
            .ToListAsync();
        return Ok(questions.Select(Map).ToList());
    }

    [HttpGet("chapter/{chapterId}")]
    public async Task<ActionResult<List<QuestionDto>>> GetByChapter(Guid chapterId)
    {
        var questions = await _unitOfWork.Questions.Query()
            .Where(question => question.ChapterId == chapterId && !question.IsDeleted)
            .OrderBy(question => question.OrderIndex)
            .ToListAsync();
        return Ok(questions.Select(Map).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<QuestionDto>> Create([FromBody] QuestionCreateDto dto)
    {
        var validation = Validate(dto.QuestionText, dto.Options, dto.CorrectOption);
        if (validation is not null)
        {
            return BadRequest(validation);
        }

        var lesson = await _unitOfWork.Lessons.GetByIdAsync(dto.LessonId);
        if (lesson is null || lesson.IsDeleted)
        {
            return NotFound(new ProblemDetails { Title = "Không tìm thấy bài học.", Status = 404 });
        }

        var question = new Question
        {
            LessonId = dto.LessonId,
            QuestionText = dto.QuestionText.Trim(),
            Options = JsonSerializer.Serialize(dto.Options.Select(option => option.Trim())),
            CorrectOption = dto.CorrectOption,
            Explanation = dto.Explanation,
            OrderIndex = dto.OrderIndex,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _unitOfWork.Questions.AddAsync(question);

        var quiz = await _unitOfWork.Quizzes.Query().FirstOrDefaultAsync(item =>
            item.LessonId == dto.LessonId
            && item.QuizType == QuizType.Lesson
            && !item.IsDeleted);
        if (quiz is not null)
        {
            var occupied = await _unitOfWork.QuizQuestions.Query()
                .AnyAsync(link =>
                    link.QuizId == quiz.Id && link.OrderIndex == dto.OrderIndex);
            if (occupied)
            {
                return Conflict(new ProblemDetails
                {
                    Title = "Thứ tự câu hỏi đã tồn tại trong quiz bài học.",
                    Status = 409
                });
            }

            await _unitOfWork.QuizQuestions.AddAsync(new QuizQuestion
            {
                QuizId = quiz.Id,
                QuestionId = question.Id,
                OrderIndex = dto.OrderIndex,
                Weight = 1
            });
            quiz.IsPublished = false;
            quiz.PublishedAt = null;
            quiz.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Quizzes.Update(quiz);
        }

        await _unitOfWork.SaveChangesAsync();
        return Ok(Map(question));
    }

    [HttpPost("auto-generate")]
    public async Task<IActionResult> AutoGenerate([FromBody] AutoGenerateQuestionsDto dto)
    {
        if (dto.Count <= 0 || dto.Count > 50) return BadRequest(new ProblemDetails { Title = "Số lượng câu hỏi không hợp lệ.", Status = 400 });

        List<QuestionCreateDto> questionsToCreate;
        int maxOrder = 0;
        Quiz? quiz = null;

        if (dto.LessonId.HasValue && dto.LessonId.Value != Guid.Empty)
        {
            var lesson = await _unitOfWork.Lessons.GetByIdAsync(dto.LessonId.Value);
            if (lesson is null || lesson.IsDeleted) return NotFound(new ProblemDetails { Title = "Không tìm thấy bài học.", Status = 404 });

            questionsToCreate = await _generatorService.GenerateQuestionsAsync(lesson.Id, null, lesson.Title, dto.Count, "Bài học");
            
            maxOrder = await _unitOfWork.Questions.Query()
                .Where(q => q.LessonId == lesson.Id && !q.IsDeleted)
                .MaxAsync(q => (int?)q.OrderIndex) ?? 0;

            quiz = await _unitOfWork.Quizzes.Query().FirstOrDefaultAsync(item =>
                item.LessonId == lesson.Id && item.QuizType == QuizType.Lesson && !item.IsDeleted);
        }
        else if (dto.ChapterId.HasValue && dto.ChapterId.Value != Guid.Empty)
        {
            var chapter = await _unitOfWork.Chapters.GetByIdAsync(dto.ChapterId.Value);
            if (chapter is null || chapter.IsDeleted) return NotFound(new ProblemDetails { Title = "Không tìm thấy chương.", Status = 404 });

            questionsToCreate = await _generatorService.GenerateQuestionsAsync(null, chapter.Id, chapter.Title, dto.Count, "Chương");
            
            maxOrder = await _unitOfWork.Questions.Query()
                .Where(q => q.ChapterId == chapter.Id && !q.IsDeleted)
                .MaxAsync(q => (int?)q.OrderIndex) ?? 0;

            quiz = await _unitOfWork.Quizzes.Query().FirstOrDefaultAsync(item =>
                item.ChapterId == chapter.Id && item.QuizType == QuizType.Chapter && !item.IsDeleted);
        }
        else
        {
            return BadRequest(new ProblemDetails { Title = "Vui lòng cung cấp LessonId hoặc ChapterId.", Status = 400 });
        }

        if (!questionsToCreate.Any()) return BadRequest(new ProblemDetails { Title = "Không tạo được câu hỏi nào.", Status = 400 });

        var createdQuestions = new List<Question>();

        foreach (var qDto in questionsToCreate)
        {
            maxOrder++;
            var question = new Question
            {
                LessonId = qDto.LessonId != Guid.Empty ? qDto.LessonId : null,
                ChapterId = qDto.ChapterId,
                QuestionText = qDto.QuestionText,
                Options = JsonSerializer.Serialize(qDto.Options),
                CorrectOption = qDto.CorrectOption,
                Explanation = qDto.Explanation,
                OrderIndex = maxOrder,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Questions.AddAsync(question);
            createdQuestions.Add(question);
        }

        await _unitOfWork.SaveChangesAsync();

        if (quiz is not null)
        {
            foreach (var q in createdQuestions)
            {
                await _unitOfWork.QuizQuestions.AddAsync(new QuizQuestion
                {
                    QuizId = quiz.Id,
                    QuestionId = q.Id,
                    OrderIndex = q.OrderIndex,
                    Weight = 1
                });
            }
            quiz.IsPublished = false;
            quiz.PublishedAt = null;
            quiz.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Quizzes.Update(quiz);
            await _unitOfWork.SaveChangesAsync();
        }

        return Ok(createdQuestions.Select(Map).ToList());
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] QuestionUpdateDto dto)
    {
        var validation = Validate(dto.QuestionText, dto.Options, dto.CorrectOption);
        if (validation is not null)
        {
            return BadRequest(validation);
        }

        var question = await _unitOfWork.Questions.GetByIdAsync(id);
        if (question is null || question.IsDeleted)
        {
            return NotFound();
        }

        question.QuestionText = dto.QuestionText.Trim();
        question.Options = JsonSerializer.Serialize(dto.Options.Select(option => option.Trim()));
        question.CorrectOption = dto.CorrectOption;
        question.Explanation = dto.Explanation;
        question.OrderIndex = dto.OrderIndex;
        question.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Questions.Update(question);
        await UnpublishLinkedQuizzesAsync(id);
        await _unitOfWork.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var question = await _unitOfWork.Questions.GetByIdAsync(id);
        if (question is null || question.IsDeleted)
        {
            return NotFound();
        }

        question.IsDeleted = true;
        question.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Questions.Update(question);
        await UnpublishLinkedQuizzesAsync(id);
        await _unitOfWork.SaveChangesAsync();
        return NoContent();
    }

    private async Task UnpublishLinkedQuizzesAsync(Guid questionId)
    {
        var quizzes = await _unitOfWork.QuizQuestions.Query()
            .Where(link => link.QuestionId == questionId)
            .Select(link => link.Quiz)
            .ToListAsync();
        foreach (var quiz in quizzes)
        {
            quiz.IsPublished = false;
            quiz.PublishedAt = null;
            quiz.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Quizzes.Update(quiz);
        }
    }

    private static ProblemDetails? Validate(
        string questionText,
        IReadOnlyCollection<string> options,
        int correctOption)
    {
        if (string.IsNullOrWhiteSpace(questionText)
            || options.Count != 4
            || options.Any(string.IsNullOrWhiteSpace)
            || correctOption is < 0 or > 3)
        {
            return new ProblemDetails
            {
                Title = "Câu hỏi phải có nội dung, đúng 4 lựa chọn và đáp án đúng từ 0 đến 3.",
                Status = 400
            };
        }

        return null;
    }

    private static QuestionDto Map(Question question) => new()
    {
        Id = question.Id,
        LessonId = question.LessonId,
        ChapterId = question.ChapterId,
        QuestionText = question.QuestionText,
        Options = JsonSerializer.Deserialize<List<string>>(question.Options) ?? new(),
        CorrectOption = question.CorrectOption,
        Explanation = question.Explanation,
        OrderIndex = question.OrderIndex
    };
}
