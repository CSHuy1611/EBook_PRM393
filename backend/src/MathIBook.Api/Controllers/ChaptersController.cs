using MathIBook.Application.DTOs;
using MathIBook.Domain.Enums;
using MathIBook.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MathIBook.Api.Controllers;

[Route("api/chapters")]
[ApiController]
[Authorize(Roles = "Student")]
public class ChaptersController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public ChaptersController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<List<ChapterDto>>> GetAll()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var chapters = await _unitOfWork.Chapters.Query()
            .Where(chapter => chapter.IsPublished && !chapter.IsDeleted)
            .OrderBy(chapter => chapter.OrderIndex)
            .Include(chapter => chapter.Lessons.Where(lesson => lesson.IsPublished && !lesson.IsDeleted))
            .Include(chapter => chapter.Quizzes.Where(quiz =>
                quiz.QuizType == QuizType.Chapter && quiz.IsPublished && !quiz.IsDeleted))
            .ToListAsync();
        var progresses = await _unitOfWork.Progresses.Query()
            .Where(progress => progress.UserId == userId)
            .ToListAsync();
        var chapterProgresses = await _unitOfWork.ChapterProgresses.Query()
            .Where(progress => progress.UserId == userId)
            .ToListAsync();
        var relatedBadges = await _unitOfWork.BadgeRules.Query()
            .Where(rule =>
                rule.RuleType == "complete_chapter"
                && rule.TargetChapterId.HasValue
                && rule.Badge.IsActive
                && !rule.Badge.IsDeleted)
            .Include(rule => rule.Badge)
            .ToListAsync();

        return Ok(chapters.Select(chapter =>
        {
            var passed = chapter.Lessons.Count(lesson => progresses.Any(progress =>
                progress.LessonId == lesson.Id && progress.Status == LearningStatus.Passed));
            var chapterQuiz = chapter.Quizzes.FirstOrDefault();
            var chapterProgress = chapterProgresses.FirstOrDefault(progress =>
                progress.ChapterId == chapter.Id);
            var quizStatus = chapterQuiz is null
                ? "Unavailable"
                : chapterProgress?.Status == LearningStatus.Passed
                    ? "Passed"
                    : passed == chapter.Lessons.Count && chapter.Lessons.Count > 0
                        ? "Unlocked"
                        : "Locked";
            var badgeRule = relatedBadges.FirstOrDefault(rule =>
                rule.TargetChapterId == chapter.Id);

            return new ChapterDto
            {
                Id = chapter.Id,
                Title = chapter.Title,
                Description = chapter.Description,
                OrderIndex = chapter.OrderIndex,
                CurriculumTopicId = chapter.CurriculumTopicId,
                IsPublished = chapter.IsPublished,
                LessonCount = chapter.Lessons.Count,
                PassedLessonCount = passed,
                CompletionPercentage = chapter.Lessons.Count > 0
                    ? Math.Round((double)passed / chapter.Lessons.Count * 100, 2)
                    : 0,
                ChapterQuizId = chapterQuiz?.Id,
                ChapterQuizStatus = quizStatus,
                RelatedBadgeId = badgeRule?.BadgeId,
                RelatedBadgeTitle = badgeRule?.Badge.Title
            };
        }).ToList());
    }

    [HttpGet("{id}/lessons")]
    public async Task<ActionResult<List<LessonDto>>> GetLessons(Guid id)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var chapterExists = await _unitOfWork.Chapters.Query()
            .AnyAsync(chapter => chapter.Id == id && chapter.IsPublished && !chapter.IsDeleted);
        if (!chapterExists)
        {
            return NotFound(new ProblemDetails { Title = "Không tìm thấy chương.", Status = 404 });
        }

        var lessons = await _unitOfWork.Lessons.Query()
            .Where(lesson => lesson.ChapterId == id && lesson.IsPublished && !lesson.IsDeleted)
            .OrderBy(lesson => lesson.OrderIndex)
            .Include(lesson => lesson.Quizzes.Where(quiz =>
                quiz.QuizType == QuizType.Lesson && quiz.IsPublished && !quiz.IsDeleted))
            .ToListAsync();
        var lessonIds = lessons.Select(lesson => lesson.Id).ToList();
        var progresses = await _unitOfWork.Progresses.Query()
            .Where(progress => progress.UserId == userId && lessonIds.Contains(progress.LessonId))
            .ToListAsync();

        return Ok(lessons.Select(lesson =>
        {
            var progress = progresses.FirstOrDefault(item => item.LessonId == lesson.Id);
            return new LessonDto
            {
                Id = lesson.Id,
                ChapterId = lesson.ChapterId,
                CurriculumTopicId = lesson.CurriculumTopicId,
                Title = lesson.Title,
                OrderIndex = lesson.OrderIndex,
                ContentVersion = lesson.ContentVersion,
                IsPublished = true,
                IsCompleted = progress?.Status == LearningStatus.Passed,
                Status = (progress?.Status ?? LearningStatus.NotStarted).ToString(),
                ContentViewed = progress?.ContentViewed ?? false,
                BestScore = progress is null ? null : (double)progress.BestScore10,
                QuizId = lesson.Quizzes.FirstOrDefault()?.Id
            };
        }).ToList());
    }
}
