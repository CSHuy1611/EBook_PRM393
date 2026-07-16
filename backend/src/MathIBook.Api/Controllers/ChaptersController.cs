using MathIBook.Application.DTOs;
using MathIBook.Domain.Entities;
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

        var chapterList = chapters.ToList();
        var chapterProgressMap = chapterProgresses
            .ToDictionary(progress => progress.ChapterId);

        return Ok(chapterList.Select((chapter, index) =>
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

            var isUnlocked = index == 0;
            if (!isUnlocked)
            {
                var prevChapter = chapterList[index - 1];
                var prevQuiz = prevChapter.Quizzes.FirstOrDefault();
                if (prevQuiz is null)
                {
                    isUnlocked = true;
                }
                else
                {
                    var prevProgress = chapterProgresses.FirstOrDefault(p =>
                        p.ChapterId == prevChapter.Id);
                    isUnlocked = prevProgress?.Status == LearningStatus.Passed;
                }
            }

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
                IsUnlocked = isUnlocked,
                LessonCount = chapter.Lessons.Count,
                PassedLessonCount = passed,
                CompletionPercentage = chapter.Lessons.Count > 0
                    ? Math.Round((double)passed / chapter.Lessons.Count * 100, 2)
                    : 0,
                ChapterQuizId = chapterQuiz?.Id,
                ChapterQuizStatus = isUnlocked ? quizStatus : "Locked",
                RelatedBadgeId = badgeRule?.BadgeId,
                RelatedBadgeTitle = badgeRule?.Badge.Title
            };
        }).ToList());
    }

    [HttpGet("{id}/lessons")]
    public async Task<ActionResult<List<LessonDto>>> GetLessons(Guid id)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var chapter = await _unitOfWork.Chapters.Query()
            .Include(ch => ch.Quizzes.Where(q => q.QuizType == QuizType.Chapter && q.IsPublished && !q.IsDeleted))
            .FirstOrDefaultAsync(ch => ch.Id == id && ch.IsPublished && !ch.IsDeleted);
        if (chapter is null)
        {
            return NotFound(new ProblemDetails { Title = "Không tìm thấy chương.", Status = 404 });
        }

        if (!await IsChapterUnlocked(userId, chapter))
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Chương chưa được mở khóa.",
                Detail = "Bạn cần hoàn thành bài kiểm tra chương trước đó.",
                Status = 401
            });
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

    private async Task<bool> IsChapterUnlocked(Guid userId, Chapter chapter)
    {
        var allChapters = await _unitOfWork.Chapters.Query()
            .Where(ch => ch.IsPublished && !ch.IsDeleted)
            .OrderBy(ch => ch.OrderIndex)
            .Include(ch => ch.Quizzes.Where(q =>
                q.QuizType == QuizType.Chapter && q.IsPublished && !q.IsDeleted))
            .ToListAsync();

        var chapterIndex = allChapters.FindIndex(ch => ch.Id == chapter.Id);
        if (chapterIndex <= 0) return true;

        var prevChapter = allChapters[chapterIndex - 1];
        var prevQuiz = prevChapter.Quizzes.FirstOrDefault();
        if (prevQuiz is null) return true;

        var prevProgress = await _unitOfWork.ChapterProgresses.Query()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.ChapterId == prevChapter.Id);
        return prevProgress?.Status == LearningStatus.Passed;
    }
}
