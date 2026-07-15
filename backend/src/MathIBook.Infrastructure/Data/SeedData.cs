using System.Text.Json;
using MathIBook.Domain.Entities;
using MathIBook.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MathIBook.Infrastructure.Data;

public static class SeedData
{
    public static async Task SeedAsync(AppDbContext context)
    {
        if (await context.Users.AnyAsync())
        {
            return;
        }

        // --- Users ---
        var admin1Id = Guid.NewGuid();
        var studentId = Guid.NewGuid();

        var admin1 = new User
        {
            Id = admin1Id,
            Name = "Admin",
            Email = "admin@mathibook.vn",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            Role = "Admin",
            Coins = 0,
            CreatedAt = DateTime.UtcNow
        };


        var student = new User
        {
            Id = studentId,
            Name = "Học sinh",
            Email = "student@mathibook.vn",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Student@123"),
            Role = "Student",
            Coins = 0,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.AddRange(admin1, student);

        // --- Grade 8 curriculum taxonomy ---
        var topicFunctionsId = Guid.NewGuid();
        var topicEquationsId = Guid.NewGuid();
        context.CurriculumTopics.AddRange(
            new CurriculumTopic
            {
                Id = topicFunctionsId,
                Code = "M8-ALG-FUNCTIONS",
                Name = "Biểu thức đại số và hàm số",
                Strand = CurriculumStrand.NumbersAndAlgebra,
                Grade = 8,
                OrderIndex = 1
            },
            new CurriculumTopic
            {
                Id = topicEquationsId,
                Code = "M8-ALG-EQUATIONS",
                Name = "Phương trình và bài toán lập phương trình",
                Strand = CurriculumStrand.NumbersAndAlgebra,
                Grade = 8,
                OrderIndex = 2
            });

        // --- Chapters & Lessons ---
        var chapter1Id = Guid.NewGuid();
        var chapter2Id = Guid.NewGuid();

        var chapter1 = new Chapter
        {
            Id = chapter1Id,
            Title = "Chương 1: Biểu thức đại số và Hàm số bậc nhất",
            Description = "Tìm hiểu về biểu thức đại số và hàm số bậc nhất",
            OrderIndex = 1,
            CurriculumTopicId = topicFunctionsId,
            IsPublished = true,
            PublishedAt = DateTime.UtcNow
        };

        var chapter2 = new Chapter
        {
            Id = chapter2Id,
            Title = "Chương 2: Phương trình và Hệ phương trình",
            Description = "Giải phương trình và hệ phương trình",
            OrderIndex = 2,
            CurriculumTopicId = topicEquationsId,
            IsPublished = true,
            PublishedAt = DateTime.UtcNow
        };

        context.Chapters.AddRange(chapter1, chapter2);

        // Lesson 1.1
        var lesson11Id = Guid.NewGuid();
        var lesson11 = new Lesson
        {
            Id = lesson11Id,
            ChapterId = chapter1Id,
            CurriculumTopicId = topicFunctionsId,
            Title = "Hàm số bậc nhất $y=ax+b$",
            ContentBody = "# Hàm số bậc nhất\n\nHàm số bậc nhất có dạng $y=ax+b$ với $a \\neq 0$.\n\nĐồ thị là một đường thẳng.\n\n## Ví dụ\n\n$$y=2x+3$$\n\nKhi $x=0$, $y=3$. Khi $x=1$, $y=5$.\n\n### Tính chất\n- Hệ số $a$ quyết định độ dốc của đường thẳng\n- Hệ số $b$ là tung độ gốc",
            SimulationType = "linear_graph",
            OrderIndex = 1,
            IsPublished = true
        };

        // Lesson 1.2
        var lesson12Id = Guid.NewGuid();
        var lesson12 = new Lesson
        {
            Id = lesson12Id,
            ChapterId = chapter1Id,
            CurriculumTopicId = topicFunctionsId,
            Title = "Đồ thị hàm số bậc nhất",
            ContentBody = "# Đồ thị hàm số bậc nhất\n\nĐồ thị của hàm số bậc nhất $y=ax+b$ $(a \\neq 0)$ là một đường thẳng.\n\n## Cách vẽ đồ thị\n\n1. Tìm giao điểm với trục tung: $x=0 \\Rightarrow y=b$\n2. Tìm giao điểm với trục hoành: $y=0 \\Rightarrow x=-\\frac{b}{a}$\n3. Vẽ đường thẳng qua hai điểm đó.\n\n## Ví dụ\n\nVẽ đồ thị hàm số $y=2x+1$:\n\n- $A(0,1)$\n- $B\\left(-\\frac12,0\\right)$\n\n### Nhận xét\n- Hệ số $a>0$: đồ thị đi lên từ trái sang phải\n- Hệ số $a<0$: đồ thị đi xuống từ trái sang phải",
            SimulationType = "linear_graph",
            OrderIndex = 2,
            IsPublished = true
        };

        // Lesson 2.1
        var lesson21Id = Guid.NewGuid();
        var lesson21 = new Lesson
        {
            Id = lesson21Id,
            ChapterId = chapter2Id,
            CurriculumTopicId = topicEquationsId,
            Title = "Phương trình bậc nhất một ẩn",
            ContentBody = "# Phương trình bậc nhất một ẩn\n\nPhương trình bậc nhất một ẩn có dạng $ax+b=0$ với $a \\neq 0$.\n\n## Cách giải\n\n$$ax+b=0 \\Leftrightarrow x=-\\frac{b}{a}$$\n\n## Ví dụ\n\nGiải phương trình $2x-4=0$\n\n$$2x-4=0 \\Leftrightarrow 2x=4 \\Leftrightarrow x=2$$\n\nVậy phương trình có nghiệm $x=2$.",
            SimulationType = null,
            OrderIndex = 1,
            IsPublished = true
        };

        // Lesson 2.2
        var lesson22Id = Guid.NewGuid();
        var lesson22 = new Lesson
        {
            Id = lesson22Id,
            ChapterId = chapter2Id,
            CurriculumTopicId = topicEquationsId,
            Title = "Giải bài toán bằng cách lập phương trình",
            ContentBody = "# Giải bài toán bằng cách lập phương trình\n\n## Các bước giải\n\n1. Đặt ẩn và điều kiện cho ẩn\n2. Biểu diễn các đại lượng chưa biết qua ẩn\n3. Lập phương trình\n4. Giải phương trình\n5. Đối chiếu điều kiện và kết luận\n\n## Ví dụ\n\nMột người đi xe máy từ A đến B với vận tốc $40$ km/h. Lúc về đi với vận tốc $30$ km/h. Thời gian cả đi và về là $3.5$ giờ. Tính quãng đường AB.\n\nGiải: Gọi quãng đường AB là $x$ (km), $x>0$.\n\nThời gian đi: $\\frac{x}{40}$ (giờ)\n\nThời gian về: $\\frac{x}{30}$ (giờ)\n\nTa có phương trình: $\\frac{x}{40}+\\frac{x}{30}=3.5$\n\n$$\\Leftrightarrow \\frac{3x+4x}{120}=3.5 \\Leftrightarrow 7x=420 \\Leftrightarrow x=60$$\n\nVậy quãng đường AB dài $60$ km.",
            SimulationType = null,
            OrderIndex = 2,
            IsPublished = true
        };

        context.Lessons.AddRange(lesson11, lesson12, lesson21, lesson22);

        // --- Questions for Lesson 1.1 ---
        var q11_1 = new Question
        {
            Id = Guid.NewGuid(),
            LessonId = lesson11Id,
            QuestionText = "Hàm số $y=2x+3$ có hệ số góc là:",
            Options = JsonSerializer.Serialize(new[] { "$1$", "$2$", "$3$", "$4$" }),
            CorrectOption = 1,
            Explanation = "Hàm số $y=ax+b$ có hệ số góc là $a$. Ở đây $a=2$.",
            OrderIndex = 1
        };

        var q11_2 = new Question
        {
            Id = Guid.NewGuid(),
            LessonId = lesson11Id,
            QuestionText = "Tung độ gốc của hàm số $y=-x+5$ là:",
            Options = JsonSerializer.Serialize(new[] { "$-1$", "$0$", "$5$", "$-5$" }),
            CorrectOption = 2,
            Explanation = "Tung độ gốc là giá trị $b$ trong $y=ax+b$. Ở đây $b=5$.",
            OrderIndex = 2
        };

        var q11_3 = new Question
        {
            Id = Guid.NewGuid(),
            LessonId = lesson11Id,
            QuestionText = "Hàm số nào sau đây là hàm số bậc nhất?",
            Options = JsonSerializer.Serialize(new[] { "$y=x^2+1$", "$y=\\frac{1}{x}$", "$y=3x-2$", "$y=\\sqrt{x}$" }),
            CorrectOption = 2,
            Explanation = "Hàm số bậc nhất có dạng $y=ax+b$ với $a \\neq 0$. $y=3x-2$ thỏa mãn.",
            OrderIndex = 3
        };

        var q11_4 = new Question
        {
            Id = Guid.NewGuid(),
            LessonId = lesson11Id,
            QuestionText = "Đồ thị của hàm số $y=ax+b$ $(a \\neq 0)$ là:",
            Options = JsonSerializer.Serialize(new[] { "Một đường parabol", "Một đường hypebol", "Một đường thẳng", "Một đường tròn" }),
            CorrectOption = 2,
            Explanation = "Đồ thị hàm số bậc nhất là một đường thẳng.",
            OrderIndex = 4
        };

        // --- Questions for Lesson 1.2 ---
        var q12_1 = new Question
        {
            Id = Guid.NewGuid(),
            LessonId = lesson12Id,
            QuestionText = "Đồ thị hàm số $y=2x$ đi qua điểm nào sau đây?",
            Options = JsonSerializer.Serialize(new[] { "$(0,1)$", "$(1,2)$", "$(2,1)$", "$(0,0)$" }),
            CorrectOption = 3,
            Explanation = "Thay $x=0$ được $y=0$, thay $x=1$ được $y=2$. Cả $(0,0)$ và $(1,2)$ đều thuộc đồ thị. Đáp án $(0,0)$.",
            OrderIndex = 1
        };

        var q12_2 = new Question
        {
            Id = Guid.NewGuid(),
            LessonId = lesson12Id,
            QuestionText = "Đồ thị hàm số $y=3x-6$ cắt trục hoành tại điểm có hoành độ:",
            Options = JsonSerializer.Serialize(new[] { "$x=0$", "$x=2$", "$x=-2$", "$x=6$" }),
            CorrectOption = 1,
            Explanation = "Cho $y=0 \\Rightarrow 3x-6=0 \\Rightarrow x=2$.",
            OrderIndex = 2
        };

        var q12_3 = new Question
        {
            Id = Guid.NewGuid(),
            LessonId = lesson12Id,
            QuestionText = "Cho hàm số $y=(m-1)x+2$. Tìm $m$ để hàm số đồng biến.",
            Options = JsonSerializer.Serialize(new[] { "$m<1$", "$m>1$", "$m=1$", "$m \\neq 1$" }),
            CorrectOption = 1,
            Explanation = "Hàm số đồng biến khi $a>0 \\Rightarrow m-1>0 \\Rightarrow m>1$.",
            OrderIndex = 3
        };

        var q12_4 = new Question
        {
            Id = Guid.NewGuid(),
            LessonId = lesson12Id,
            QuestionText = "Đường thẳng $y=-2x+4$ cắt trục tung tại điểm:",
            Options = JsonSerializer.Serialize(new[] { "$(0,0)$", "$(0,2)$", "$(0,4)$", "$(0,-2)$" }),
            CorrectOption = 2,
            Explanation = "Cho $x=0 \\Rightarrow y=4$. Điểm $(0,4)$.",
            OrderIndex = 4
        };

        // --- Questions for Lesson 2.1 ---
        var q21_1 = new Question
        {
            Id = Guid.NewGuid(),
            LessonId = lesson21Id,
            QuestionText = "Giải phương trình $2x+3=7$:",
            Options = JsonSerializer.Serialize(new[] { "$x=1$", "$x=2$", "$x=3$", "$x=4$" }),
            CorrectOption = 1,
            Explanation = "$2x+3=7 \\Leftrightarrow 2x=4 \\Leftrightarrow x=2$.",
            OrderIndex = 1
        };

        var q21_2 = new Question
        {
            Id = Guid.NewGuid(),
            LessonId = lesson21Id,
            QuestionText = "Nghiệm của phương trình $3x-6=0$ là:",
            Options = JsonSerializer.Serialize(new[] { "$x=-2$", "$x=0$", "$x=2$", "$x=6$" }),
            CorrectOption = 2,
            Explanation = "$3x-6=0 \\Leftrightarrow 3x=6 \\Leftrightarrow x=2$.",
            OrderIndex = 2
        };

        var q21_3 = new Question
        {
            Id = Guid.NewGuid(),
            LessonId = lesson21Id,
            QuestionText = "Giải phương trình $5-2x=1$:",
            Options = JsonSerializer.Serialize(new[] { "$x=1$", "$x=2$", "$x=3$", "$x=4$" }),
            CorrectOption = 1,
            Explanation = "$5-2x=1 \\Leftrightarrow -2x=-4 \\Leftrightarrow x=2$.",
            OrderIndex = 3
        };

        var q21_4 = new Question
        {
            Id = Guid.NewGuid(),
            LessonId = lesson21Id,
            QuestionText = "Phương trình nào sau đây là phương trình bậc nhất một ẩn?",
            Options = JsonSerializer.Serialize(new[] { "$x^2+1=0$", "$\\frac{1}{x}=2$", "$3x+5=0$", "$xy=1$" }),
            CorrectOption = 2,
            Explanation = "Phương trình bậc nhất một ẩn có dạng $ax+b=0$ với $a \\neq 0$. $3x+5=0$ thỏa mãn.",
            OrderIndex = 4
        };

        // --- Questions for Lesson 2.2 ---
        var q22_1 = new Question
        {
            Id = Guid.NewGuid(),
            LessonId = lesson22Id,
            QuestionText = "Tổng hai số là 10 và hiệu của chúng là 4. Số lớn hơn là:",
            Options = JsonSerializer.Serialize(new[] { "$5$", "$6$", "$7$", "$8$" }),
            CorrectOption = 2,
            Explanation = "Gọi số lớn là $x$, số bé là $10-x$. Ta có $x-(10-x)=4 \\Leftrightarrow 2x=14 \\Leftrightarrow x=7$.",
            OrderIndex = 1
        };

        var q22_2 = new Question
        {
            Id = Guid.NewGuid(),
            LessonId = lesson22Id,
            QuestionText = "Một hình chữ nhật có chu vi 30cm, chiều dài gấp đôi chiều rộng. Diện tích là:",
            Options = JsonSerializer.Serialize(new[] { "$30\\text{cm}^2$", "$40\\text{cm}^2$", "$50\\text{cm}^2$", "$60\\text{cm}^2$" }),
            CorrectOption = 1,
            Explanation = "Gọi chiều rộng là $x$, chiều dài là $2x$. Chu vi: $2(x+2x)=30 \\Rightarrow 6x=30 \\Rightarrow x=5$. Diện tích: $5\\cdot10=50\\text{cm}^2$.",
            OrderIndex = 2
        };

        var q22_3 = new Question
        {
            Id = Guid.NewGuid(),
            LessonId = lesson22Id,
            QuestionText = "Tuổi cha gấp 3 lần tuổi con. Sau 10 năm, tuổi cha gấp đôi tuổi con. Tuổi con hiện nay là:",
            Options = JsonSerializer.Serialize(new[] { "$10$", "$15$", "$20$", "$25$" }),
            CorrectOption = 0,
            Explanation = "Gọi tuổi con là $x$, tuổi cha là $3x$. Sau 10 năm: $3x+10=2(x+10) \\Rightarrow 3x+10=2x+20 \\Rightarrow x=10$.",
            OrderIndex = 3
        };

        var q22_4 = new Question
        {
            Id = Guid.NewGuid(),
            LessonId = lesson22Id,
            QuestionText = "Hai xe xuất phát cùng lúc từ cùng một điểm. Xe A đi với vận tốc 60km/h, xe B đi với vận tốc 40km/h. Sau bao lâu chúng cách nhau 50km?",
            Options = JsonSerializer.Serialize(new[] { "1 giờ", "1.5 giờ", "2 giờ", "2.5 giờ" }),
            CorrectOption = 2,
            Explanation = "Hiệu vận tốc: $60-40=20$ km/h. Thời gian: $50/20=2.5$ giờ.",
            OrderIndex = 4
        };

        context.Questions.AddRange(q11_1, q11_2, q11_3, q11_4,
                                    q12_1, q12_2, q12_3, q12_4,
                                    q21_1, q21_2, q21_3, q21_4,
                                    q22_1, q22_2, q22_3, q22_4);

        // --- Badges ---
        var badge1Id = Guid.NewGuid();
        var badge2Id = Guid.NewGuid();
        var badge3Id = Guid.NewGuid();

        var badge1 = new Badge
        {
            Id = badge1Id,
            Title = "Hoàn thành Chương 1",
            Description = "Hoàn thành tất cả bài học trong Chương 1",
            IconUrl = "/images/badges/chapter1.png",
            ConditionType = "complete_chapter",
            ConditionValue = JsonSerializer.Serialize(new { chapterId = chapter1Id.ToString() })
        };

        var badge2 = new Badge
        {
            Id = badge2Id,
            Title = "Học sinh xuất sắc",
            Description = "Đạt 3 lần điểm tuyệt đối liên tiếp trong bài kiểm tra",
            IconUrl = "/images/badges/excellent.png",
            ConditionType = "perfect_quiz_streak",
            ConditionValue = JsonSerializer.Serialize(new { streak = 3 })
        };

        var badge3 = new Badge
        {
            Id = badge3Id,
            Title = "Nhà toán học",
            Description = "Tích lũy 100 xu",
            IconUrl = "/images/badges/mathematician.png",
            ConditionType = "total_coins",
            ConditionValue = JsonSerializer.Serialize(new { coins = 100 })
        };

        context.Badges.AddRange(badge1, badge2, badge3);

        // --- Reward policies, lesson quizzes and chapter quizzes ---
        var lessonPolicy = new RewardPolicy
        {
            Id = Guid.NewGuid(),
            Name = "Thưởng quiz bài học mặc định",
            QuizType = QuizType.Lesson,
            CoinsPerCorrectAnswer = 10,
            FirstPassBonusCoins = 10,
            PerfectScoreBonusCoins = 5,
            RetryRewardPercent = 50,
            EffectiveFrom = DateTime.UtcNow.AddDays(-1)
        };
        var chapterPolicy = new RewardPolicy
        {
            Id = Guid.NewGuid(),
            Name = "Thưởng quiz chương mặc định",
            QuizType = QuizType.Chapter,
            CoinsPerCorrectAnswer = 10,
            FirstPassBonusCoins = 25,
            PerfectScoreBonusCoins = 10,
            ChapterCompletionBonusCoins = 50,
            RetryRewardPercent = 25,
            EffectiveFrom = DateTime.UtcNow.AddDays(-1)
        };
        context.RewardPolicies.AddRange(lessonPolicy, chapterPolicy);

        var lessonQuiz11 = CreateQuiz(QuizType.Lesson, lesson11Id, null, "Quiz: Hàm số bậc nhất", lessonPolicy.Id);
        var lessonQuiz12 = CreateQuiz(QuizType.Lesson, lesson12Id, null, "Quiz: Đồ thị hàm số bậc nhất", lessonPolicy.Id);
        var lessonQuiz21 = CreateQuiz(QuizType.Lesson, lesson21Id, null, "Quiz: Phương trình bậc nhất một ẩn", lessonPolicy.Id);
        var lessonQuiz22 = CreateQuiz(QuizType.Lesson, lesson22Id, null, "Quiz: Lập phương trình", lessonPolicy.Id);
        var chapterQuiz1 = CreateQuiz(QuizType.Chapter, null, chapter1Id, "Kiểm tra Chương 1", chapterPolicy.Id);
        var chapterQuiz2 = CreateQuiz(QuizType.Chapter, null, chapter2Id, "Kiểm tra Chương 2", chapterPolicy.Id);
        context.Quizzes.AddRange(
            lessonQuiz11,
            lessonQuiz12,
            lessonQuiz21,
            lessonQuiz22,
            chapterQuiz1,
            chapterQuiz2);

        AddQuizQuestions(context, lessonQuiz11.Id, [q11_1, q11_2, q11_3, q11_4]);
        AddQuizQuestions(context, lessonQuiz12.Id, [q12_1, q12_2, q12_3, q12_4]);
        AddQuizQuestions(context, lessonQuiz21.Id, [q21_1, q21_2, q21_3, q21_4]);
        AddQuizQuestions(context, lessonQuiz22.Id, [q22_1, q22_2, q22_3, q22_4]);
        AddQuizQuestions(context, chapterQuiz1.Id, [q11_1, q11_2, q12_1, q12_2]);
        AddQuizQuestions(context, chapterQuiz2.Id, [q21_1, q21_2, q22_1, q22_2]);

        context.BadgeRules.AddRange(
            new BadgeRule
            {
                BadgeId = badge1Id,
                RuleType = "complete_chapter",
                TargetChapterId = chapter1Id,
                TargetQuizId = chapterQuiz1.Id,
                OrderIndex = 1
            },
            new BadgeRule
            {
                BadgeId = badge2Id,
                RuleType = "perfect_quiz_streak",
                ThresholdValue = 3,
                OrderIndex = 1
            },
            new BadgeRule
            {
                BadgeId = badge3Id,
                RuleType = "total_coins",
                ThresholdValue = 100,
                OrderIndex = 1
            });

        await context.SaveChangesAsync();
    }
    private static Quiz CreateQuiz(
        QuizType type,
        Guid? lessonId,
        Guid? chapterId,
        string title,
        Guid rewardPolicyId)
    {
        return new Quiz
        {
            Id = Guid.NewGuid(),
            QuizType = type,
            LessonId = lessonId,
            ChapterId = chapterId,
            RewardPolicyId = rewardPolicyId,
            Title = title,
            PassScore = 5,
            DurationSeconds = type == QuizType.Chapter ? 1200 : 600,
            IsPublished = true,
            PublishedAt = DateTime.UtcNow
        };
    }

    private static void AddQuizQuestions(
        AppDbContext context,
        Guid quizId,
        IReadOnlyList<Question> questions)
    {
        for (var index = 0; index < questions.Count; index++)
        {
            context.QuizQuestions.Add(new QuizQuestion
            {
                QuizId = quizId,
                QuestionId = questions[index].Id,
                OrderIndex = index + 1,
                Weight = 1
            });
        }
    }

}
