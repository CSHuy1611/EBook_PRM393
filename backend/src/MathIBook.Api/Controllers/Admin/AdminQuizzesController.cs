using System.Security.Claims;
using System.Text.Json;
using MathIBook.Application.DTOs;
using MathIBook.Application.Interfaces;
using MathIBook.Domain.Entities;
using MathIBook.Domain.Enums;
using MathIBook.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MathIBook.Api.Controllers.Admin;

[Route("api/admin/quizzes")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminQuizzesController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IContentValidationService _validationService;

    public AdminQuizzesController(
        IUnitOfWork unitOfWork,
        IContentValidationService validationService)
    {
        _unitOfWork = unitOfWork;
        _validationService = validationService;
    }

    [HttpGet]
    public async Task<ActionResult<List<AdminQuizDto>>> GetAll(
        [FromQuery] Guid? lessonId,
        [FromQuery] Guid? chapterId)
    {
        var query = QuizQuery().Where(quiz => !quiz.IsDeleted);
        if (lessonId.HasValue)
        {
            query = query.Where(quiz => quiz.LessonId == lessonId);
        }

        if (chapterId.HasValue)
        {
            query = query.Where(quiz => quiz.ChapterId == chapterId);
        }

        var quizzes = await query
            .OrderBy(quiz => quiz.QuizType)
            .ThenBy(quiz => quiz.Title)
            .ToListAsync();
        return Ok(quizzes.Select(Map).ToList());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AdminQuizDto>> GetById(Guid id)
    {
        var quiz = await QuizQuery()
            .FirstOrDefaultAsync(item => item.Id == id && !item.IsDeleted);
        return quiz is null
            ? NotFound(new ProblemDetails { Title = "Không tìm thấy quiz.", Status = 404 })
            : Ok(Map(quiz));
    }

    [HttpPost]
    public async Task<ActionResult<AdminQuizDto>> Create([FromBody] AdminQuizUpsertDto dto)
    {
        var targetError = await ValidateTargetAsync(dto, null);
        if (targetError is not null)
        {
            return BadRequest(targetError);
        }

        var quiz = new Quiz
        {
            QuizType = dto.QuizType,
            LessonId = dto.LessonId,
            ChapterId = dto.ChapterId,
            RewardPolicyId = dto.RewardPolicyId,
            Title = dto.Title.Trim(),
            PassScore = dto.PassScore,
            DurationSeconds = dto.DurationSeconds,
            FirstPassCoins = dto.FirstPassCoins,
            IsPublished = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _unitOfWork.Quizzes.AddAsync(quiz);
        await AddAuditAsync("Quiz", quiz.Id, "Create", null, Snapshot(quiz));
        await _unitOfWork.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = quiz.Id }, Map(quiz));
    }

    [HttpPost("generate")]
    public async Task<ActionResult<AdminQuizDto>> Generate([FromBody] AdminQuizGenerateDto dto)
    {
        if (!dto.LessonId.HasValue && !dto.ChapterId.HasValue)
        {
            return BadRequest(new ProblemDetails { Title = "Phải cung cấp LessonId hoặc ChapterId.", Status = 400 });
        }

        var quizType = dto.LessonId.HasValue ? QuizType.Lesson : QuizType.Chapter;

        string title = !string.IsNullOrWhiteSpace(dto.Title) ? dto.Title.Trim() : "Bài Trắc Nghiệm";
        
        // Nếu không cung cấp tiêu đề, tự sinh tiêu đề theo tên bài học/chương
        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            if (dto.LessonId.HasValue)
            {
                var lesson = await _unitOfWork.Lessons.GetByIdAsync(dto.LessonId.Value);
                if (lesson == null || lesson.IsDeleted) return NotFound(new ProblemDetails { Title = "Không tìm thấy bài học.", Status = 404 });
                title = $"Quiz Bài Học: {lesson.Title}";
            }
            else if (dto.ChapterId.HasValue)
            {
                var chapter = await _unitOfWork.Chapters.GetByIdAsync(dto.ChapterId.Value);
                if (chapter == null || chapter.IsDeleted) return NotFound(new ProblemDetails { Title = "Không tìm thấy chương.", Status = 404 });
                title = $"Quiz Chương: {chapter.Title}";
            }
        }

        var qQuery = _unitOfWork.Questions.Query().Where(q => !q.IsDeleted);
        qQuery = dto.LessonId.HasValue
            ? qQuery.Where(q => q.LessonId == dto.LessonId)
            : qQuery.Where(q => q.ChapterId == dto.ChapterId);

        var allQuestions = await qQuery.ToListAsync();
        int finalQuestionCount = Math.Min(dto.QuestionCount, allQuestions.Count);
        
        if (finalQuestionCount == 0)
        {
            return BadRequest(new ProblemDetails { Title = "Không có câu hỏi nào trong ngân hàng cho phạm vi này.", Status = 400 });
        }

        var randomQuestions = allQuestions.OrderBy(x => Guid.NewGuid()).Take(finalQuestionCount).ToList();

        var quiz = new Quiz
        {
            QuizType = quizType,
            LessonId = dto.LessonId,
            ChapterId = dto.ChapterId,
            Title = title,
            PassScore = dto.PassScore,
            DurationSeconds = dto.DurationSeconds,
            IsPublished = false, // Tạo mới mặc định là tắt, Admin sẽ gạt công tắc sau
            PublishedAt = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Quizzes.AddAsync(quiz);

        int orderIndex = 1;
        foreach (var q in randomQuestions)
        {
            await _unitOfWork.QuizQuestions.AddAsync(new QuizQuestion
            {
                QuizId = quiz.Id,
                QuestionId = q.Id,
                OrderIndex = orderIndex++,
                Weight = 1
            });
        }

        await AddAuditAsync("Quiz", quiz.Id, "Generate", null, Snapshot(quiz));
        await _unitOfWork.SaveChangesAsync();
        
        var generatedQuiz = await _unitOfWork.Quizzes.Query()
            .Include(q => q.QuizQuestions)
            .ThenInclude(qq => qq.Question)
            .FirstOrDefaultAsync(q => q.Id == quiz.Id);

        return Ok(Map(generatedQuiz ?? quiz));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] AdminQuizUpsertDto dto)
    {
        var quiz = await _unitOfWork.Quizzes.GetByIdAsync(id);
        if (quiz is null || quiz.IsDeleted)
        {
            return NotFound(new ProblemDetails { Title = "Không tìm thấy quiz.", Status = 404 });
        }

        var existingQuestions = await _unitOfWork.QuizQuestions.Query()
            .Where(qq => qq.QuizId == id)
            .ToListAsync();
        
        bool isChangingQuestionCount = dto.QuestionCount.HasValue && dto.QuestionCount.Value != existingQuestions.Count;

        var hasAttempts = await _unitOfWork.QuizAttempts.Query()
            .AnyAsync(attempt => attempt.QuizId == id);
        if (hasAttempts
            && (quiz.QuizType != dto.QuizType
                || quiz.LessonId != dto.LessonId
                || quiz.ChapterId != dto.ChapterId
                || isChangingQuestionCount))
        {
            return Conflict(new ProblemDetails
            {
                Title = "Không thể đổi phạm vi hoặc số lượng câu hỏi của quiz đã có lịch sử làm bài.",
                Status = 409
            });
        }

        var targetError = await ValidateTargetAsync(dto, id);
        if (targetError is not null)
        {
            return BadRequest(targetError);
        }

        var before = Snapshot(quiz);
        quiz.QuizType = dto.QuizType;
        quiz.LessonId = dto.LessonId;
        quiz.ChapterId = dto.ChapterId;
        quiz.RewardPolicyId = dto.RewardPolicyId;
        quiz.Title = dto.Title.Trim();
        quiz.PassScore = dto.PassScore;
        quiz.DurationSeconds = dto.DurationSeconds;
        quiz.FirstPassCoins = dto.FirstPassCoins;
        quiz.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Quizzes.Update(quiz);
        await AddAuditAsync("Quiz", quiz.Id, "Update", before, Snapshot(quiz));

        if (isChangingQuestionCount)
        {
            foreach (var existing in existingQuestions)
            {
                _unitOfWork.QuizQuestions.Remove(existing);
            }
            await _unitOfWork.SaveChangesAsync();
            
            var qQuery = _unitOfWork.Questions.Query().Where(q => !q.IsDeleted);
            qQuery = dto.LessonId.HasValue
                ? qQuery.Where(q => q.LessonId == dto.LessonId)
                : qQuery.Where(q => q.ChapterId == dto.ChapterId);
                
            var allQuestions = await qQuery.ToListAsync();
            int finalQuestionCount = Math.Min(dto.QuestionCount!.Value, allQuestions.Count);
            
            var randomQuestions = allQuestions.OrderBy(x => Guid.NewGuid()).Take(finalQuestionCount).ToList();
            
            foreach (var q in randomQuestions)
            {
                var qq = new QuizQuestion
                {
                    QuizId = quiz.Id,
                    QuestionId = q.Id,
                    OrderIndex = randomQuestions.IndexOf(q) + 1,
                    Weight = 1
                };
                await _unitOfWork.QuizQuestions.AddAsync(qq);
            }
        }

        await _unitOfWork.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var quiz = await _unitOfWork.Quizzes.GetByIdAsync(id);
        if (quiz is null || quiz.IsDeleted)
        {
            return NotFound(new ProblemDetails { Title = "Không tìm thấy quiz.", Status = 404 });
        }

        var before = Snapshot(quiz);
        quiz.IsDeleted = true;
        quiz.IsPublished = false;
        quiz.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Quizzes.Update(quiz);
        await AddAuditAsync("Quiz", quiz.Id, "SoftDelete", before, Snapshot(quiz));
        await _unitOfWork.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{quizId}/questions")]
    public async Task<ActionResult<QuestionDto>> AddQuestion(
        Guid quizId,
        [FromBody] QuizQuestionUpsertDto dto)
    {
        var quiz = await _unitOfWork.Quizzes.GetByIdAsync(quizId);
        if (quiz is null || quiz.IsDeleted)
        {
            return NotFound(new ProblemDetails { Title = "Không tìm thấy quiz.", Status = 404 });
        }

        Question question;
        if (dto.QuestionId.HasValue)
        {
            question = await _unitOfWork.Questions.GetByIdAsync(dto.QuestionId.Value)
                ?? throw new InvalidOperationException("Không tìm thấy câu hỏi.");
            if (question.IsDeleted || !await QuestionBelongsToQuiz(question, quiz))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Câu hỏi không thuộc phạm vi của quiz.",
                    Status = 400
                });
            }
        }
        else
        {
            var validation = ValidateQuestion(dto);
            if (validation is not null)
            {
                return BadRequest(validation);
            }

            question = new Question
            {
                LessonId = quiz.QuizType == QuizType.Lesson ? quiz.LessonId : null,
                ChapterId = quiz.QuizType == QuizType.Chapter ? quiz.ChapterId : null,
                QuestionText = dto.QuestionText.Trim(),
                Options = JsonSerializer.Serialize(dto.Options.Select(option => option.Trim())),
                CorrectOption = dto.CorrectOption,
                Explanation = dto.Explanation,
                OrderIndex = dto.OrderIndex,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Questions.AddAsync(question);
        }

        var exists = await _unitOfWork.QuizQuestions.Query()
            .AnyAsync(link => link.QuizId == quizId && link.QuestionId == question.Id);
        if (exists)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Câu hỏi đã có trong quiz.",
                Status = 409
            });
        }

        var orderOccupied = await _unitOfWork.QuizQuestions.Query()
            .AnyAsync(link => link.QuizId == quizId && link.OrderIndex == dto.OrderIndex);
        if (orderOccupied)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Thứ tự câu hỏi đã được sử dụng.",
                Status = 409
            });
        }

        await _unitOfWork.QuizQuestions.AddAsync(new QuizQuestion
        {
            QuizId = quizId,
            QuestionId = question.Id,
            OrderIndex = dto.OrderIndex,
            Weight = Math.Max(1, dto.Weight)
        });
        quiz.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Quizzes.Update(quiz);
        await AddAuditAsync("Quiz", quiz.Id, "AddQuestion", null, new { question.Id });
        await _unitOfWork.SaveChangesAsync();
        return Ok(MapQuestion(question));
    }

    [HttpPut("{quizId}/questions/{questionId}")]
    public async Task<IActionResult> UpdateQuestion(
        Guid quizId,
        Guid questionId,
        [FromBody] QuizQuestionUpsertDto dto)
    {
        var link = await _unitOfWork.QuizQuestions.Query()
            .Include(item => item.Quiz)
            .Include(item => item.Question)
            .FirstOrDefaultAsync(item =>
                item.QuizId == quizId && item.QuestionId == questionId);
        if (link is null || link.Quiz.IsDeleted)
        {
            return NotFound(new ProblemDetails { Title = "Không tìm thấy câu hỏi trong quiz.", Status = 404 });
        }

        var validation = ValidateQuestion(dto);
        if (validation is not null)
        {
            return BadRequest(validation);
        }

        var orderOccupied = await _unitOfWork.QuizQuestions.Query()
            .AnyAsync(item =>
                item.QuizId == quizId
                && item.QuestionId != questionId
                && item.OrderIndex == dto.OrderIndex);
        if (orderOccupied)
        {
            return Conflict(new ProblemDetails { Title = "Thứ tự câu hỏi đã được sử dụng.", Status = 409 });
        }

        link.Question.QuestionText = dto.QuestionText.Trim();
        link.Question.Options = JsonSerializer.Serialize(dto.Options.Select(option => option.Trim()));
        link.Question.CorrectOption = dto.CorrectOption;
        link.Question.Explanation = dto.Explanation;
        link.Question.OrderIndex = dto.OrderIndex;
        link.Question.UpdatedAt = DateTime.UtcNow;
        link.OrderIndex = dto.OrderIndex;
        link.Weight = Math.Max(1, dto.Weight);
        link.Quiz.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Questions.Update(link.Question);
        _unitOfWork.QuizQuestions.Update(link);
        _unitOfWork.Quizzes.Update(link.Quiz);
        await AddAuditAsync("Quiz", quizId, "UpdateQuestion", null, new { questionId });
        await _unitOfWork.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{quizId}/questions/{questionId}")]
    public async Task<IActionResult> RemoveQuestion(Guid quizId, Guid questionId)
    {
        var link = await _unitOfWork.QuizQuestions.Query()
            .Include(item => item.Quiz)
            .FirstOrDefaultAsync(item =>
                item.QuizId == quizId && item.QuestionId == questionId);
        if (link is null)
        {
            return NotFound();
        }

        _unitOfWork.QuizQuestions.Remove(link);
        link.Quiz.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Quizzes.Update(link.Quiz);
        await AddAuditAsync("Quiz", quizId, "RemoveQuestion", new { questionId }, null);
        await _unitOfWork.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("{quizId}/questions/reorder")]
    public async Task<IActionResult> Reorder(
        Guid quizId,
        [FromBody] List<QuizQuestionOrderDto> dto)
    {
        if (dto.Select(item => item.OrderIndex).Distinct().Count() != dto.Count
            || dto.Select(item => item.QuestionId).Distinct().Count() != dto.Count)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "QuestionId và OrderIndex không được trùng.",
                Status = 400
            });
        }

        var links = await _unitOfWork.QuizQuestions.Query()
            .Where(link => link.QuizId == quizId)
            .ToListAsync();
        if (links.Count != dto.Count
            || links.Any(link => dto.All(item => item.QuestionId != link.QuestionId)))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Danh sách sắp xếp phải chứa đúng toàn bộ câu hỏi của quiz.",
                Status = 400
            });
        }

        // Move to a temporary range first so swapping two values cannot violate
        // the unique (QuizId, OrderIndex) constraint between SQL statements.
        for (var index = 0; index < links.Count; index++)
        {
            links[index].OrderIndex = 1_000_000 + index;
            _unitOfWork.QuizQuestions.Update(links[index]);
        }
        await _unitOfWork.SaveChangesAsync();

        foreach (var item in dto)
        {
            var link = links.First(candidate => candidate.QuestionId == item.QuestionId);
            link.OrderIndex = item.OrderIndex;
            _unitOfWork.QuizQuestions.Update(link);
        }

        await _unitOfWork.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id}/validation")]
    public async Task<ActionResult<ContentValidationResultDto>> Validate(Guid id)
    {
        var data = await BuildValidationAsync(id);
        return data is null
            ? NotFound(new ProblemDetails { Title = "Không tìm thấy quiz.", Status = 404 })
            : Ok(data);
    }

    [HttpPatch("{id}/publish")]
    public async Task<IActionResult> TogglePublish(Guid id)
    {
        var quiz = await QuizQuery()
            .FirstOrDefaultAsync(item => item.Id == id && !item.IsDeleted);
        if (quiz is null)
        {
            return NotFound(new ProblemDetails { Title = "Không tìm thấy quiz.", Status = 404 });
        }

        if (quiz.IsPublished)
        {
            var before = Snapshot(quiz);
            quiz.IsPublished = false;
            quiz.PublishedAt = null;
            quiz.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Quizzes.Update(quiz);
            await AddAuditAsync("Quiz", id, "Unpublish", before, Snapshot(quiz));
            await _unitOfWork.SaveChangesAsync();
            return NoContent();
        }

        var validation = await BuildValidationAsync(id);
        if (validation is null || !validation.IsValid)
        {
            return BadRequest(validation);
        }

        var targetValid = await TargetBelongsToPublishedGrade8ContentAsync(quiz);
        if (!targetValid)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Quiz chỉ được xuất bản khi nội dung đích đã xuất bản và thuộc Toán lớp 8.",
                Status = 400
            });
        }

        var beforePublish = Snapshot(quiz);
        quiz.IsPublished = true;
        quiz.PublishedAt = DateTime.UtcNow;
        quiz.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Quizzes.Update(quiz);
        
        // Đảm bảo chỉ 1 Quiz được Publish cho 1 bài học/chương
        var otherQuizzesQuery = _unitOfWork.Quizzes.Query().Where(q => q.Id != id && !q.IsDeleted && q.IsPublished);
        if (quiz.QuizType == QuizType.Lesson)
        {
            otherQuizzesQuery = otherQuizzesQuery.Where(q => q.LessonId == quiz.LessonId && q.QuizType == QuizType.Lesson);
        }
        else
        {
            otherQuizzesQuery = otherQuizzesQuery.Where(q => q.ChapterId == quiz.ChapterId && q.QuizType == QuizType.Chapter);
        }
        var otherQuizzes = await otherQuizzesQuery.ToListAsync();
        foreach (var otherQuiz in otherQuizzes)
        {
            otherQuiz.IsPublished = false;
            otherQuiz.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Quizzes.Update(otherQuiz);
        }

        await AddAuditAsync("Quiz", id, "Publish", beforePublish, Snapshot(quiz));
        await _unitOfWork.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id}/stats")]
    public async Task<IActionResult> GetStats(Guid id)
    {
        var exists = await _unitOfWork.Quizzes.Query()
            .AnyAsync(quiz => quiz.Id == id && !quiz.IsDeleted);
        if (!exists)
        {
            return NotFound();
        }

        var attempts = await _unitOfWork.QuizAttempts.Query()
            .Where(attempt => attempt.QuizId == id)
            .Include(attempt => attempt.User)
            .OrderByDescending(attempt => attempt.CreatedAt)
            .ToListAsync();
        var retryCount = attempts
            .GroupBy(attempt => attempt.UserId)
            .Sum(group => Math.Max(0, group.Count() - 1));

        return Ok(new
        {
            totalAttempts = attempts.Count,
            uniqueStudents = attempts.Select(attempt => attempt.UserId).Distinct().Count(),
            passRate = attempts.Count == 0
                ? 0
                : Math.Round((double)attempts.Count(attempt => attempt.IsPassed) / attempts.Count * 100, 2),
            averageScore = attempts.Count == 0
                ? 0
                : Math.Round(attempts.Average(attempt => (double)attempt.Score10), 2),
            retryCount,
            history = attempts.Take(100).Select(attempt => new
            {
                attempt.Id,
                attempt.UserId,
                studentName = attempt.User.Name,
                attempt.Score10,
                attempt.IsPassed,
                attempt.CoinsEarned,
                attempt.DurationSeconds,
                attempt.CreatedAt
            })
        });
    }

    private IQueryable<Quiz> QuizQuery() =>
        _unitOfWork.Quizzes.Query()
            .Include(quiz => quiz.QuizQuestions.OrderBy(link => link.OrderIndex))
            .ThenInclude(link => link.Question);

    private async Task<ProblemDetails?> ValidateTargetAsync(
        AdminQuizUpsertDto dto,
        Guid? currentQuizId)
    {
        if (dto.PassScore is < 0 or > 10
            || dto.DurationSeconds <= 0
            || dto.FirstPassCoins < 0)
        {
            return new ProblemDetails
            {
                Title = "Điểm đạt, thời lượng hoặc xu thưởng không hợp lệ.",
                Status = 400
            };
        }

        var targetValid = dto.QuizType switch
        {
            QuizType.Lesson => dto.LessonId.HasValue && !dto.ChapterId.HasValue,
            QuizType.Chapter => dto.ChapterId.HasValue && !dto.LessonId.HasValue,
            _ => false
        };
        if (!targetValid || string.IsNullOrWhiteSpace(dto.Title))
        {
            return new ProblemDetails
            {
                Title = "Quiz phải có tiêu đề và thuộc đúng một bài học hoặc chương.",
                Status = 400
            };
        }

        var targetExists = dto.QuizType == QuizType.Lesson
            ? await _unitOfWork.Lessons.Query().AnyAsync(
                lesson => lesson.Id == dto.LessonId && !lesson.IsDeleted)
            : await _unitOfWork.Chapters.Query().AnyAsync(
                chapter => chapter.Id == dto.ChapterId && !chapter.IsDeleted);
        if (!targetExists)
        {
            return new ProblemDetails { Title = "Không tìm thấy nội dung đích.", Status = 400 };
        }

        if (dto.RewardPolicyId.HasValue)
        {
            var policy = await _unitOfWork.RewardPolicies.GetByIdAsync(dto.RewardPolicyId.Value);
            if (policy is null || policy.QuizType != dto.QuizType)
            {
                return new ProblemDetails
                {
                    Title = "Chính sách thưởng không tồn tại hoặc sai loại quiz.",
                    Status = 400
                };
            }
        }

        return null;
    }

    private static ProblemDetails? ValidateQuestion(QuizQuestionUpsertDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.QuestionText)
            || dto.Options.Count != 4
            || dto.Options.Any(string.IsNullOrWhiteSpace)
            || dto.CorrectOption is < 0 or > 3
            || dto.Weight <= 0)
        {
            return new ProblemDetails
            {
                Title = "Câu hỏi phải có nội dung, đúng 4 lựa chọn, đáp án đúng và trọng số dương.",
                Status = 400
            };
        }

        return null;
    }

    private async Task<ContentValidationResultDto?> BuildValidationAsync(Guid quizId)
    {
        var quiz = await QuizQuery()
            .FirstOrDefaultAsync(item => item.Id == quizId && !item.IsDeleted);
        if (quiz is null)
        {
            return null;
        }

        var chapterLessonIds = quiz.ChapterId.HasValue
            ? await _unitOfWork.Lessons.Query()
                .Where(lesson => lesson.ChapterId == quiz.ChapterId && !lesson.IsDeleted)
                .Select(lesson => lesson.Id)
                .ToListAsync()
            : new List<Guid>();
        var questions = quiz.QuizQuestions
            .Where(link => !link.Question.IsDeleted)
            .Select(link => link.Question)
            .ToList();
        return _validationService.ValidateQuiz(quiz, questions, chapterLessonIds);
    }

    private async Task<bool> TargetBelongsToPublishedGrade8ContentAsync(Quiz quiz)
    {
        if (quiz.QuizType == QuizType.Lesson)
        {
            return await _unitOfWork.Lessons.Query()
                .AnyAsync(lesson =>
                    lesson.Id == quiz.LessonId
                    && lesson.IsPublished
                    && !lesson.IsDeleted);
        }

        return await _unitOfWork.Chapters.Query()
            .AnyAsync(chapter =>
                chapter.Id == quiz.ChapterId
                && chapter.IsPublished
                && !chapter.IsDeleted);
    }

    private async Task<bool> QuestionBelongsToQuiz(Question question, Quiz quiz)
    {
        if (quiz.QuizType == QuizType.Lesson)
        {
            return question.LessonId == quiz.LessonId;
        }

        if (question.ChapterId == quiz.ChapterId)
        {
            return true;
        }

        return question.LessonId.HasValue
            && await _unitOfWork.Lessons.Query().AnyAsync(lesson =>
                lesson.Id == question.LessonId
                && lesson.ChapterId == quiz.ChapterId
                && !lesson.IsDeleted);
    }

    private AdminQuizDto Map(Quiz quiz) => new()
    {
        Id = quiz.Id,
        QuizType = quiz.QuizType,
        LessonId = quiz.LessonId,
        ChapterId = quiz.ChapterId,
        RewardPolicyId = quiz.RewardPolicyId,
        Title = quiz.Title,
        PassScore = quiz.PassScore,
        DurationSeconds = quiz.DurationSeconds,
        FirstPassCoins = quiz.FirstPassCoins,
        IsPublished = quiz.IsPublished,
        PublishedAt = quiz.PublishedAt,
        QuestionCount = quiz.QuizQuestions.Count(link => !link.Question.IsDeleted),
        Questions = quiz.QuizQuestions
            .Where(link => !link.Question.IsDeleted)
            .OrderBy(link => link.OrderIndex)
            .Select(link => MapQuestion(link.Question))
            .ToList()
    };

    private static QuestionDto MapQuestion(Question question) => new()
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

    private static readonly JsonSerializerOptions _auditJsonOptions = new()
    {
        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private async Task AddAuditAsync(
        string entityType,
        Guid entityId,
        string action,
        object? before,
        object? after)
    {
        await _unitOfWork.ContentAuditLogs.AddAsync(new ContentAuditLog
        {
            AdminUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            BeforeData = before is null ? null : JsonSerializer.Serialize(before, _auditJsonOptions),
            AfterData = after is null ? null : JsonSerializer.Serialize(after, _auditJsonOptions),
            CreatedAt = DateTime.UtcNow
        });
    }

    private static object Snapshot(Quiz quiz) => new
    {
        quiz.QuizType,
        quiz.LessonId,
        quiz.ChapterId,
        quiz.RewardPolicyId,
        quiz.Title,
        quiz.PassScore,
        quiz.DurationSeconds,
        quiz.FirstPassCoins,
        quiz.IsPublished,
        quiz.IsDeleted
    };
}
