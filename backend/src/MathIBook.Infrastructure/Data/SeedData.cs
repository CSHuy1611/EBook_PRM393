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
        var admin2Id = Guid.NewGuid();
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

        var admin2 = new User
        {
            Id = admin2Id,
            Name = "Admin 2",
            Email = "admin2@mathibook.vn",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            Role = "Admin",
            Coins = 0,
            CreatedAt = DateTime.UtcNow
        };

        var student = new User
        {
            Id = studentId,
            Name = "Há»c sinh",
            Email = "student@mathibook.vn",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Student@123"),
            Role = "Student",
            Coins = 0,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.AddRange(admin1, admin2, student);

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
            Title = "ChÆ°Æ¡ng 1: Biá»ƒu thá»©c Ä‘áº¡i sá»‘ vÃ  HÃ m sá»‘ báº­c nháº¥t",
            Description = "TÃ¬m hiá»ƒu vá» biá»ƒu thá»©c Ä‘áº¡i sá»‘ vÃ  hÃ m sá»‘ báº­c nháº¥t",
            OrderIndex = 1,
            CurriculumTopicId = topicFunctionsId,
            IsPublished = true,
            PublishedAt = DateTime.UtcNow
        };

        var chapter2 = new Chapter
        {
            Id = chapter2Id,
            Title = "ChÆ°Æ¡ng 2: PhÆ°Æ¡ng trÃ¬nh vÃ  Há»‡ phÆ°Æ¡ng trÃ¬nh",
            Description = "Giáº£i phÆ°Æ¡ng trÃ¬nh vÃ  há»‡ phÆ°Æ¡ng trÃ¬nh",
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
            Title = "HÃ m sá»‘ báº­c nháº¥t $y=ax+b$",
            ContentBody = "# HÃ m sá»‘ báº­c nháº¥t\n\nHÃ m sá»‘ báº­c nháº¥t cÃ³ dáº¡ng $y=ax+b$ vá»›i $a \\neq 0$.\n\nÄá»“ thá»‹ lÃ  má»™t Ä‘Æ°á»ng tháº³ng.\n\n## VÃ­ dá»¥\n\n$$y=2x+3$$\n\nKhi $x=0$, $y=3$. Khi $x=1$, $y=5$.\n\n### TÃ­nh cháº¥t\n- Há»‡ sá»‘ $a$ quyáº¿t Ä‘á»‹nh Ä‘á»™ dá»‘c cá»§a Ä‘Æ°á»ng tháº³ng\n- Há»‡ sá»‘ $b$ lÃ  tung Ä‘á»™ gá»‘c",
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
            Title = "Äá»“ thá»‹ hÃ m sá»‘ báº­c nháº¥t",
            ContentBody = "# Äá»“ thá»‹ hÃ m sá»‘ báº­c nháº¥t\n\nÄá»“ thá»‹ cá»§a hÃ m sá»‘ báº­c nháº¥t $y=ax+b$ $(a \\neq 0)$ lÃ  má»™t Ä‘Æ°á»ng tháº³ng.\n\n## CÃ¡ch váº½ Ä‘á»“ thá»‹\n\n1. TÃ¬m giao Ä‘iá»ƒm vá»›i trá»¥c tung: $x=0 \\Rightarrow y=b$\n2. TÃ¬m giao Ä‘iá»ƒm vá»›i trá»¥c hoÃ nh: $y=0 \\Rightarrow x=-\\frac{b}{a}$\n3. Váº½ Ä‘Æ°á»ng tháº³ng qua hai Ä‘iá»ƒm Ä‘Ã³.\n\n## VÃ­ dá»¥\n\nVáº½ Ä‘á»“ thá»‹ hÃ m sá»‘ $y=2x+1$:\n\n- $A(0,1)$\n- $B\\left(-\\frac12,0\\right)$\n\n### Nháº­n xÃ©t\n- Há»‡ sá»‘ $a>0$: Ä‘á»“ thá»‹ Ä‘i lÃªn tá»« trÃ¡i sang pháº£i\n- Há»‡ sá»‘ $a<0$: Ä‘á»“ thá»‹ Ä‘i xuá»‘ng tá»« trÃ¡i sang pháº£i",
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
            Title = "PhÆ°Æ¡ng trÃ¬nh báº­c nháº¥t má»™t áº©n",
            ContentBody = "# PhÆ°Æ¡ng trÃ¬nh báº­c nháº¥t má»™t áº©n\n\nPhÆ°Æ¡ng trÃ¬nh báº­c nháº¥t má»™t áº©n cÃ³ dáº¡ng $ax+b=0$ vá»›i $a \\neq 0$.\n\n## CÃ¡ch giáº£i\n\n$$ax+b=0 \\Leftrightarrow x=-\\frac{b}{a}$$\n\n## VÃ­ dá»¥\n\nGiáº£i phÆ°Æ¡ng trÃ¬nh $2x-4=0$\n\n$$2x-4=0 \\Leftrightarrow 2x=4 \\Leftrightarrow x=2$$\n\nVáº­y phÆ°Æ¡ng trÃ¬nh cÃ³ nghiá»‡m $x=2$.",
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
            Title = "Giáº£i bÃ i toÃ¡n báº±ng cÃ¡ch láº­p phÆ°Æ¡ng trÃ¬nh",
            ContentBody = "# Giáº£i bÃ i toÃ¡n báº±ng cÃ¡ch láº­p phÆ°Æ¡ng trÃ¬nh\n\n## CÃ¡c bÆ°á»›c giáº£i\n\n1. Äáº·t áº©n vÃ  Ä‘iá»u kiá»‡n cho áº©n\n2. Biá»ƒu diá»…n cÃ¡c Ä‘áº¡i lÆ°á»£ng chÆ°a biáº¿t qua áº©n\n3. Láº­p phÆ°Æ¡ng trÃ¬nh\n4. Giáº£i phÆ°Æ¡ng trÃ¬nh\n5. Äá»‘i chiáº¿u Ä‘iá»u kiá»‡n vÃ  káº¿t luáº­n\n\n## VÃ­ dá»¥\n\nMá»™t ngÆ°á»i Ä‘i xe mÃ¡y tá»« A Ä‘áº¿n B vá»›i váº­n tá»‘c $40$ km/h. LÃºc vá» Ä‘i vá»›i váº­n tá»‘c $30$ km/h. Thá»i gian cáº£ Ä‘i vÃ  vá» lÃ  $3.5$ giá». TÃ­nh quÃ£ng Ä‘Æ°á»ng AB.\n\nGiáº£i: Gá»i quÃ£ng Ä‘Æ°á»ng AB lÃ  $x$ (km), $x>0$.\n\nThá»i gian Ä‘i: $\\frac{x}{40}$ (giá»)\n\nThá»i gian vá»: $\\frac{x}{30}$ (giá»)\n\nTa cÃ³ phÆ°Æ¡ng trÃ¬nh: $\\frac{x}{40}+\\frac{x}{30}=3.5$\n\n$$\\Leftrightarrow \\frac{3x+4x}{120}=3.5 \\Leftrightarrow 7x=420 \\Leftrightarrow x=60$$\n\nVáº­y quÃ£ng Ä‘Æ°á»ng AB dÃ i $60$ km.",
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
            QuestionText = "HÃ m sá»‘ $y=2x+3$ cÃ³ há»‡ sá»‘ gÃ³c lÃ :",
            Options = JsonSerializer.Serialize(new[] { "$1$", "$2$", "$3$", "$4$" }),
            CorrectOption = 1,
            Explanation = "HÃ m sá»‘ $y=ax+b$ cÃ³ há»‡ sá»‘ gÃ³c lÃ  $a$. á»ž Ä‘Ã¢y $a=2$.",
            OrderIndex = 1
        };

        var q11_2 = new Question
        {
            Id = Guid.NewGuid(),
            LessonId = lesson11Id,
            QuestionText = "Tung Ä‘á»™ gá»‘c cá»§a hÃ m sá»‘ $y=-x+5$ lÃ :",
            Options = JsonSerializer.Serialize(new[] { "$-1$", "$0$", "$5$", "$-5$" }),
            CorrectOption = 2,
            Explanation = "Tung Ä‘á»™ gá»‘c lÃ  giÃ¡ trá»‹ $b$ trong $y=ax+b$. á»ž Ä‘Ã¢y $b=5$.",
            OrderIndex = 2
        };

        var q11_3 = new Question
        {
            Id = Guid.NewGuid(),
            LessonId = lesson11Id,
            QuestionText = "HÃ m sá»‘ nÃ o sau Ä‘Ã¢y lÃ  hÃ m sá»‘ báº­c nháº¥t?",
            Options = JsonSerializer.Serialize(new[] { "$y=x^2+1$", "$y=\\frac{1}{x}$", "$y=3x-2$", "$y=\\sqrt{x}$" }),
            CorrectOption = 2,
            Explanation = "HÃ m sá»‘ báº­c nháº¥t cÃ³ dáº¡ng $y=ax+b$ vá»›i $a \\neq 0$. $y=3x-2$ thá»a mÃ£n.",
            OrderIndex = 3
        };

        var q11_4 = new Question
        {
            Id = Guid.NewGuid(),
            LessonId = lesson11Id,
            QuestionText = "Äá»“ thá»‹ cá»§a hÃ m sá»‘ $y=ax+b$ $(a \\neq 0)$ lÃ :",
            Options = JsonSerializer.Serialize(new[] { "Má»™t Ä‘Æ°á»ng parabol", "Má»™t Ä‘Æ°á»ng hypebol", "Má»™t Ä‘Æ°á»ng tháº³ng", "Má»™t Ä‘Æ°á»ng trÃ²n" }),
            CorrectOption = 2,
            Explanation = "Äá»“ thá»‹ hÃ m sá»‘ báº­c nháº¥t lÃ  má»™t Ä‘Æ°á»ng tháº³ng.",
            OrderIndex = 4
        };

        // --- Questions for Lesson 1.2 ---
        var q12_1 = new Question
        {
            Id = Guid.NewGuid(),
            LessonId = lesson12Id,
            QuestionText = "Äá»“ thá»‹ hÃ m sá»‘ $y=2x$ Ä‘i qua Ä‘iá»ƒm nÃ o sau Ä‘Ã¢y?",
            Options = JsonSerializer.Serialize(new[] { "$(0,1)$", "$(1,2)$", "$(2,1)$", "$(0,0)$" }),
            CorrectOption = 3,
            Explanation = "Thay $x=0$ Ä‘Æ°á»£c $y=0$, thay $x=1$ Ä‘Æ°á»£c $y=2$. Cáº£ $(0,0)$ vÃ  $(1,2)$ Ä‘á»u thuá»™c Ä‘á»“ thá»‹. ÄÃ¡p Ã¡n $(0,0)$.",
            OrderIndex = 1
        };

        var q12_2 = new Question
        {
            Id = Guid.NewGuid(),
            LessonId = lesson12Id,
            QuestionText = "Äá»“ thá»‹ hÃ m sá»‘ $y=3x-6$ cáº¯t trá»¥c hoÃ nh táº¡i Ä‘iá»ƒm cÃ³ hoÃ nh Ä‘á»™:",
            Options = JsonSerializer.Serialize(new[] { "$x=0$", "$x=2$", "$x=-2$", "$x=6$" }),
            CorrectOption = 1,
            Explanation = "Cho $y=0 \\Rightarrow 3x-6=0 \\Rightarrow x=2$.",
            OrderIndex = 2
        };

        var q12_3 = new Question
        {
            Id = Guid.NewGuid(),
            LessonId = lesson12Id,
            QuestionText = "Cho hÃ m sá»‘ $y=(m-1)x+2$. TÃ¬m $m$ Ä‘á»ƒ hÃ m sá»‘ Ä‘á»“ng biáº¿n.",
            Options = JsonSerializer.Serialize(new[] { "$m<1$", "$m>1$", "$m=1$", "$m \\neq 1$" }),
            CorrectOption = 1,
            Explanation = "HÃ m sá»‘ Ä‘á»“ng biáº¿n khi $a>0 \\Rightarrow m-1>0 \\Rightarrow m>1$.",
            OrderIndex = 3
        };

        var q12_4 = new Question
        {
            Id = Guid.NewGuid(),
            LessonId = lesson12Id,
            QuestionText = "ÄÆ°á»ng tháº³ng $y=-2x+4$ cáº¯t trá»¥c tung táº¡i Ä‘iá»ƒm:",
            Options = JsonSerializer.Serialize(new[] { "$(0,0)$", "$(0,2)$", "$(0,4)$", "$(0,-2)$" }),
            CorrectOption = 2,
            Explanation = "Cho $x=0 \\Rightarrow y=4$. Äiá»ƒm $(0,4)$.",
            OrderIndex = 4
        };

        // --- Questions for Lesson 2.1 ---
        var q21_1 = new Question
        {
            Id = Guid.NewGuid(),
            LessonId = lesson21Id,
            QuestionText = "Giáº£i phÆ°Æ¡ng trÃ¬nh $2x+3=7$:",
            Options = JsonSerializer.Serialize(new[] { "$x=1$", "$x=2$", "$x=3$", "$x=4$" }),
            CorrectOption = 1,
            Explanation = "$2x+3=7 \\Leftrightarrow 2x=4 \\Leftrightarrow x=2$.",
            OrderIndex = 1
        };

        var q21_2 = new Question
        {
            Id = Guid.NewGuid(),
            LessonId = lesson21Id,
            QuestionText = "Nghiá»‡m cá»§a phÆ°Æ¡ng trÃ¬nh $3x-6=0$ lÃ :",
            Options = JsonSerializer.Serialize(new[] { "$x=-2$", "$x=0$", "$x=2$", "$x=6$" }),
            CorrectOption = 2,
            Explanation = "$3x-6=0 \\Leftrightarrow 3x=6 \\Leftrightarrow x=2$.",
            OrderIndex = 2
        };

        var q21_3 = new Question
        {
            Id = Guid.NewGuid(),
            LessonId = lesson21Id,
            QuestionText = "Giáº£i phÆ°Æ¡ng trÃ¬nh $5-2x=1$:",
            Options = JsonSerializer.Serialize(new[] { "$x=1$", "$x=2$", "$x=3$", "$x=4$" }),
            CorrectOption = 1,
            Explanation = "$5-2x=1 \\Leftrightarrow -2x=-4 \\Leftrightarrow x=2$.",
            OrderIndex = 3
        };

        var q21_4 = new Question
        {
            Id = Guid.NewGuid(),
            LessonId = lesson21Id,
            QuestionText = "PhÆ°Æ¡ng trÃ¬nh nÃ o sau Ä‘Ã¢y lÃ  phÆ°Æ¡ng trÃ¬nh báº­c nháº¥t má»™t áº©n?",
            Options = JsonSerializer.Serialize(new[] { "$x^2+1=0$", "$\\frac{1}{x}=2$", "$3x+5=0$", "$xy=1$" }),
            CorrectOption = 2,
            Explanation = "PhÆ°Æ¡ng trÃ¬nh báº­c nháº¥t má»™t áº©n cÃ³ dáº¡ng $ax+b=0$ vá»›i $a \\neq 0$. $3x+5=0$ thá»a mÃ£n.",
            OrderIndex = 4
        };

        // --- Questions for Lesson 2.2 ---
        var q22_1 = new Question
        {
            Id = Guid.NewGuid(),
            LessonId = lesson22Id,
            QuestionText = "Tá»•ng hai sá»‘ lÃ  10 vÃ  hiá»‡u cá»§a chÃºng lÃ  4. Sá»‘ lá»›n hÆ¡n lÃ :",
            Options = JsonSerializer.Serialize(new[] { "$5$", "$6$", "$7$", "$8$" }),
            CorrectOption = 2,
            Explanation = "Gá»i sá»‘ lá»›n lÃ  $x$, sá»‘ bÃ© lÃ  $10-x$. Ta cÃ³ $x-(10-x)=4 \\Leftrightarrow 2x=14 \\Leftrightarrow x=7$.",
            OrderIndex = 1
        };

        var q22_2 = new Question
        {
            Id = Guid.NewGuid(),
            LessonId = lesson22Id,
            QuestionText = "Má»™t hÃ¬nh chá»¯ nháº­t cÃ³ chu vi 30cm, chiá»u dÃ i gáº¥p Ä‘Ã´i chiá»u rá»™ng. Diá»‡n tÃ­ch lÃ :",
            Options = JsonSerializer.Serialize(new[] { "$30\\text{cm}^2$", "$40\\text{cm}^2$", "$50\\text{cm}^2$", "$60\\text{cm}^2$" }),
            CorrectOption = 1,
            Explanation = "Gá»i chiá»u rá»™ng lÃ  $x$, chiá»u dÃ i lÃ  $2x$. Chu vi: $2(x+2x)=30 \\Rightarrow 6x=30 \\Rightarrow x=5$. Diá»‡n tÃ­ch: $5\\cdot10=50\\text{cm}^2$.",
            OrderIndex = 2
        };

        var q22_3 = new Question
        {
            Id = Guid.NewGuid(),
            LessonId = lesson22Id,
            QuestionText = "Tuá»•i cha gáº¥p 3 láº§n tuá»•i con. Sau 10 nÄƒm, tuá»•i cha gáº¥p Ä‘Ã´i tuá»•i con. Tuá»•i con hiá»‡n nay lÃ :",
            Options = JsonSerializer.Serialize(new[] { "$10$", "$15$", "$20$", "$25$" }),
            CorrectOption = 0,
            Explanation = "Gá»i tuá»•i con lÃ  $x$, tuá»•i cha lÃ  $3x$. Sau 10 nÄƒm: $3x+10=2(x+10) \\Rightarrow 3x+10=2x+20 \\Rightarrow x=10$.",
            OrderIndex = 3
        };

        var q22_4 = new Question
        {
            Id = Guid.NewGuid(),
            LessonId = lesson22Id,
            QuestionText = "Hai xe xuáº¥t phÃ¡t cÃ¹ng lÃºc tá»« cÃ¹ng má»™t Ä‘iá»ƒm. Xe A Ä‘i vá»›i váº­n tá»‘c 60km/h, xe B Ä‘i vá»›i váº­n tá»‘c 40km/h. Sau bao lÃ¢u chÃºng cÃ¡ch nhau 50km?",
            Options = JsonSerializer.Serialize(new[] { "1 giá»", "1.5 giá»", "2 giá»", "2.5 giá»" }),
            CorrectOption = 2,
            Explanation = "Hiá»‡u váº­n tá»‘c: $60-40=20$ km/h. Thá»i gian: $50/20=2.5$ giá».",
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
            Title = "HoÃ n thÃ nh ChÆ°Æ¡ng 1",
            Description = "HoÃ n thÃ nh táº¥t cáº£ bÃ i há»c trong ChÆ°Æ¡ng 1",
            IconUrl = "/images/badges/chapter1.png",
            ConditionType = "complete_chapter",
            ConditionValue = JsonSerializer.Serialize(new { chapterId = chapter1Id.ToString() })
        };

        var badge2 = new Badge
        {
            Id = badge2Id,
            Title = "Há»c sinh xuáº¥t sáº¯c",
            Description = "Äáº¡t 3 láº§n Ä‘iá»ƒm tuyá»‡t Ä‘á»‘i liÃªn tiáº¿p trong bÃ i kiá»ƒm tra",
            IconUrl = "/images/badges/excellent.png",
            ConditionType = "perfect_quiz_streak",
            ConditionValue = JsonSerializer.Serialize(new { streak = 3 })
        };

        var badge3 = new Badge
        {
            Id = badge3Id,
            Title = "NhÃ  toÃ¡n há»c",
            Description = "TÃ­ch lÅ©y 100 xu",
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
