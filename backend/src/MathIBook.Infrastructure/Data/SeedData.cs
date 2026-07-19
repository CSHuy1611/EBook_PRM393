using System.Text.Json;
using MathIBook.Domain.Entities;
using MathIBook.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MathIBook.Infrastructure.Data;

public static class SeedData
{
    // Mật khẩu demo chỉ phục vụ môi trường seed/kiểm thử, được hash trước khi lưu.
    private const string DemoStudentPassword = "Student@123";

    // Năm Student có số xu khác nhau để kiểm tra thứ tự bảng xếp hạng.
    private static readonly (string Name, string Email, int Coins)[] AdditionalStudentDefinitions =
    {
        ("Nguyễn Minh Anh", "student1@mathibook.vn", 1250),
        ("Trần Gia Huy", "student2@mathibook.vn", 980),
        ("Lê Khánh Linh", "student3@mathibook.vn", 760),
        ("Phạm Hoài Nam", "student4@mathibook.vn", 540),
        ("Võ Ngọc Mai", "student5@mathibook.vn", 320)
    };

    public static async Task SeedAsync(AppDbContext context)
    {
        // Database phát triển đã có User: không seed lại toàn bộ giáo trình.
        if (await context.Users.AnyAsync())
        {
            // Vẫn bổ sung Student mẫu còn thiếu và sửa rule badge legacy.
            await SeedAdditionalStudentsAsync(context);
            await RepairLegacyBadgeRuleThresholdsAsync(context);
            return;
        }

        // Database mới: tạo Admin, Student mặc định và các Student dùng cho ranking.
        var adminId = Guid.NewGuid();
        var studentId = Guid.NewGuid();

        context.Users.AddRange(
            new User
            {
                Id = adminId,
                Name = "Admin",
                Email = "admin@mathibook.vn",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                Role = "Admin",
                Coins = 0,
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = studentId,
                Name = "Học sinh",
                Email = "student@mathibook.vn",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Student@123"),
                Role = "Student",
                Coins = 0,
                CreatedAt = DateTime.UtcNow
            });
        // Hàm helper hash mật khẩu và gắn Role/Coins/timestamps thống nhất.
        context.Users.AddRange(CreateAdditionalStudents(AdditionalStudentDefinitions));

        var topicPoly = Id("poly"); var topicFrac = Id("frac");
        var topicFunc = Id("func"); var topicQuad = Id("quad");
        var topicThales = Id("thales"); var topicSimilar = Id("sim");
        var topicSolid = Id("solid"); var topicStat = Id("stat");

        context.CurriculumTopics.AddRange(
            new CurriculumTopic { Id = topicPoly, Code = "M8-ALG-POLY", Name = "Đa thức và hằng đẳng thức đáng nhớ", Strand = CurriculumStrand.NumbersAndAlgebra, Grade = 8, OrderIndex = 1 },
            new CurriculumTopic { Id = topicFrac, Code = "M8-ALG-FRAC", Name = "Phân thức đại số", Strand = CurriculumStrand.NumbersAndAlgebra, Grade = 8, OrderIndex = 2 },
            new CurriculumTopic { Id = topicFunc, Code = "M8-ALG-FUNC", Name = "Phương trình và hàm số bậc nhất", Strand = CurriculumStrand.NumbersAndAlgebra, Grade = 8, OrderIndex = 3 },
            new CurriculumTopic { Id = topicQuad, Code = "M8-GEO-QUAD", Name = "Tứ giác", Strand = CurriculumStrand.GeometryAndMeasurement, Grade = 8, OrderIndex = 4 },
            new CurriculumTopic { Id = topicThales, Code = "M8-GEO-THALES", Name = "Định lí Thalès trong tam giác", Strand = CurriculumStrand.GeometryAndMeasurement, Grade = 8, OrderIndex = 5 },
            new CurriculumTopic { Id = topicSimilar, Code = "M8-GEO-SIMILAR", Name = "Tam giác đồng dạng", Strand = CurriculumStrand.GeometryAndMeasurement, Grade = 8, OrderIndex = 6 },
            new CurriculumTopic { Id = topicSolid, Code = "M8-GEO-SOLID", Name = "Hình khối trong thực tiễn", Strand = CurriculumStrand.GeometryAndMeasurement, Grade = 8, OrderIndex = 7 },
            new CurriculumTopic { Id = topicStat, Code = "M8-STAT-PROB", Name = "Thống kê và xác suất", Strand = CurriculumStrand.StatisticsAndProbability, Grade = 8, OrderIndex = 8 });

        // ──────────────────────────────────────────────
        // CHAPTER 1: ĐA THỨC
        // ──────────────────────────────────────────────
        var c1 = Guid.NewGuid();
        context.Chapters.Add(new Chapter { Id = c1, Title = "Chương 1: Đa thức", Description = "Tìm hiểu về đơn thức, đa thức và các phép toán trên đa thức", OrderIndex = 1, CurriculumTopicId = topicPoly, IsPublished = true, PublishedAt = DateTime.UtcNow });

        var l1_1 = Lesson(c1, topicPoly, "Đơn thức", 1, null,
            "# Đơn thức\n\nĐơn thức là biểu thức đại số chỉ gồm một số, một biến, hoặc tích của các số và các biến.\n\n**Ví dụ:** $3x^2y$, $-5xy^3$, $7$, $x$, $\\frac{1}{2}xy$\n\n## Bậc của đơn thức\nBậc của đơn thức có hệ số khác 0 là tổng số mũ của tất cả các biến trong đơn thức.\n\n**Ví dụ:** Đơn thức $3x^2y^3$ có bậc là $2+3=5$.\n\n## Nhân hai đơn thức\nNhân hệ số với hệ số, nhân phần biến với phần biến.\n\n**Ví dụ:** $(2x^2y)(3xy^3) = (2\\cdot3)(x^2\\cdot x)(y\\cdot y^3) = 6x^3y^4$");
        var l1_2 = Lesson(c1, topicPoly, "Đa thức", 2, null,
            "# Đa thức\n\nĐa thức là tổng của những đơn thức. Mỗi đơn thức trong tổng gọi là một hạng tử của đa thức.\n\n**Ví dụ:** $3x^2y - 5xy + 2$ là đa thức có ba hạng tử.\n\n## Bậc của đa thức\nBậc của đa thức là bậc của hạng tử có bậc cao nhất trong dạng thu gọn.\n\n**Ví dụ:** Đa thức $4x^3 - 2x^2 + x - 7$ có bậc 3.\n\n## Thu gọn đa thức\nCộng các đơn thức đồng dạng (cùng phần biến) để thu gọn.\n\n**Ví dụ:** $3x^2y + 5x^2y - 2x^2y = (3+5-2)x^2y = 6x^2y$");
        var l1_3 = Lesson(c1, topicPoly, "Phép cộng và phép trừ đa thức", 3, null,
            "# Phép cộng và phép trừ đa thức\n\nĐể cộng (hay trừ) hai đa thức, ta viết chúng thành tổng (hay hiệu) rồi bỏ dấu ngoặc và thu gọn các đơn thức đồng dạng.\n\n## Ví dụ cộng\nCho $A = 3x^2 + 2xy - 5$ và $B = x^2 - 3xy + 4$.\n\n$A + B = (3x^2 + 2xy - 5) + (x^2 - 3xy + 4)$\n$= 3x^2 + 2xy - 5 + x^2 - 3xy + 4$\n$= (3x^2 + x^2) + (2xy - 3xy) + (-5 + 4)$\n$= 4x^2 - xy - 1$\n\n## Ví dụ trừ\n$A - B = (3x^2 + 2xy - 5) - (x^2 - 3xy + 4)$\n$= 3x^2 + 2xy - 5 - x^2 + 3xy - 4$\n$= (3x^2 - x^2) + (2xy + 3xy) + (-5 - 4)$\n$= 2x^2 + 5xy - 9$");
        var l1_4 = Lesson(c1, topicPoly, "Phép nhân đa thức", 4, null,
            "# Phép nhân đa thức\n\n## Nhân đơn thức với đa thức\nNhân đơn thức với từng hạng tử của đa thức rồi cộng các tích lại.\n\n$A(B + C) = AB + AC$\n\n**Ví dụ:** $2x(3x^2 - 5x + 1) = 6x^3 - 10x^2 + 2x$\n\n## Nhân đa thức với đa thức\nNhân mỗi hạng tử của đa thức này với từng hạng tử của đa thức kia rồi cộng các tích lại.\n\n$(A + B)(C + D) = AC + AD + BC + BD$\n\n**Ví dụ:** $(x+2)(x^2 - 3x + 1) = x\\cdot x^2 + x\\cdot(-3x) + x\\cdot1 + 2\\cdot x^2 + 2\\cdot(-3x) + 2\\cdot1$\n$= x^3 - 3x^2 + x + 2x^2 - 6x + 2 = x^3 - x^2 - 5x + 2$");
        var l1_5 = Lesson(c1, topicPoly, "Phép chia đa thức cho đơn thức", 5, null,
            "# Phép chia đa thức cho đơn thức\n\nMuốn chia đa thức $A$ cho đơn thức $B$ (trường hợp các hạng tử của $A$ đều chia hết cho $B$), ta chia mỗi hạng tử của $A$ cho $B$ rồi cộng các kết quả lại.\n\n**Ví dụ:** $(6x^3y^2 - 9x^2y^3 + 3xy^2) : 3xy^2$\n$= 6x^3y^2 : 3xy^2 - 9x^2y^3 : 3xy^2 + 3xy^2 : 3xy^2$\n$= 2x^2 - 3xy + 1$\n\nChú ý: $a^m : a^n = a^{m-n}$ với $m \\geq n$.");

        context.Lessons.AddRange(l1_1, l1_2, l1_3, l1_4, l1_5);

        var q = new List<Question>();
        Q(q, l1_1, "Đơn thức nào sau đây có bậc là 5?", new[] { "$2x^3y$", "$3x^2y^3$", "$5xy$", "$x^4$" }, 1, "Bậc của $3x^2y^3$ là $2+3=5$.");
        Q(q, l1_1, "Tích của $2x^2y$ và $3xy^3$ là:", new[] { "$5x^3y^4$", "$6x^3y^4$", "$6x^2y^3$", "$5x^2y^3$" }, 1, "$(2\\cdot3)(x^2\\cdot x)(y\\cdot y^3) = 6x^3y^4$.");
        Q(q, l1_1, "Đơn thức đồng dạng với $3x^2y$ là:", new[] { "$3xy^2$", "$-5x^2y$", "$3x^2y^2$", "$3xy$" }, 1, "Hai đơn thức đồng dạng có cùng phần biến $x^2y$.");
        Q(q, l1_1, "Kết quả của $(-2x^3)(3x^2)$ là:", new[] { "$-6x^5$", "$6x^5$", "$-6x^6$", "$-5x^5$" }, 0, "$-2\\cdot3\\cdot x^{3+2} = -6x^5$.");

        Q(q, l1_2, "Đa thức $3x^2y - 5xy^2 + 2x - 7$ có bậc là:", new[] { "$2$", "$3$", "$4$", "$1$" }, 1, "Bậc của $3x^2y$ là 3, của $-5xy^2$ là 3, vậy bậc đa thức là 3.");
        Q(q, l1_2, "Thu gọn $3x^2 + 5x - 2x^2 + 3x$ được:", new[] { "$x^2 + 8x$", "$5x^2 + 8x$", "$x^2 + 2x$", "$5x^2 + 2x$" }, 0, "$(3x^2-2x^2)+(5x+3x) = x^2 + 8x$.");
        Q(q, l1_2, "Đa thức $x^2y^3 - 2x^3y^2 + xy$ có bậc là:", new[] { "$3$", "$4$", "$5$", "$6$" }, 2, "$x^2y^3$ bậc 5, $-2x^3y^2$ bậc 5, $xy$ bậc 2. Đa thức bậc 5.");
        Q(q, l1_2, "Hạng tử nào không phải là hạng tử của đa thức $2x^2 - 3xy + y^2$?", new[] { "$2x^2$", "$-3xy$", "$y^2$", "$2xy$" }, 3, "Đa thức có các hạng tử $2x^2$, $-3xy$, $y^2$. $2xy$ không có trong đa thức.");

        Q(q, l1_3, "Cho $A = 2x^2 + 3xy - 1$, $B = x^2 - 2xy + 4$. $A+B$ bằng:", new[] { "$3x^2 + xy + 3$", "$3x^2 + 5xy - 5$", "$3x^2 - xy + 3$", "$x^2 + xy + 3$" }, 0, "$(2x^2+x^2)+(3xy-2xy)+(-1+4) = 3x^2 + xy + 3$.");
        Q(q, l1_3, "Kết quả của $(3x^2 - 2x + 1) - (x^2 + 3x - 2)$ là:", new[] { "$2x^2 - 5x + 3$", "$4x^2 + x - 1$", "$2x^2 + x + 3$", "$2x^2 - 5x - 1$" }, 0, "$(3x^2-x^2)+(-2x-3x)+(1+2) = 2x^2 - 5x + 3$.");
        Q(q, l1_3, "Thu gọn $(2x+3y)+(x-2y)$:", new[] { "$3x + y$", "$3x + 5y$", "$x + y$", "$3x - y$" }, 0, "$(2x+x)+(3y-2y) = 3x + y$.");
        Q(q, l1_3, "Hiệu của $(4x^2-2x+5) - (2x^2-3x+1)$ là:", new[] { "$2x^2 + x + 4$", "$2x^2 - 5x + 4$", "$6x^2 - 5x + 6$", "$2x^2 - x + 4$" }, 0, "$(4x^2-2x^2)+(-2x+3x)+(5-1) = 2x^2 + x + 4$.");

        Q(q, l1_4, "Kết quả của $2x(3x^2 - 5x + 1)$ là:", new[] { "$6x^3 - 10x^2 + 2x$", "$6x^2 - 10x + 2$", "$6x^3 + 10x^2 + 2x$", "$5x^3 - 10x^2 + 2x$" }, 0, "$2x\\cdot3x^2=6x^3$, $2x\\cdot(-5x)=-10x^2$, $2x\\cdot1=2x$.");
        Q(q, l1_4, "Tính $(x+2)(x-3)$:", new[] { "$x^2 - x - 6$", "$x^2 + x - 6$", "$x^2 - 5x - 6$", "$x^2 + 5x - 6$" }, 0, "$x\\cdot x + x\\cdot(-3) + 2\\cdot x + 2\\cdot(-3) = x^2 - 3x + 2x - 6 = x^2 - x - 6$.");
        Q(q, l1_4, "Kết quả của $(x-1)(x^2 + x + 1)$ là:", new[] { "$x^3 - 1$", "$x^3 + 1$", "$x^3 - x^2 - 1$", "$x^3 - 1$" }, 0, "$(x-1)(x^2+x+1) = x^3+x^2+x - x^2 - x - 1 = x^3-1$ (hằng đẳng thức).");
        Q(q, l1_4, "Tính $3x(x-2)$:", new[] { "$3x^2 - 6x$", "$3x^2 - 2x$", "$3x^2 - 6$", "$3x - 6$" }, 0, "$3x\\cdot x = 3x^2$, $3x\\cdot(-2) = -6x$.");

        Q(q, l1_5, "Kết quả của $(12x^3y^2) : (3xy)$ là:", new[] { "$4x^2y$", "$4x^3y$", "$4x^2y^2$", "$4xy$" }, 0, "$12:3=4$, $x^3:x=x^2$, $y^2:y=y$. Kết quả $4x^2y$.");
        Q(q, l1_5, "Tính $(6x^4 - 9x^3 + 3x^2) : 3x^2$:", new[] { "$2x^2 - 3x + 1$", "$2x^2 - 3x$", "$2x^2 - 9x + 3$", "$2x^2 - 3x + 3$" }, 0, "$6x^4:3x^2=2x^2$, $-9x^3:3x^2=-3x$, $3x^2:3x^2=1$.");
        Q(q, l1_5, "Kết quả của $(20x^5y^3) : (-5x^2y)$ là:", new[] { "$-4x^3y^2$", "$4x^3y^2$", "$-4x^3y^3$", "$-4x^7y^4$" }, 0, "$20:(-5)=-4$, $x^5:x^2=x^3$, $y^3:y=y^2$.");
        Q(q, l1_5, "Chia $(8x^3y^2 - 12x^2y^3) : 4x^2y$ được:", new[] { "$2xy - 3y^2$", "$2xy - 3xy^2$", "$2x - 3y$", "$2x^2y - 3xy^2$" }, 0, "$8x^3y^2:4x^2y=2xy$, $-12x^2y^3:4x^2y=-3y^2$.");

        // ──────────────────────────────────────────────
        // CHAPTER 2: HẰNG ĐẲNG THỨC ĐÁNG NHỚ VÀ ỨNG DỤNG
        // ──────────────────────────────────────────────
        var c2 = Guid.NewGuid();
        context.Chapters.Add(new Chapter { Id = c2, Title = "Chương 2: Hằng đẳng thức đáng nhớ và ứng dụng", Description = "Các hằng đẳng thức đáng nhớ và phân tích đa thức thành nhân tử", OrderIndex = 2, CurriculumTopicId = topicPoly, IsPublished = true, PublishedAt = DateTime.UtcNow });

        var l2_1 = Lesson(c2, topicPoly, "Hiệu hai bình phương và bình phương của tổng, hiệu", 1, null,
            "# Hiệu hai bình phương\n\n$$A^2 - B^2 = (A-B)(A+B)$$\n\n**Ví dụ:** $x^2 - 9 = (x-3)(x+3)$\n\n## Bình phương của một tổng\n\n$$(A+B)^2 = A^2 + 2AB + B^2$$\n\n**Ví dụ:** $(x+3)^2 = x^2 + 6x + 9$\n\n## Bình phương của một hiệu\n\n$$(A-B)^2 = A^2 - 2AB + B^2$$\n\n**Ví dụ:** $(2x-1)^2 = 4x^2 - 4x + 1$");
        var l2_2 = Lesson(c2, topicPoly, "Lập phương của tổng và lập phương của hiệu", 2, null,
            "# Lập phương của một tổng\n\n$$(A+B)^3 = A^3 + 3A^2B + 3AB^2 + B^3$$\n\n**Ví dụ:** $(x+1)^3 = x^3 + 3x^2 + 3x + 1$\n\n## Lập phương của một hiệu\n\n$$(A-B)^3 = A^3 - 3A^2B + 3AB^2 - B^3$$\n\n**Ví dụ:** $(x-2)^3 = x^3 - 6x^2 + 12x - 8$");
        var l2_3 = Lesson(c2, topicPoly, "Tổng và hiệu hai lập phương", 3, null,
            "# Tổng hai lập phương\n\n$$A^3 + B^3 = (A+B)(A^2 - AB + B^2)$$\n\n**Ví dụ:** $x^3 + 8 = x^3 + 2^3 = (x+2)(x^2 - 2x + 4)$\n\n## Hiệu hai lập phương\n\n$$A^3 - B^3 = (A-B)(A^2 + AB + B^2)$$\n\n**Ví dụ:** $27 - y^3 = 3^3 - y^3 = (3-y)(9 + 3y + y^2)$\n\n### Bảy hằng đẳng thức đáng nhớ\n1. $(A+B)^2 = A^2 + 2AB + B^2$\n2. $(A-B)^2 = A^2 - 2AB + B^2$\n3. $A^2 - B^2 = (A-B)(A+B)$\n4. $(A+B)^3 = A^3 + 3A^2B + 3AB^2 + B^3$\n5. $(A-B)^3 = A^3 - 3A^2B + 3AB^2 - B^3$\n6. $A^3 + B^3 = (A+B)(A^2 - AB + B^2)$\n7. $A^3 - B^3 = (A-B)(A^2 + AB + B^2)$");
        var l2_4 = Lesson(c2, topicPoly, "Phân tích đa thức thành nhân tử", 4, null,
            "# Phân tích đa thức thành nhân tử\n\nPhân tích đa thức thành nhân tử là biến đổi đa thức thành tích của các đa thức.\n\n## Các phương pháp\n\n### 1. Đặt nhân tử chung\n$AB + AC = A(B+C)$\n**Ví dụ:** $3x^2 + 6x = 3x(x+2)$\n\n### 2. Dùng hằng đẳng thức\n**Ví dụ:** $x^2 - 4 = (x-2)(x+2)$\n\n### 3. Nhóm hạng tử\n**Ví dụ:** $x^2 - 2x + xy - 2y = (x^2 - 2x) + (xy - 2y) = x(x-2) + y(x-2) = (x-2)(x+y)$\n\n### 4. Phối hợp nhiều phương pháp\n**Ví dụ:** $2x^3 - 8x = 2x(x^2 - 4) = 2x(x-2)(x+2)$");

        context.Lessons.AddRange(l2_1, l2_2, l2_3, l2_4);

        Q(q, l2_1, "Khai triển $(x+3)^2$:", new[] { "$x^2 + 6x + 9$", "$x^2 + 3x + 9$", "$x^2 + 6x + 6$", "$x^2 + 9$" }, 0, "$(x+3)^2 = x^2 + 2\\cdot x\\cdot3 + 3^2 = x^2 + 6x + 9$.");
        Q(q, l2_1, "Khai triển $(2x-1)^2$:", new[] { "$4x^2 - 4x + 1$", "$4x^2 - 2x + 1$", "$2x^2 - 4x + 1$", "$4x^2 + 4x + 1$" }, 0, "$(2x-1)^2 = (2x)^2 - 2\\cdot2x\\cdot1 + 1^2 = 4x^2 - 4x + 1$.");
        Q(q, l2_1, "Viết $x^2 - 16$ thành tích:", new[] { "$(x-4)(x+4)$", "$(x-8)(x+8)$", "$(x-4)^2$", "$(x+4)^2$" }, 0, "$x^2 - 16 = x^2 - 4^2 = (x-4)(x+4)$.");
        Q(q, l2_1, "Giá trị của $101^2 - 99^2$ là:", new[] { "$200$", "$400$", "$40$", "$20$" }, 1, "$101^2-99^2 = (101-99)(101+99) = 2\\cdot200 = 400$.");

        Q(q, l2_2, "Khai triển $(x+2)^3$:", new[] { "$x^3 + 6x^2 + 12x + 8$", "$x^3 + 6x^2 + 6x + 8$", "$x^3 + 3x^2 + 12x + 8$", "$x^3 + 6x^2 + 12x + 4$" }, 0, "$(x+2)^3 = x^3 + 3\\cdot x^2\\cdot2 + 3\\cdot x\\cdot4 + 8 = x^3 + 6x^2 + 12x + 8$.");
        Q(q, l2_2, "Khai triển $(2x-1)^3$:", new[] { "$8x^3 - 12x^2 + 6x - 1$", "$8x^3 - 12x^2 + 6x + 1$", "$8x^3 - 6x^2 + 6x - 1$", "$2x^3 - 12x^2 + 6x - 1$" }, 0, "$(2x-1)^3 = (2x)^3 - 3\\cdot(2x)^2\\cdot1 + 3\\cdot2x\\cdot1 - 1 = 8x^3 - 12x^2 + 6x - 1$.");
        Q(q, l2_2, "Tính $(x+1)^3$:", new[] { "$x^3 + 3x^2 + 3x + 1$", "$x^3 + 3x^2 + 3x$", "$x^3 + 3x^2 + 1$", "$x^3 + 3x + 1$" }, 0, "$(x+1)^3 = x^3 + 3x^2 + 3x + 1$.");
        Q(q, l2_2, "Khai triển $(x-3)^3$:", new[] { "$x^3 - 9x^2 + 27x - 27$", "$x^3 - 9x^2 + 27x + 27$", "$x^3 - 27$", "$x^3 - 9x^2 - 27x - 27$" }, 0, "$(x-3)^3 = x^3 - 3x^2\\cdot3 + 3x\\cdot9 - 27 = x^3 - 9x^2 + 27x - 27$.");

        Q(q, l2_3, "Phân tích $x^3 + 27$ thành nhân tử:", new[] { "$(x+3)(x^2 - 3x + 9)$", "$(x+3)(x^2 + 3x + 9)$", "$(x-3)(x^2 + 3x + 9)$", "$(x+27)(x^2 - 27x + 729)$" }, 0, "$x^3+27 = x^3+3^3 = (x+3)(x^2 - 3x + 9)$.");
        Q(q, l2_3, "Phân tích $8 - y^3$ thành nhân tử:", new[] { "$(2-y)(4+2y+y^2)$", "$(2+y)(4-2y+y^2)$", "$(2-y)(4-2y+y^2)$", "$(8-y)(64+8y+y^2)$" }, 0, "$8-y^3 = 2^3-y^3 = (2-y)(4+2y+y^2)$.");
        Q(q, l2_3, "Khai triển hằng đẳng thức $A^3 - B^3$:", new[] { "$(A-B)(A^2+AB+B^2)$", "$(A-B)(A^2-AB+B^2)$", "$(A+B)(A^2-AB+B^2)$", "$(A-B)(A^2+B^2)$" }, 0, "Hiệu hai lập phương: $A^3-B^3 = (A-B)(A^2+AB+B^2)$.");
        Q(q, l2_3, "Viết $x^3+8$ dưới dạng tích:", new[] { "$(x+2)(x^2-2x+4)$", "$(x-2)(x^2+2x+4)$", "$(x+2)(x^2+2x+4)$", "$(x+8)(x^2-8x+64)$" }, 0, "$x^3+8 = x^3+2^3 = (x+2)(x^2-2x+4)$.");

        Q(q, l2_4, "Phân tích $2x^2 - 6x$ thành nhân tử:", new[] { "$2x(x-3)$", "$2(x^2-3x)$", "$x(2x-6)$", "$2x(x+3)$" }, 0, "Đặt $2x$ làm nhân tử chung: $2x(x-3)$.");
        Q(q, l2_4, "Phân tích $x^2 - 4x + 4$ thành nhân tử:", new[] { "$(x-2)^2$", "$(x+2)^2$", "$(x-4)^2$", "$x(x-4)+4$" }, 0, "$x^2-4x+4 = x^2 - 2\\cdot x\\cdot2 + 2^2 = (x-2)^2$.");
        Q(q, l2_4, "Phân tích $x^2 - xy + x - y$:", new[] { "$(x-y)(x+1)$", "$(x+y)(x-1)$", "$(x-y)(x-1)$", "$x(x-y+1)-y$" }, 0, "Nhóm: $(x^2-xy)+(x-y)=x(x-y)+(x-y)=(x-y)(x+1)$.");
        Q(q, l2_4, "Phân tích $3x^3 - 12x$:", new[] { "$3x(x-2)(x+2)$", "$3x(x^2-4)$", "$3(x^3-4x)$", "$x(3x^2-12)$" }, 0, "$3x(x^2-4) = 3x(x-2)(x+2)$.");

        // ──────────────────────────────────────────────
        // CHAPTER 3: PHÂN THỨC ĐẠI SỐ
        // ──────────────────────────────────────────────
        var c3 = Guid.NewGuid();
        context.Chapters.Add(new Chapter { Id = c3, Title = "Chương 3: Phân thức đại số", Description = "Khái niệm phân thức, tính chất và các phép toán trên phân thức", OrderIndex = 3, CurriculumTopicId = topicFrac, IsPublished = true, PublishedAt = DateTime.UtcNow });

        var l3_1 = Lesson(c3, topicFrac, "Phân thức đại số", 1, null,
            "# Phân thức đại số\n\nPhân thức đại số có dạng $\\frac{A}{B}$ trong đó $A, B$ là các đa thức và $B \\neq 0$.\n\n$A$ gọi là tử thức, $B$ gọi là mẫu thức.\n\n**Ví dụ:** $\\frac{2x+3}{x-1}$, $\\frac{x^2+1}{x-2}$\n\nHai phân thức bằng nhau: $\\frac{A}{B} = \\frac{C}{D}$ nếu $A\\cdot D = B\\cdot C$.");
        var l3_2 = Lesson(c3, topicFrac, "Tính chất cơ bản của phân thức đại số", 2, null,
            "# Tính chất cơ bản của phân thức đại số\n\n## Tính chất\nNếu nhân (hoặc chia) cả tử và mẫu của một phân thức với cùng một đa thức khác 0 thì được phân thức bằng phân thức đã cho.\n\n$$\\frac{A}{B} = \\frac{A\\cdot M}{B\\cdot M} \\quad (M \\neq 0)$$\n$$\\frac{A}{B} = \\frac{A:N}{B:N} \\quad (N \\text{ là nhân tử chung})$$\n\n## Rút gọn phân thức\nChia cả tử và mẫu cho nhân tử chung.\n\n**Ví dụ:** $\\frac{2x^2-2x}{x-1} = \\frac{2x(x-1)}{x-1} = 2x$");
        var l3_3 = Lesson(c3, topicFrac, "Phép cộng và phép trừ phân thức đại số", 3, null,
            "# Phép cộng và phép trừ phân thức đại số\n\n## Cộng hai phân thức cùng mẫu\n$$\\frac{A}{M} + \\frac{B}{M} = \\frac{A+B}{M}$$\n\n**Ví dụ:** $\\frac{x}{x+1} + \\frac{2}{x+1} = \\frac{x+2}{x+1}$\n\n## Cộng hai phân thức khác mẫu\nQuy đồng mẫu thức rồi cộng.\n\n**Ví dụ:** $\\frac{x}{x-1} + \\frac{2}{x+1} = \\frac{x(x+1)+2(x-1)}{(x-1)(x+1)} = \\frac{x^2+x+2x-2}{x^2-1} = \\frac{x^2+3x-2}{x^2-1}$\n\nPhép trừ tương tự: $\\frac{A}{B} - \\frac{C}{D} = \\frac{A}{B} + \\left(-\\frac{C}{D}\\right)$");
        var l3_4 = Lesson(c3, topicFrac, "Phép nhân và phép chia phân thức đại số", 4, null,
            "# Phép nhân và phép chia phân thức đại số\n\n## Nhân hai phân thức\n$$\\frac{A}{B} \\cdot \\frac{C}{D} = \\frac{A\\cdot C}{B\\cdot D}$$\n\n**Ví dụ:** $\\frac{x}{x+1} \\cdot \\frac{x-1}{x} = \\frac{x(x-1)}{(x+1)x} = \\frac{x-1}{x+1}$\n\n## Chia hai phân thức\n$$\\frac{A}{B} : \\frac{C}{D} = \\frac{A}{B} \\cdot \\frac{D}{C} \\quad (C \\neq 0)$$\n\n**Ví dụ:** $\\frac{x^2}{y} : \\frac{x}{y^2} = \\frac{x^2}{y} \\cdot \\frac{y^2}{x} = xy$");

        context.Lessons.AddRange(l3_1, l3_2, l3_3, l3_4);

        Q(q, l3_1, "Phân thức $\\frac{2x+1}{x-3}$ xác định khi:", new[] { "$x \\neq 3$", "$x \\neq 0$", "$x \\neq -3$", "$x \\neq \\frac{1}{2}$" }, 0, "Phân thức xác định khi mẫu $x-3 \\neq 0 \\Rightarrow x \\neq 3$.");
        Q(q, l3_1, "Phân thức nào bằng $\\frac{x}{x-1}$?", new[] { "$\\frac{2x}{2x-2}$", "$\\frac{x+1}{x}$", "$\\frac{x^2}{x^2-1}$", "$\\frac{1}{1-x}$" }, 0, "$\\frac{2x}{2x-2} = \\frac{2x}{2(x-1)} = \\frac{x}{x-1}$.");
        Q(q, l3_1, "Điều kiện xác định của $\\frac{x+2}{x^2-4}$ là:", new[] { "$x \\neq \\pm 2$", "$x \\neq 2$", "$x \\neq -2$", "$x \\neq 0$" }, 0, "$x^2-4=(x-2)(x+2) \\neq 0 \\Rightarrow x \\neq 2$ và $x \\neq -2$.");
        Q(q, l3_1, "Phân thức $\\frac{3}{x-1}$ có tử thức là:", new[] { "$3$", "$x-1$", "$3x-3$", "$\\frac{3}{x-1}$" }, 0, "Tử thức là 3, mẫu thức là $x-1$.");

        Q(q, l3_2, "Rút gọn $\\frac{4x^2y}{6xy^2}$:", new[] { "$\\frac{2x}{3y}$", "$\\frac{2xy}{3}$", "$\\frac{4x}{6y}$", "$\\frac{2x^2}{3y^2}$" }, 0, "Chia cả tử và mẫu cho $2xy$: $\\frac{2x}{3y}$.");
        Q(q, l3_2, "Rút gọn $\\frac{x^2-1}{x+1}$:", new[] { "$x-1$", "$x+1$", "$1$", "$\\frac{x-1}{x+1}$" }, 0, "$\\frac{(x-1)(x+1)}{x+1} = x-1$.");
        Q(q, l3_2, "Kết quả rút gọn $\\frac{x^2-4x+4}{x-2}$:", new[] { "$x-2$", "$x+2$", "$2$", "$x-4$" }, 0, "$\\frac{(x-2)^2}{x-2} = x-2$.");
        Q(q, l3_2, "Đa thức thích hợp điền vào chỗ trống: $\\frac{x-1}{x+1} = \\frac{(x-1)^2}{(x+1)(...)}$", new[] { "$x-1$", "$x+1$", "$1$", "$x^2-1$" }, 0, "Nhân tử và mẫu với $(x-1)$, mẫu mới là $(x+1)(x-1)$.");

        Q(q, l3_3, "Tính $\\frac{2x}{x+1} + \\frac{3}{x+1}$:", new[] { "$\\frac{2x+3}{x+1}$", "$\\frac{5x}{x+1}$", "$\\frac{2x+3}{2x+2}$", "$\\frac{6x}{x+1}$" }, 0, "Cùng mẫu: $\\frac{2x+3}{x+1}$.");
        Q(q, l3_3, "Tính $\\frac{1}{x} + \\frac{1}{x+1}$:", new[] { "$\\frac{2x+1}{x(x+1)}$", "$\\frac{2}{2x+1}$", "$\\frac{1}{x(x+1)}$", "$\\frac{2}{x(x+1)}$" }, 0, "Mẫu chung $x(x+1)$: $\\frac{x+1+x}{x(x+1)} = \\frac{2x+1}{x(x+1)}$.");
        Q(q, l3_3, "Kết quả của $\\frac{x}{x-2} - \\frac{2}{x-2}$ là:", new[] { "$1$", "$\\frac{x-2}{x-2}$", "$\\frac{x+2}{x-2}$", "$0$" }, 0, "$\\frac{x-2}{x-2} = 1$.");
        Q(q, l3_3, "Tính $\\frac{x}{x-1} - \\frac{1}{x}$:", new[] { "$\\frac{x^2 - x + 1}{x(x-1)}$", "$\\frac{x^2 - x - 1}{x(x-1)}$", "$\\frac{x-1}{x(x-1)}$", "$1$" }, 0, "$\\frac{x^2}{x(x-1)} - \\frac{x-1}{x(x-1)} = \\frac{x^2 - x + 1}{x(x-1)}$.");

        Q(q, l3_4, "Tính $\\frac{x}{y} \\cdot \\frac{y}{x}$:", new[] { "$1$", "$\\frac{xy}{xy}$", "$\\frac{x^2}{y^2}$", "$\\frac{y}{x}$" }, 0, "$\\frac{x}{y} \\cdot \\frac{y}{x} = \\frac{xy}{xy} = 1$.");
        Q(q, l3_4, "Kết quả của $\\frac{x+1}{x-1} : \\frac{x+1}{x}$ là:", new[] { "$\\frac{x}{x-1}$", "$\\frac{x-1}{x}$", "$1$", "$\\frac{(x+1)^2}{x(x-1)}$" }, 0, "$\\frac{x+1}{x-1} \\cdot \\frac{x}{x+1} = \\frac{x}{x-1}$.");
        Q(q, l3_4, "Tính $\\frac{2x}{3y^2} \\cdot \\frac{6y}{x}$:", new[] { "$\\frac{4}{y}$", "$\\frac{12x}{3xy}$", "$\\frac{4x}{y}$", "$\\frac{12}{3y}$" }, 0, "$\\frac{2x\\cdot6y}{3y^2\\cdot x} = \\frac{12xy}{3xy^2} = \\frac{4}{y}$.");
        Q(q, l3_4, "Kết quả của $\\frac{x^2-1}{x} : \\frac{x+1}{x^2}$ là:", new[] { "$x(x-1)$", "$x(x+1)$", "$\\frac{x-1}{x}$", "$x^2-1$" }, 0, "$\\frac{(x-1)(x+1)}{x} \\cdot \\frac{x^2}{x+1} = x(x-1)$.");

        // ──────────────────────────────────────────────
        // CHAPTER 4: PHƯƠNG TRÌNH BẬC NHẤT VÀ HÀM SỐ BẬC NHẤT
        // ──────────────────────────────────────────────
        var c4 = Guid.NewGuid();
        context.Chapters.Add(new Chapter { Id = c4, Title = "Chương 4: Phương trình bậc nhất và hàm số bậc nhất", Description = "Phương trình bậc nhất một ẩn, giải bài toán bằng lập phương trình, hàm số và đồ thị", OrderIndex = 4, CurriculumTopicId = topicFunc, IsPublished = true, PublishedAt = DateTime.UtcNow });

        var l4_1 = Lesson(c4, topicFunc, "Phương trình bậc nhất một ẩn", 1, null,
            "# Phương trình bậc nhất một ẩn\n\nPhương trình bậc nhất một ẩn có dạng $ax + b = 0$ với $a \\neq 0$.\n\n## Cách giải\n\n$$ax + b = 0 \\Leftrightarrow ax = -b \\Leftrightarrow x = -\\frac{b}{a}$$\n\n**Ví dụ:** Giải $2x - 6 = 0$\n\n$2x = 6 \\Leftrightarrow x = 3$\n\n## Phương trình tích\n$A(x)\\cdot B(x) = 0 \\Leftrightarrow A(x) = 0$ hoặc $B(x) = 0$\n\n## Phương trình chứa ẩn ở mẫu\nTìm ĐKXĐ, quy đồng, khử mẫu và giải.");
        var l4_2 = Lesson(c4, topicFunc, "Giải bài toán bằng cách lập phương trình", 2, null,
            "# Giải bài toán bằng cách lập phương trình\n\n## Các bước giải\n1. Đặt ẩn và điều kiện cho ẩn\n2. Biểu diễn các đại lượng chưa biết qua ẩn\n3. Lập phương trình\n4. Giải phương trình\n5. Đối chiếu điều kiện và kết luận\n\n**Ví dụ:** Một xe máy đi từ A đến B với vận tốc 40 km/h, lúc về với vận tốc 30 km/h. Tổng thời gian 3,5 giờ. Tính quãng đường AB.\n\nGọi quãng đường AB là $x$ (km), $x > 0$.\n\nThời gian đi: $\\frac{x}{40}$ h, thời gian về: $\\frac{x}{30}$ h.\n\nPT: $\\frac{x}{40} + \\frac{x}{30} = 3,5$ \\Rightarrow $\\frac{7x}{120} = 3,5$ \\Rightarrow $x = 60$.\n\nVậy quãng đường AB dài 60 km.");
        var l4_3 = Lesson(c4, topicFunc, "Khái niệm hàm số và đồ thị của hàm số", 3, "linear_graph",
            "# Khái niệm hàm số\n\nHàm số là quy tắc tương ứng mỗi giá trị $x$ với một giá trị duy nhất $y$. Ký hiệu $y = f(x)$.\n\n## Mặt phẳng tọa độ\nTrục hoành $Ox$, trục tung $Oy$. Điểm $M(x_0;y_0)$ có hoành độ $x_0$, tung độ $y_0$.\n\n## Đồ thị hàm số\nTập hợp các điểm $(x;f(x))$ trên mặt phẳng tọa độ.\n\n**Ví dụ:** Hàm số $y = 2x$ có đồ thị là đường thẳng đi qua gốc tọa độ và điểm $(1;2)$.");
        var l4_4 = Lesson(c4, topicFunc, "Hàm số bậc nhất $y = ax + b$ và đồ thị", 4, "linear_graph",
            "# Hàm số bậc nhất\n\nHàm số bậc nhất có dạng $y = ax + b$ với $a \\neq 0$.\n\nĐồ thị là một đường thẳng.\n\n## Cách vẽ đồ thị\n1. Tìm giao với trục tung: $x=0 \\Rightarrow y=b$. Điểm $A(0;b)$.\n2. Tìm giao với trục hoành: $y=0 \\Rightarrow x = -\\frac{b}{a}$. Điểm $B(-\\frac{b}{a};0)$.\n3. Vẽ đường thẳng qua $A$ và $B$.\n\n**Ví dụ:** $y = 2x + 1$\n$A(0;1)$, $B(-\\frac{1}{2};0)$");
        var l4_5 = Lesson(c4, topicFunc, "Hệ số góc của đường thẳng", 5, "linear_graph",
            "# Hệ số góc của đường thẳng\n\nCho đường thẳng $y = ax + b$ ($a \\neq 0$). Hệ số $a$ gọi là hệ số góc.\n\n## Ý nghĩa\n- $a > 0$: góc nhọn, đường thẳng đi lên từ trái sang phải.\n- $a < 0$: góc tù, đường thẳng đi xuống từ trái sang phải.\n- Giá trị $|a|$ càng lớn, đường thẳng càng dốc.\n\n## Hai đường thẳng song song, cắt nhau\nCho $d_1: y = a_1x + b_1$, $d_2: y = a_2x + b_2$.\n- $d_1 \\parallel d_2$ khi $a_1 = a_2$ và $b_1 \\neq b_2$.\n- $d_1 \\equiv d_2$ khi $a_1 = a_2$ và $b_1 = b_2$.\n- $d_1$ cắt $d_2$ khi $a_1 \\neq a_2$.");

        context.Lessons.AddRange(l4_1, l4_2, l4_3, l4_4, l4_5);

        Q(q, l4_1, "Nghiệm của phương trình $3x - 9 = 0$ là:", new[] { "$x = 3$", "$x = -3$", "$x = 9$", "$x = 0$" }, 0, "$3x = 9 \\Rightarrow x = 3$.");
        Q(q, l4_1, "Giải phương trình $2x + 5 = x - 3$:", new[] { "$x = -8$", "$x = 8$", "$x = -2$", "$x = 2$" }, 0, "$2x - x = -3 - 5 \\Rightarrow x = -8$.");
        Q(q, l4_1, "Tập nghiệm của $(x-2)(x+3) = 0$ là:", new[] { "$\\{2;-3\\}$", "$\\{-2;3\\}$", "$\\{2;3\\}$", "$\\{-2;-3\\}$" }, 0, "$x-2=0$ hoặc $x+3=0 \\Rightarrow x=2$ hoặc $x=-3$.");
        Q(q, l4_1, "Điều kiện xác định của $\\frac{2x}{x-1} + \\frac{3}{x+2} = 0$ là:", new[] { "$x \\neq 1$ và $x \\neq -2$", "$x \\neq 1$", "$x \\neq -2$", "$x \\neq 0$" }, 0, "Mẫu $x-1 \\neq 0$ và $x+2 \\neq 0 \\Rightarrow x \\neq 1$ và $x \\neq -2$.");

        Q(q, l4_2, "Một số gấp đôi số kia, tổng của chúng là 30. Số lớn là:", new[] { "$20$", "$10$", "$15$", "$25$" }, 0, "Gọi số bé là $x$, số lớn $2x$. $x+2x=30 \\Rightarrow x=10$, số lớn $20$.");
        Q(q, l4_2, "Chu vi hình chữ nhật 40cm, chiều dài hơn chiều rộng 4cm. Diện tích là:", new[] { "$96\\text{cm}^2$", "$84\\text{cm}^2$", "$120\\text{cm}^2$", "$80\\text{cm}^2$" }, 0, "Rộng $x$, dài $x+4$. $2(x+x+4)=40 \\Rightarrow x=8$, dài $12$. DT $8\\cdot12=96$.");
        Q(q, l4_2, "Hiệu hai số là 12, số lớn gấp 3 lần số bé. Số bé là:", new[] { "$6$", "$4$", "$8$", "$12$" }, 0, "Số bé $x$, số lớn $3x$. $3x-x=12 \\Rightarrow x=6$.");
        Q(q, l4_2, "Một ca nô xuôi dòng 60km hết 2 giờ, vận tốc nước 3km/h. Vận tốc riêng:", new[] { "$27$ km/h", "$30$ km/h", "$24$ km/h", "$33$ km/h" }, 0, "Vận tốc xuôi $60:2=30$ km/h. Vận tốc riêng $30-3=27$ km/h.");

        Q(q, l4_3, "Điểm $M(-2;3)$ có hoành độ là:", new[] { "$-2$", "$3$", "$-3$", "$2$" }, 0, "Điểm $(x;y)$ có hoành độ $x$. Ở đây $x=-2$.");
        Q(q, l4_3, "Đồ thị hàm số $y = 3x$ đi qua điểm:", new[] { "$(1;3)$", "$(3;1)$", "$(0;3)$", "$(1;0)$" }, 0, "Thay $x=1$ được $y=3$. Điểm $(1;3)$.");
        Q(q, l4_3, "Cho $f(x) = 2x + 1$, tính $f(2)$:", new[] { "$5$", "$3$", "$4$", "$2$" }, 0, "$f(2) = 2\\cdot2 + 1 = 5$.");
        Q(q, l4_3, "Điểm nào thuộc đồ thị hàm số $y = -x + 2$?", new[] { "$(1;1)$", "$(-1;1)$", "$(0;-2)$", "$(2;0)$" }, 0, "Thay $x=1$: $y = -1+2 = 1$. Điểm $(1;1)$ thuộc đồ thị.");

        Q(q, l4_4, "Hàm số $y = 2x - 1$ cắt trục tung tại điểm:", new[] { "$(0;-1)$", "$(0;1)$", "$(-1;0)$", "$(1;0)$" }, 0, "Cho $x=0$: $y = -1$. Điểm $(0;-1)$.");
        Q(q, l4_4, "Hàm số $y = -3x + 6$ cắt trục hoành tại:", new[] { "$(2;0)$", "$(0;6)$", "$(-2;0)$", "$(6;0)$" }, 0, "Cho $y=0$: $-3x+6=0 \\Rightarrow x=2$. Điểm $(2;0)$.");
        Q(q, l4_4, "Hàm số nào sau đây là hàm số bậc nhất?", new[] { "$y = 5$", "$y = 2x^2 + 1$", "$y = \\frac{1}{2}x - 3$", "$y = \\frac{2}{x}$" }, 2, "Hàm số bậc nhất có dạng $y=ax+b$. $y = \\frac{1}{2}x - 3$ thỏa mãn.");
        Q(q, l4_4, "Đồ thị hàm số $y = ax + b$ ($a \\neq 0$) là:", new[] { "Một đường thẳng", "Một parabol", "Một đường cong", "Một đường tròn" }, 0, "Đồ thị hàm số bậc nhất là một đường thẳng.");

        Q(q, l4_5, "Hệ số góc của đường thẳng $y = 3x - 5$ là:", new[] { "$3$", "$-5$", "$5$", "$-3$" }, 0, "Hệ số góc là $a=3$ trong $y=ax+b$.");
        Q(q, l4_5, "Đường thẳng nào song song với $y = 2x + 1$?", new[] { "$y = 2x - 3$", "$y = -2x + 1$", "$y = x + 1$", "$y = 2x + 1$" }, 0, "Hai đường thẳng song song khi $a_1=a_2$ và $b_1 \\neq b_2$.");
        Q(q, l4_5, "Tìm $m$ để $y = (m-2)x + 3$ đồng biến:", new[] { "$m > 2$", "$m < 2$", "$m \\neq 2$", "$m = 2$" }, 0, "Hàm số đồng biến khi $a > 0 \\Rightarrow m-2 > 0 \\Rightarrow m > 2$.");
        Q(q, l4_5, "Góc tạo bởi $y = -x + 2$ với trục $Ox$ là:", new[] { "Góc tù", "Góc nhọn", "Góc vuông", "Góc bẹt" }, 0, "Hệ số góc $a = -1 < 0$ nên góc là góc tù.");

        // ──────────────────────────────────────────────
        // CHAPTER 5: TỨ GIÁC
        // ──────────────────────────────────────────────
        var c5 = Guid.NewGuid();
        context.Chapters.Add(new Chapter { Id = c5, Title = "Chương 5: Tứ giác", Description = "Tứ giác, hình thang cân, hình bình hành, hình chữ nhật, hình thoi, hình vuông", OrderIndex = 5, CurriculumTopicId = topicQuad, IsPublished = true, PublishedAt = DateTime.UtcNow });

        var l5_1 = Lesson(c5, topicQuad, "Tứ giác", 1, null,
            "# Tứ giác\n\nTứ giác $ABCD$ là hình gồm bốn đoạn thẳng $AB, BC, CD, DA$.\n\n## Tổng các góc của tứ giác\n\n$$\\widehat{A} + \\widehat{B} + \\widehat{C} + \\widehat{D} = 360^\\circ$$\n\n**Ví dụ:** Cho tứ giác $ABCD$ có $\\widehat{A}=120^\\circ$, $\\widehat{B}=80^\\circ$, $\\widehat{C}=60^\\circ$. Tính $\\widehat{D}$.\n\n$\\widehat{D} = 360^\\circ - (120^\\circ + 80^\\circ + 60^\\circ) = 100^\\circ$.");
        var l5_2 = Lesson(c5, topicQuad, "Hình thang cân", 2, null,
            "# Hình thang cân\n\nHình thang là tứ giác có hai cạnh đối song song.\n\nHình thang cân là hình thang có hai góc kề một đáy bằng nhau.\n\n## Tính chất\n- Hai cạnh bên bằng nhau.\n- Hai đường chéo bằng nhau.\n\n## Dấu hiệu nhận biết\n- Hình thang có hai góc kề một đáy bằng nhau là hình thang cân.\n- Hình thang có hai đường chéo bằng nhau là hình thang cân.");
        var l5_3 = Lesson(c5, topicQuad, "Hình bình hành", 3, null,
            "# Hình bình hành\n\nHình bình hành là tứ giác có các cạnh đối song song.\n\n## Tính chất\n- Các cạnh đối bằng nhau.\n- Các góc đối bằng nhau.\n- Hai đường chéo cắt nhau tại trung điểm của mỗi đường.\n\n## Dấu hiệu nhận biết\n- Tứ giác có các cạnh đối song song.\n- Tứ giác có các cạnh đối bằng nhau.\n- Tứ giác có hai cạnh đối song song và bằng nhau.\n- Tứ giác có các góc đối bằng nhau.\n- Tứ giác có hai đường chéo cắt nhau tại trung điểm mỗi đường.");
        var l5_4 = Lesson(c5, topicQuad, "Hình chữ nhật", 4, null,
            "# Hình chữ nhật\n\nHình chữ nhật là tứ giác có bốn góc vuông.\n\n## Tính chất\n- Có tất cả tính chất của hình bình hành, hình thang cân.\n- Hai đường chéo bằng nhau và cắt nhau tại trung điểm mỗi đường.\n\n## Dấu hiệu nhận biết\n- Tứ giác có ba góc vuông.\n- Hình thang cân có một góc vuông.\n- Hình bình hành có một góc vuông.\n- Hình bình hành có hai đường chéo bằng nhau.");
        var l5_5 = Lesson(c5, topicQuad, "Hình thoi và hình vuông", 5, null,
            "# Hình thoi\n\nHình thoi là tứ giác có bốn cạnh bằng nhau.\n\n**Tính chất:** Hai đường chéo vuông góc và là phân giác các góc.\n\n# Hình vuông\n\nHình vuông là tứ giác có bốn góc vuông và bốn cạnh bằng nhau.\n\nHình vuông vừa là hình chữ nhật, vừa là hình thoi.\n\n**Tính chất:** Hai đường chéo bằng nhau, vuông góc, cắt nhau tại trung điểm mỗi đường và là phân giác các góc.");

        context.Lessons.AddRange(l5_1, l5_2, l5_3, l5_4, l5_5);

        Q(q, l5_1, "Tổng các góc trong của một tứ giác bằng:", new[] { "$180^\\circ$", "$270^\\circ$", "$360^\\circ$", "$90^\\circ$" }, 2, "Tổng các góc của tứ giác là $360^\\circ$.");
        Q(q, l5_1, "Tứ giác có $\\widehat{A}=100^\\circ$, $\\widehat{B}=80^\\circ$, $\\widehat{C}=70^\\circ$. Số đo $\\widehat{D}$ là:", new[] { "$110^\\circ$", "$100^\\circ$", "$120^\\circ$", "$90^\\circ$" }, 0, "$\\widehat{D}=360^\\circ-(100^\\circ+80^\\circ+70^\\circ)=110^\\circ$.");
        Q(q, l5_1, "Một tứ giác có thể có tối đa bao nhiêu góc vuông?", new[] { "$4$", "$2$", "$3$", "$1$" }, 0, "Tứ giác có thể có tối đa 4 góc vuông (hình chữ nhật).");
        Q(q, l5_1, "Tứ giác có hai đường chéo bằng nhau có thể là:", new[] { "Hình thang cân", "Hình thoi", "Hình bình hành", "Tất cả đều sai" }, 0, "Hình thang cân có hai đường chéo bằng nhau.");

        Q(q, l5_2, "Hình thang cân có:", new[] { "Hai cạnh bên bằng nhau", "Hai cạnh đáy bằng nhau", "Hai đường chéo vuông góc", "Các góc đối bằng nhau" }, 0, "Hình thang cân có hai cạnh bên bằng nhau, hai đường chéo bằng nhau.");
        Q(q, l5_2, "Một hình thang có đáy lớn 12cm, đáy bé 8cm thì đường trung bình dài:", new[] { "$10$cm", "$20$cm", "$4$cm", "$96$cm" }, 0, "Đường trung bình $= \\frac{12+8}{2} = 10$cm.");
        Q(q, l5_2, "Hình thang cân $ABCD$ ($AB \\parallel CD$) có $\\widehat{A}=110^\\circ$. Số đo $\\widehat{B}$ là:", new[] { "$110^\\circ$", "$70^\\circ$", "$90^\\circ$", "$55^\\circ$" }, 0, "Trong hình thang cân, hai góc kề một đáy bằng nhau nên $\\widehat{B}=\\widehat{A}=110^\\circ$.");
        Q(q, l5_2, "Dấu hiệu nhận biết hình thang cân:", new[] { "Hai đường chéo bằng nhau", "Hai cạnh bên song song", "Hai cạnh đáy bằng nhau", "Các góc đối bằng nhau" }, 0, "Hình thang có hai đường chéo bằng nhau là hình thang cân.");

        Q(q, l5_3, "Hình bình hành có:", new[] { "Các cạnh đối song song", "Các cạnh bằng nhau", "Hai đường chéo bằng nhau", "Các góc vuông" }, 0, "Hình bình hành có các cạnh đối song song và bằng nhau.");
        Q(q, l5_3, "Trong hình bình hành, hai đường chéo:", new[] { "Cắt nhau tại trung điểm mỗi đường", "Bằng nhau", "Vuông góc", "Là phân giác các góc" }, 0, "Hai đường chéo của hình bình hành cắt nhau tại trung điểm mỗi đường.");
        Q(q, l5_3, "Tứ giác có các cạnh đối bằng nhau là:", new[] { "Hình bình hành", "Hình thang cân", "Hình thoi", "Hình chữ nhật" }, 0, "Tứ giác có các cạnh đối bằng nhau là hình bình hành.");
        Q(q, l5_3, "Cho hình bình hành $ABCD$ có $\\widehat{A}=70^\\circ$. Số đo $\\widehat{C}$ là:", new[] { "$70^\\circ$", "$110^\\circ$", "$90^\\circ$", "$140^\\circ$" }, 0, "Hình bình hành có các góc đối bằng nhau nên $\\widehat{C}=\\widehat{A}=70^\\circ$.");

        Q(q, l5_4, "Hình chữ nhật có:", new[] { "Bốn góc vuông", "Bốn cạnh bằng nhau", "Hai đường chéo vuông góc", "Các cạnh đối song song" }, 0, "Hình chữ nhật là tứ giác có bốn góc vuông.");
        Q(q, l5_4, "Trong hình chữ nhật, hai đường chéo:", new[] { "Bằng nhau", "Vuông góc", "Là phân giác các góc", "Song song" }, 0, "Hình chữ nhật có hai đường chéo bằng nhau.");
        Q(q, l5_4, "Dấu hiệu nhận biết hình chữ nhật:", new[] { "Hình bình hành có một góc vuông", "Tứ giác có hai góc vuông", "Hình thang có một góc vuông", "Tứ giác có hai đường chéo bằng nhau" }, 0, "Hình bình hành có một góc vuông là hình chữ nhật.");
        Q(q, l5_4, "Đường chéo hình chữ nhật $ABCD$ ($AB=6$, $BC=8$) dài:", new[] { "$10$", "$14$", "$2$", "$48$" }, 0, "Áp dụng Pythagore: $AC^2 = 6^2+8^2=100 \\Rightarrow AC=10$.");

        Q(q, l5_5, "Hình thoi có:", new[] { "Bốn cạnh bằng nhau", "Bốn góc bằng nhau", "Hai đường chéo bằng nhau", "Các góc vuông" }, 0, "Hình thoi là tứ giác có bốn cạnh bằng nhau.");
        Q(q, l5_5, "Hình vuông có:", new[] { "Bốn góc vuông và bốn cạnh bằng nhau", "Bốn góc vuông", "Bốn cạnh bằng nhau", "Hai đường chéo vuông góc" }, 0, "Hình vuông vừa là hình chữ nhật, vừa là hình thoi.");
        Q(q, l5_5, "Trong hình thoi, hai đường chéo:", new[] { "Vuông góc với nhau", "Bằng nhau", "Song song", "Là phân giác các cạnh" }, 0, "Hình thoi có hai đường chéo vuông góc với nhau.");
        Q(q, l5_5, "Một tứ giác vừa là hình chữ nhật, vừa là hình thoi thì đó là:", new[] { "Hình vuông", "Hình bình hành", "Hình thang cân", "Tứ giác thường" }, 0, "Hình vuông vừa có bốn góc vuông (hình chữ nhật) vừa có bốn cạnh bằng nhau (hình thoi).");

        // ──────────────────────────────────────────────
        // CHAPTER 6: ĐỊNH LÍ THALÈS
        // ──────────────────────────────────────────────
        var c6 = Guid.NewGuid();
        context.Chapters.Add(new Chapter { Id = c6, Title = "Chương 6: Định lí Thalès trong tam giác", Description = "Định lí Thalès, đường trung bình, tính chất đường phân giác", OrderIndex = 6, CurriculumTopicId = topicThales, IsPublished = true, PublishedAt = DateTime.UtcNow });

        var l6_1 = Lesson(c6, topicThales, "Định lí Thalès trong tam giác", 1, null,
            "# Định lí Thalès trong tam giác\n\nNếu một đường thẳng song song với một cạnh của tam giác và cắt hai cạnh còn lại thì nó định ra trên hai cạnh đó những đoạn thẳng tương ứng tỉ lệ.\n\n**Định lí thuận:** $\\triangle ABC$, $MN \\parallel BC$ ($M \\in AB$, $N \\in AC$) $\\Rightarrow \\frac{AM}{AB} = \\frac{AN}{AC} = \\frac{MN}{BC}$\n\nHệ quả: $\\frac{AM}{MB} = \\frac{AN}{NC}$");
        var l6_2 = Lesson(c6, topicThales, "Đường trung bình của tam giác", 2, null,
            "# Đường trung bình của tam giác\n\nĐường trung bình của tam giác là đoạn thẳng nối trung điểm hai cạnh của tam giác.\n\n## Định lí\n- Đường thẳng đi qua trung điểm một cạnh và song song với cạnh thứ hai thì đi qua trung điểm cạnh thứ ba.\n- Đường trung bình của tam giác thì song song với cạnh thứ ba và bằng nửa cạnh ấy.\n\n**Ví dụ:** $\\triangle ABC$, $M$ là trung điểm $AB$, $N$ là trung điểm $AC$ $\\Rightarrow MN \\parallel BC$ và $MN = \\frac{BC}{2}$.");
        var l6_3 = Lesson(c6, topicThales, "Tính chất đường phân giác của tam giác", 3, null,
            "# Tính chất đường phân giác của tam giác\n\nTrong tam giác, đường phân giác của một góc chia cạnh đối diện thành hai đoạn tỉ lệ với hai cạnh kề hai đoạn ấy.\n\n$\\triangle ABC$, $AD$ là phân giác $\\widehat{A}$ ($D \\in BC$) $\\Rightarrow \\frac{BD}{DC} = \\frac{AB}{AC}$\n\n**Ví dụ:** $\\triangle ABC$ có $AB=6$, $AC=8$, $BC=7$. Phân giác $AD$. Tính $BD$, $DC$.\n\n$\\frac{BD}{DC} = \\frac{AB}{AC} = \\frac{6}{8} = \\frac{3}{4}$.\n\n$BD = \\frac{3}{7}\\cdot BC = \\frac{3}{7}\\cdot7 = 3$, $DC = 7-3 = 4$.");

        context.Lessons.AddRange(l6_1, l6_2, l6_3);

        Q(q, l6_1, "Cho $\\triangle ABC$, $MN \\parallel BC$ ($M \\in AB$, $N \\in AC$). Biết $AM=2$, $MB=3$, $AN=3$. Tính $NC$:", new[] { "$4,5$", "$2$", "$3$", "$5$" }, 0, "$\\frac{AM}{MB} = \\frac{AN}{NC} \\Rightarrow \\frac{2}{3} = \\frac{3}{NC} \\Rightarrow NC = \\frac{9}{2}=4,5$.");
        Q(q, l6_1, "Nếu $MN \\parallel BC$ trong $\\triangle ABC$ thì:", new[] { "$\\frac{AM}{AB} = \\frac{AN}{AC}$", "$\\frac{AM}{MB} = \\frac{AN}{NC}$", "$\\frac{AM}{AB} = \\frac{MN}{BC}$", "Tất cả đều đúng" }, 3, "Cả ba tỉ lệ đều đúng theo định lí Thalès và hệ quả.");
        Q(q, l6_1, "Cho $\\triangle ABC$, $DE \\parallel BC$ ($D \\in AB$, $E \\in AC$). $AD=4$, $DB=2$, $AE=6$. Tính $EC$:", new[] { "$3$", "$2$", "$4$", "$5$" }, 0, "$\\frac{AD}{DB} = \\frac{AE}{EC} \\Rightarrow \\frac{4}{2} = \\frac{6}{EC} \\Rightarrow EC = 3$.");
        Q(q, l6_1, "Định lí Thalès áp dụng cho:", new[] { "Tam giác", "Tứ giác", "Hình tròn", "Đa giác" }, 0, "Định lí Thalès áp dụng trong tam giác.");

        Q(q, l6_2, "Đường trung bình của tam giác là:", new[] { "Đoạn nối trung điểm hai cạnh", "Đoạn nối đỉnh và trung điểm cạnh đối", "Đường thẳng song song với đáy", "Đường vuông góc với cạnh" }, 0, "Đường trung bình là đoạn nối trung điểm hai cạnh của tam giác.");
        Q(q, l6_2, "Cho $\\triangle ABC$, $M$, $N$ là trung điểm $AB$, $AC$. $BC=12$. Tính $MN$:", new[] { "$6$", "$12$", "$24$", "$4$" }, 0, "$MN = \\frac{BC}{2} = \\frac{12}{2} = 6$.");
        Q(q, l6_2, "Trong $\\triangle ABC$, đường thẳng qua trung điểm $AB$ và song song $BC$:", new[] { "Đi qua trung điểm $AC$", "Đi qua đỉnh $A$", "Vuông góc $AC$", "Bằng nửa $BC$" }, 0, "Đường thẳng qua trung điểm một cạnh và song song cạnh thứ hai thì đi qua trung điểm cạnh thứ ba.");
        Q(q, l6_2, "Tam giác $ABC$ có $M$, $N$ là trung điểm $AB$, $AC$. Biết $MN=8$, $BC$ bằng:", new[] { "$16$", "$4$", "$8$", "$12$" }, 0, "$MN = \\frac{BC}{2} \\Rightarrow BC = 2MN = 16$.");

        Q(q, l6_3, "Phân giác $AD$ của $\\triangle ABC$ ($D \\in BC$) thì:", new[] { "$\\frac{BD}{DC} = \\frac{AB}{AC}$", "$\\frac{BD}{DC} = \\frac{AD}{AC}$", "$\\frac{BD}{DC} = \\frac{AB}{AD}$", "$\\frac{BD}{AB} = \\frac{DC}{AC}$" }, 0, "Tính chất đường phân giác: $\\frac{BD}{DC} = \\frac{AB}{AC}$.");
        Q(q, l6_3, "Cho $\\triangle ABC$, $AB=9$, $AC=12$, $BC=14$. Phân giác $AD$. Tính $BD$:", new[] { "$6$", "$8$", "$7$", "$9$" }, 0, "$\\frac{BD}{DC} = \\frac{9}{12} = \\frac{3}{4}$. $BD = \\frac{3}{7}\\cdot14 = 6$.");
        Q(q, l6_3, "Tính chất đường phân giác đúng với:", new[] { "Phân giác trong", "Phân giác ngoài", "Cả trong và ngoài", "Chỉ phân giác trong" }, 2, "Tính chất đúng cho cả đường phân giác trong và phân giác ngoài.");
        Q(q, l6_3, "Cho $\\triangle ABC$, $AD$ là phân giác, $AB=6$, $AC=9$, $BD=4$. Tính $DC$:", new[] { "$6$", "$4$", "$9$", "$3$" }, 0, "$\\frac{BD}{DC} = \\frac{AB}{AC} \\Rightarrow \\frac{4}{DC} = \\frac{6}{9} \\Rightarrow DC = 6$.");

        // ──────────────────────────────────────────────
        // CHAPTER 7: TAM GIÁC ĐỒNG DẠNG
        // ──────────────────────────────────────────────
        var c7 = Guid.NewGuid();
        context.Chapters.Add(new Chapter { Id = c7, Title = "Chương 7: Tam giác đồng dạng", Description = "Tam giác đồng dạng, các trường hợp đồng dạng, định lí Pythagore", OrderIndex = 7, CurriculumTopicId = topicSimilar, IsPublished = true, PublishedAt = DateTime.UtcNow });

        var l7_1 = Lesson(c7, topicSimilar, "Hai tam giác đồng dạng", 1, null,
            "# Hai tam giác đồng dạng\n\nTam giác $A'B'C'$ gọi là đồng dạng với tam giác $ABC$ nếu:\n\n$\\widehat{A'} = \\widehat{A}$, $\\widehat{B'} = \\widehat{B}$, $\\widehat{C'} = \\widehat{C}$\n\nvà $\\frac{A'B'}{AB} = \\frac{B'C'}{BC} = \\frac{C'A'}{CA} = k$\n\n$k$ gọi là tỉ số đồng dạng.\n\nKý hiệu: $\\triangle A'B'C' \\backsim \\triangle ABC$.");
        var l7_2 = Lesson(c7, topicSimilar, "Ba trường hợp đồng dạng của hai tam giác", 2, null,
            "# Ba trường hợp đồng dạng của hai tam giác\n\n## 1. Cạnh - Cạnh - Cạnh (c-c-c)\n$\\frac{A'B'}{AB} = \\frac{B'C'}{BC} = \\frac{C'A'}{CA}$ $\\Rightarrow$ $\\triangle A'B'C' \\backsim \\triangle ABC$\n\n## 2. Cạnh - Góc - Cạnh (c-g-c)\n$\\frac{A'B'}{AB} = \\frac{A'C'}{AC}$ và $\\widehat{A'} = \\widehat{A}$ $\\Rightarrow$ $\\triangle A'B'C' \\backsim \\triangle ABC$\n\n## 3. Góc - Góc (g-g)\n$\\widehat{A'} = \\widehat{A}$ và $\\widehat{B'} = \\widehat{B}$ $\\Rightarrow$ $\\triangle A'B'C' \\backsim \\triangle ABC$");
        var l7_3 = Lesson(c7, topicSimilar, "Định lí Pythagore và ứng dụng", 3, null,
            "# Định lí Pythagore\n\nTrong tam giác vuông, bình phương cạnh huyền bằng tổng bình phương hai cạnh góc vuông.\n\n$$BC^2 = AB^2 + AC^2$$\n\n**Ví dụ:** Tam giác vuông có hai cạnh góc vuông 3 và 4 thì cạnh huyền $= \\sqrt{3^2+4^2} = 5$.\n\n## Định lí đảo\nNếu một tam giác có bình phương một cạnh bằng tổng bình phương hai cạnh kia thì tam giác đó là tam giác vuông.");
        var l7_4 = Lesson(c7, topicSimilar, "Các trường hợp đồng dạng của tam giác vuông", 4, null,
            "# Các trường hợp đồng dạng của tam giác vuông\n\n## 1. Góc nhọn\nNếu một góc nhọn của tam giác vuông này bằng một góc nhọn của tam giác vuông kia thì hai tam giác vuông đồng dạng.\n\n## 2. Hai cạnh góc vuông tỉ lệ\nNếu hai cạnh góc vuông của tam giác vuông này tỉ lệ với hai cạnh góc vuông của tam giác vuông kia thì chúng đồng dạng.\n\n## 3. Cạnh huyền - cạnh góc vuông\nNếu cạnh huyền và một cạnh góc vuông của tam giác vuông này tỉ lệ với cạnh huyền và một cạnh góc vuông của tam giác vuông kia thì chúng đồng dạng.");
        var l7_5 = Lesson(c7, topicSimilar, "Hình đồng dạng", 5, null,
            "# Hình đồng dạng\n\nHai hình $H$ và $H'$ được gọi là đồng dạng nếu có một hình $H''$ bằng $H$ (có thể co $H''$) và đồng dạng với $H'$.\n\nNói cách khác, hai hình đồng dạng khi chúng có cùng hình dạng nhưng kích thước có thể khác nhau.\n\n**Ví dụ:** Mọi hình tròn đều đồng dạng với nhau. Mọi hình vuông đều đồng dạng với nhau.\n\n## Ứng dụng\n- Thu nhỏ/phóng to hình vẽ.\n- Bản đồ, bản vẽ kỹ thuật sử dụng tỉ lệ đồng dạng.");

        context.Lessons.AddRange(l7_1, l7_2, l7_3, l7_4, l7_5);

        Q(q, l7_1, "Hai tam giác đồng dạng khi:", new[] { "Có các góc tương ứng bằng nhau và các cạnh tương ứng tỉ lệ", "Có các cạnh bằng nhau", "Có diện tích bằng nhau", "Có chu vi bằng nhau" }, 0, "Định nghĩa: các góc bằng nhau, các cạnh tương ứng tỉ lệ.");
        Q(q, l7_1, "Nếu $\\triangle ABC \\backsim \\triangle DEF$ với tỉ số $k=2$ thì:", new[] { "$\\frac{AB}{DE}=2$", "$\\frac{AB}{DE}=\\frac{1}{2}$", "$AB=DE$", "$\\frac{BC}{EF}=\\frac{1}{2}$" }, 0, "Tỉ số đồng dạng $k = \\frac{AB}{DE} = 2$.");
        Q(q, l7_1, "Ký hiệu hai tam giác đồng dạng là:", new[] { "$\\backsim$", "$=$", "$\\cong$", "$\\perp$" }, 0, "Ký hiệu đồng dạng là $\\backsim$.");
        Q(q, l7_1, "Hai tam giác bằng nhau có tỉ số đồng dạng là:", new[] { "$1$", "$2$", "$0$", "$\\frac{1}{2}$" }, 0, "Hai tam giác bằng nhau thì các cạnh tương ứng bằng nhau, tỉ số $k=1$.");

        Q(q, l7_2, "Trường hợp đồng dạng c-c-c là:", new[] { "Ba cạnh tỉ lệ", "Hai cạnh tỉ lệ và góc xen giữa bằng nhau", "Hai góc bằng nhau", "Cạnh huyền và góc nhọn" }, 0, "c-c-c: ba cạnh của tam giác này tỉ lệ với ba cạnh của tam giác kia.");
        Q(q, l7_2, "Để $\\triangle ABC \\backsim \\triangle DEF$ theo trường hợp c-g-c cần:", new[] { "$\\frac{AB}{DE} = \\frac{AC}{DF}$ và $\\widehat{A} = \\widehat{D}$", "$\\frac{AB}{DE} = \\frac{BC}{EF}$", "$\\widehat{B} = \\widehat{E}$", "$\\frac{AB}{DE} = \\frac{BC}{EF} = \\frac{AC}{DF}$" }, 0, "c-g-c: hai cạnh tỉ lệ và góc xen giữa bằng nhau.");
        Q(q, l7_2, "Hai tam giác có $\\widehat{A} = \\widehat{D} = 60^\\circ$, $\\widehat{B} = \\widehat{E} = 50^\\circ$ thì:", new[] { "$\\triangle ABC \\backsim \\triangle DEF$ (g-g)", "Không đủ dữ kiện", "Không đồng dạng", "Bằng nhau" }, 0, "Hai góc bằng nhau suy ra đồng dạng theo trường hợp g-g.");
        Q(q, l7_2, "Điều kiện nào sau đây đủ để hai tam giác đồng dạng?", new[] { "$\\widehat{A} = \\widehat{D}$ và $\\widehat{B} = \\widehat{E}$", "$\\frac{AB}{DE} = \\frac{BC}{EF}$", "$\\widehat{A} = \\widehat{D}$", "$\\frac{AB}{DE} = 2$" }, 0, "Hai góc bằng nhau (g-g) là đủ để hai tam giác đồng dạng.");

        Q(q, l7_3, "Cạnh huyền của tam giác vuông có hai cạnh góc vuông 6, 8 là:", new[] { "$10$", "$14$", "$2$", "$100$" }, 0, "$\\sqrt{6^2+8^2} = \\sqrt{36+64} = \\sqrt{100} = 10$.");
        Q(q, l7_3, "Bộ ba nào là độ dài tam giác vuông?", new[] { "$3,4,5$", "$2,3,4$", "$1,2,3$", "$5,6,7$" }, 0, "$3^2+4^2=9+16=25=5^2$ thỏa mãn Pythagore.");
        Q(q, l7_3, "Định lí Pythagore áp dụng cho:", new[] { "Tam giác vuông", "Tam giác đều", "Tam giác cân", "Mọi tam giác" }, 0, "Định lí Pythagore chỉ áp dụng cho tam giác vuông.");
        Q(q, l7_3, "Một tam giác có các cạnh $9, 12, 15$ là tam giác:", new[] { "Vuông", "Đều", "Cân", "Thường" }, 0, "$9^2+12^2 = 81+144=225=15^2$, nên là tam giác vuông.");

        Q(q, l7_4, "Hai tam giác vuông đồng dạng nếu:", new[] { "Có một góc nhọn bằng nhau", "Có hai cạnh bằng nhau", "Có cạnh huyền bằng nhau", "Có diện tích bằng nhau" }, 0, "Nếu một góc nhọn của tam giác vuông này bằng góc nhọn của tam giác vuông kia thì chúng đồng dạng.");
        Q(q, l7_4, "Tam giác vuông $ABC$ ($\\widehat{A}=90^\\circ$) có $AB=3$, $AC=4$. Tam giác vuông $DEF$ ($\\widehat{D}=90^\\circ$) có $DE=6$, $DF=8$. Kết luận:", new[] { "$\\triangle ABC \\backsim \\triangle DEF$", "$\\triangle ABC = \\triangle DEF$", "Không đồng dạng", "Có chu vi bằng nhau" }, 0, "$\\frac{AB}{DE} = \\frac{3}{6} = \\frac{1}{2}$, $\\frac{AC}{DF} = \\frac{4}{8} = \\frac{1}{2}$ nên đồng dạng (cgv-cgv).");
        Q(q, l7_4, "Tỉ số đồng dạng của hai tam giác vuông có cạnh góc vuông 3-4 và 6-8 là:", new[] { "$\\frac{1}{2}$", "$2$", "$3$", "$\\frac{2}{3}$" }, 0, "Tỉ số $\\frac{3}{6} = \\frac{1}{2}$.");
        Q(q, l7_4, "Điều kiện cạnh huyền - cạnh góc vuông dùng cho:", new[] { "Tam giác vuông", "Tam giác thường", "Tam giác cân", "Tam giác đều" }, 0, "Trường hợp đồng dạng cạnh huyền - cạnh góc vuông áp dụng cho tam giác vuông.");

        Q(q, l7_5, "Hai hình tròn bất kỳ luôn:", new[] { "Đồng dạng", "Bằng nhau", "Có chu vi bằng nhau", "Không đồng dạng" }, 0, "Mọi hình tròn đều đồng dạng với nhau.");
        Q(q, l7_5, "Hai hình vuông bất kỳ luôn:", new[] { "Đồng dạng", "Bằng nhau", "Có cạnh bằng nhau", "Có diện tích bằng nhau" }, 0, "Mọi hình vuông đều đồng dạng.");
        Q(q, l7_5, "Bản đồ tỉ lệ $1:10000$ là ứng dụng của:", new[] { "Hình đồng dạng", "Hình bằng nhau", "Diện tích", "Chu vi" }, 0, "Bản đồ là ứng dụng của phóng to/thu nhỏ theo tỉ lệ đồng dạng.");
        Q(q, l7_5, "Hai hình chữ nhật có kích thước $2\\times3$ và $4\\times6$:", new[] { "Đồng dạng (tỉ số 2)", "Bằng nhau", "Không đồng dạng", "Có chu vi bằng nhau" }, 0, "Tỉ lệ $\\frac{2}{4}=\\frac{3}{6}=\\frac{1}{2}$, nên đồng dạng.");

        // ──────────────────────────────────────────────
        // CHAPTER 8: HÌNH CHÓP ĐỀU
        // ──────────────────────────────────────────────
        var c8 = Guid.NewGuid();
        context.Chapters.Add(new Chapter { Id = c8, Title = "Chương 8: Hình chóp tam giác đều và hình chóp tứ giác đều", Description = "Hình chóp tam giác đều, hình chóp tứ giác đều, diện tích và thể tích", OrderIndex = 8, CurriculumTopicId = topicSolid, IsPublished = true, PublishedAt = DateTime.UtcNow });

        var l8_1 = Lesson(c8, topicSolid, "Hình chóp tam giác đều", 1, null,
            "# Hình chóp tam giác đều\n\nHình chóp tam giác đều có đáy là tam giác đều, các mặt bên là tam giác cân bằng nhau có chung đỉnh.\n\n## Các yếu tố\n- Đỉnh $S$, đáy $ABC$ là tam giác đều.\n- Các cạnh bên $SA, SB, SC$ bằng nhau.\n- Đường cao $SH$ ($H$ là trọng tâm đáy).\n- Trung đoạn $SI$ ($I$ là trung điểm cạnh đáy).\n\n## Diện tích xung quanh\n$$S_{xq} = p \\cdot d$$\n($p$ là nửa chu vi đáy, $d$ là trung đoạn)\n\n## Thể tích\n$$V = \\frac{1}{3} S_{đáy} \\cdot h$$");
        var l8_2 = Lesson(c8, topicSolid, "Hình chóp tứ giác đều", 2, null,
            "# Hình chóp tứ giác đều\n\nHình chóp tứ giác đều có đáy là hình vuông, các mặt bên là tam giác cân bằng nhau có chung đỉnh.\n\n## Ví dụ: Hình chóp tứ giác đều $S.ABCD$\n- Đáy $ABCD$ là hình vuông.\n- Các cạnh bên $SA=SB=SC=SD$.\n- Đường cao $SO$ ($O$ là tâm đáy).\n\n## Diện tích xung quanh\n$$S_{xq} = p \\cdot d$$\n\n## Thể tích\n$$V = \\frac{1}{3} S_{đáy} \\cdot h$$\n\n**Ví dụ:** Hình chóp tứ giác đều cạnh đáy 6, đường cao 4. Thể tích $V = \\frac{1}{3}\\cdot6^2\\cdot4 = 48$.");

        context.Lessons.AddRange(l8_1, l8_2);

        Q(q, l8_1, "Đáy của hình chóp tam giác đều là:", new[] { "Tam giác đều", "Tam giác vuông", "Tam giác cân", "Tứ giác" }, 0, "Hình chóp tam giác đều có đáy là tam giác đều.");
        Q(q, l8_1, "Các mặt bên của hình chóp tam giác đều là:", new[] { "Tam giác cân", "Tam giác đều", "Tam giác vuông", "Hình thang" }, 0, "Các mặt bên là tam giác cân bằng nhau.");
        Q(q, l8_1, "Công thức thể tích hình chóp đều:", new[] { "$V = \\frac{1}{3}S_{đáy}\\cdot h$", "$V = S_{đáy}\\cdot h$", "$V = \\frac{1}{2}S_{đáy}\\cdot h$", "$V = \\frac{1}{3}S_{đáy}\\cdot h^2$" }, 0, "Thể tích hình chóp $= \\frac{1}{3}$ diện tích đáy $\\times$ chiều cao.");
        Q(q, l8_1, "Trong hình chóp tam giác đều, chân đường cao là:", new[] { "Trọng tâm đáy", "Trung điểm cạnh", "Đỉnh đáy", "Tâm đường tròn ngoại tiếp" }, 0, "Chân đường cao của hình chóp tam giác đều là trọng tâm của tam giác đáy.");

        Q(q, l8_2, "Đáy của hình chóp tứ giác đều là:", new[] { "Hình vuông", "Hình thoi", "Hình chữ nhật", "Tứ giác thường" }, 0, "Hình chóp tứ giác đều có đáy là hình vuông.");
        Q(q, l8_2, "Thể tích hình chóp tứ giác đều cạnh đáy 4, cao 6 là:", new[] { "$32$", "$96$", "$64$", "$48$" }, 0, "$V = \\frac{1}{3}\\cdot4^2\\cdot6 = \\frac{1}{3}\\cdot16\\cdot6 = 32$.");
        Q(q, l8_2, "Diện tích xung quanh hình chóp đều tính bằng:", new[] { "$p\\cdot d$", "$2p\\cdot d$", "$\\frac{1}{2}p\\cdot d$", "$p\\cdot h$" }, 0, "$S_{xq} = p\\cdot d$ với $p$ là nửa chu vi đáy, $d$ là trung đoạn.");
        Q(q, l8_2, "Hình chóp tứ giác đều có số mặt là:", new[] { "$5$", "$4$", "$6$", "$3$" }, 0, "1 mặt đáy (hình vuông) + 4 mặt bên (tam giác) = 5 mặt.");

        // ──────────────────────────────────────────────
        // CHAPTER 9: DỮ LIỆU VÀ BIỂU ĐỒ
        // ──────────────────────────────────────────────
        var c9 = Guid.NewGuid();
        context.Chapters.Add(new Chapter { Id = c9, Title = "Chương 9: Dữ liệu và biểu đồ", Description = "Thu thập, phân loại, biểu diễn và phân tích dữ liệu thống kê", OrderIndex = 9, CurriculumTopicId = topicStat, IsPublished = true, PublishedAt = DateTime.UtcNow });

        var l9_1 = Lesson(c9, topicStat, "Thu thập và phân loại dữ liệu", 1, null,
            "# Thu thập và phân loại dữ liệu\n\n## Dữ liệu\nDữ liệu là thông tin thu thập được từ các cuộc điều tra, khảo sát.\n\n## Phân loại dữ liệu\n- Dữ liệu định tính: màu sắc, loại, đánh giá (tốt, khá,...).\n- Dữ liệu định lượng: số đo, số lượng, cân nặng.\n\n## Thu thập dữ liệu\n- Quan sát, phỏng vấn, bảng hỏi.\n- Từ nguồn có sẵn: Internet, báo cáo.");
        var l9_2 = Lesson(c9, topicStat, "Biểu diễn dữ liệu bằng bảng và biểu đồ", 2, null,
            "# Biểu diễn dữ liệu\n\n## Bảng dữ liệu\nSắp xếp dữ liệu theo hàng và cột.\n\n## Biểu đồ tranh\nDùng hình ảnh để biểu diễn số liệu.\n\n## Biểu đồ cột\nCột đứng thể hiện giá trị.\n\n## Biểu đồ đoạn thẳng\nDùng đoạn thẳng nối các điểm.\n\n## Biểu đồ hình tròn\nThể hiện tỉ lệ phần trăm.");
        var l9_3 = Lesson(c9, topicStat, "Phân tích số liệu thống kê dựa vào biểu đồ", 3, null,
            "# Phân tích số liệu thống kê\n\nDựa vào biểu đồ, ta có thể:\n- Nhận xét xu hướng tăng/giảm.\n- So sánh các giá trị.\n- Tính tỉ lệ phần trăm.\n- Dự đoán xu hướng.\n\n**Ví dụ:** Biểu đồ cột cho thấy doanh thu quý 1 là 100 triệu, quý 2 là 120 triệu, quý 3 là 90 triệu. Nhận xét: Doanh thu tăng ở quý 2 và giảm ở quý 3.");

        context.Lessons.AddRange(l9_1, l9_2, l9_3);

        Q(q, l9_1, "Dữ liệu 'màu sắc yêu thích' là loại:", new[] { "Định tính", "Định lượng", "Số", "Rời rạc" }, 0, "Dữ liệu về màu sắc là dữ liệu định tính.");
        Q(q, l9_1, "Dữ liệu 'chiều cao học sinh' là loại:", new[] { "Định lượng", "Định tính", "Phân loại", "Không xác định" }, 0, "Chiều cao là số đo, thuộc dữ liệu định lượng.");
        Q(q, l9_1, "Phương pháp thu thập dữ liệu nào là trực tiếp?", new[] { "Phỏng vấn", "Tra cứu Internet", "Đọc báo cáo", "Xem tài liệu" }, 0, "Phỏng vấn là phương pháp thu thập dữ liệu trực tiếp.");
        Q(q, l9_1, "Kết quả xếp loại học lực (Giỏi, Khá, TB) thuộc loại:", new[] { "Định tính", "Định lượng", "Số nguyên", "Số thực" }, 0, "Xếp loại học lực là dữ liệu định tính.");

        Q(q, l9_2, "Biểu đồ hình tròn thường dùng để:", new[] { "Thể hiện tỉ lệ phần trăm", "So sánh số liệu qua thời gian", "Biểu diễn xu hướng", "Thể hiện tần số" }, 0, "Biểu đồ hình tròn thể hiện tỉ lệ phần trăm của các thành phần.");
        Q(q, l9_2, "Loại biểu đồ dùng để biểu diễn sự thay đổi theo thời gian:", new[] { "Biểu đồ đoạn thẳng", "Biểu đồ hình tròn", "Biểu đồ cột", "Biểu đồ tranh" }, 0, "Biểu đồ đoạn thẳng thể hiện sự thay đổi theo thời gian.");
        Q(q, l9_2, "Biểu đồ cột dùng để:", new[] { "So sánh các giá trị", "Thể hiện tỉ lệ", "Biểu diễn xu hướng", "Thể hiện tần số" }, 0, "Biểu đồ cột dùng để so sánh các giá trị.");
        Q(q, l9_2, "Bảng dữ liệu có:", new[] { "Hàng và cột", "Chỉ hàng", "Chỉ cột", "Không cấu trúc" }, 0, "Bảng dữ liệu gồm hàng và cột để sắp xếp dữ liệu.");

        Q(q, l9_3, "Biểu đồ cho thấy doanh thu tăng dần qua các tháng. Điều này thể hiện:", new[] { "Xu hướng tăng", "Xu hướng giảm", "Không thay đổi", "Biến động ngẫu nhiên" }, 0, "Doanh thu tăng dần thể hiện xu hướng tăng.");
        Q(q, l9_3, "Nếu biểu đồ có cột cao nhất ở tháng 6 thì:", new[] { "Tháng 6 có giá trị lớn nhất", "Tháng 6 có giá trị nhỏ nhất", "Không kết luận được", "Tháng 6 bằng các tháng khác" }, 0, "Cột càng cao biểu diễn giá trị càng lớn.");
        Q(q, l9_3, "Phân tích biểu đồ giúp:", new[] { "Nhận xét và dự đoán xu hướng", "Tính toán số liệu gốc", "Vẽ lại biểu đồ", "Thay đổi số liệu" }, 0, "Phân tích biểu đồ giúp nhận xét, so sánh, dự đoán xu hướng.");
        Q(q, l9_3, "Tỉ lệ phần trăm của một thành phần trong biểu đồ tròn là $25\\%$. Góc tương ứng là:", new[] { "$90^\\circ$", "$360^\\circ$", "$25^\\circ$", "$180^\\circ$" }, 0, "$25\\%$ của $360^\\circ = 0,25\\cdot360 = 90^\\circ$.");

        // ──────────────────────────────────────────────
        // CHAPTER 10: XÁC SUẤT CỦA BIẾN CỐ
        // ──────────────────────────────────────────────
        var c10 = Guid.NewGuid();
        context.Chapters.Add(new Chapter { Id = c10, Title = "Chương 10: Mở đầu về xác suất của biến cố", Description = "Kết quả có thể, kết quả thuận lợi và cách tính xác suất", OrderIndex = 10, CurriculumTopicId = topicStat, IsPublished = true, PublishedAt = DateTime.UtcNow });

        var l10_1 = Lesson(c10, topicStat, "Kết quả có thể và kết quả thuận lợi", 1, null,
            "# Kết quả có thể và kết quả thuận lợi\n\n## Kết quả có thể\nLà tất cả các kết quả có thể xảy ra của một phép thử.\n\n**Ví dụ:** Gieo một con xúc xắc, các kết quả có thể: $1,2,3,4,5,6$.\n\n## Kết quả thuận lợi\nLà các kết quả làm cho biến cố xảy ra.\n\n**Ví dụ:** Biến cố \"gieo được mặt chẵn\" có kết quả thuận lợi là $2,4,6$.");
        var l10_2 = Lesson(c10, topicStat, "Cách tính xác suất của biến cố bằng tỉ số", 2, null,
            "# Cách tính xác suất\n\nXác suất của biến cố $A$:\n\n$$P(A) = \\frac{\\text{số kết quả thuận lợi cho } A}{\\text{số kết quả có thể}}$$\n\n**Ví dụ:** Gieo xúc xắc. Tính xác suất xuất hiện mặt 3 chấm.\n\nSố kết quả có thể: $6$ (1,2,3,4,5,6).\nSố kết quả thuận lợi: $1$ (mặt 3).\n\n$P = \\frac{1}{6}$.\n\nChú ý: $0 \\leq P(A) \\leq 1$.");
        var l10_3 = Lesson(c10, topicStat, "Xác suất thực nghiệm và ứng dụng", 3, null,
            "# Xác suất thực nghiệm\n\nKhi thực hiện một phép thử nhiều lần, xác suất thực nghiệm tiến gần đến xác suất lý thuyết.\n\n$$P_{th\\text{ực nghiệm}}(A) = \\frac{\\text{số lần } A \\text{ xảy ra}}{\\text{tổng số lần thử}}$$\n\n## Ứng dụng\n- Dự đoán kết quả.\n- Phân tích rủi ro.\n- Thống kê y tế, kinh tế.\n\n**Ví dụ:** Tung đồng xu 100 lần được 48 mặt ngửa. Xác suất thực nghiệm xuất hiện mặt ngửa là $\\frac{48}{100} = 0,48$.");

        context.Lessons.AddRange(l10_1, l10_2, l10_3);

        Q(q, l10_1, "Gieo xúc xắc, số kết quả có thể là:", new[] { "$6$", "$2$", "$1$", "$12$" }, 0, "Xúc xắc 6 mặt nên có 6 kết quả có thể.");
        Q(q, l10_1, "Kết quả thuận lợi cho biến cố 'gieo được số lẻ' khi gieo xúc xắc là:", new[] { "$1,3,5$", "$2,4,6$", "$1,2,3$", "$4,5,6$" }, 0, "Các số lẻ trên xúc xắc là 1, 3, 5.");
        Q(q, l10_1, "Rút một lá từ bộ bài 52 lá. Số kết quả có thể là:", new[] { "$52$", "$13$", "$4$", "$26$" }, 0, "Bộ bài 52 lá nên có 52 kết quả có thể.");
        Q(q, l10_1, "Biến cố là:", new[] { "Một tập con của không gian mẫu", "Một kết quả", "Một phép thử", "Một con số" }, 0, "Biến cố là tập con của không gian mẫu (tập các kết quả có thể).");

        Q(q, l10_2, "Xác suất gieo xúc xắc được mặt 4 chấm là:", new[] { "$\\frac{1}{6}$", "$\\frac{1}{2}$", "$\\frac{1}{4}$", "$\\frac{1}{3}$" }, 0, "1 kết quả thuận lợi trong 6 kết quả có thể: $P = \\frac{1}{6}$.");
        Q(q, l10_2, "Rút ngẫu nhiên một lá từ bộ 52 lá. Xác suất rút được Át là:", new[] { "$\\frac{1}{13}$", "$\\frac{1}{52}$", "$\\frac{1}{4}$", "$\\frac{4}{52}$" }, 0, "Có 4 lá Át trong 52 lá: $P = \\frac{4}{52} = \\frac{1}{13}$.");
        Q(q, l10_2, "Xác suất của biến cố chắc chắn là:", new[] { "$1$", "$0$", "$0,5$", "$\\infty$" }, 0, "Biến cố chắc chắn luôn xảy ra, $P=1$.");
        Q(q, l10_2, "Xác suất của biến cố không thể là:", new[] { "$0$", "$1$", "$0,5$", "$-1$" }, 0, "Biến cố không thể không bao giờ xảy ra, $P=0$.");

        Q(q, l10_3, "Tung đồng xu 50 lần được 28 lần ngửa. Xác suất thực nghiệm của mặt ngửa là:", new[] { "$\\frac{28}{50}$", "$\\frac{1}{2}$", "$\\frac{22}{50}$", "$\\frac{50}{28}$" }, 0, "Xác suất thực nghiệm $= \\frac{28}{50}$.");
        Q(q, l10_3, "Khi số lần thử tăng lên, xác suất thực nghiệm:", new[] { "Tiến gần xác suất lý thuyết", "Luôn tăng", "Luôn giảm", "Không thay đổi" }, 0, "Luật số lớn: xác suất thực nghiệm tiến gần xác suất lý thuyết.");
        Q(q, l10_3, "Gieo xúc xắc 120 lần được 18 lần mặt 5. Xác suất thực nghiệm mặt 5:", new[] { "$\\frac{18}{120}$", "$\\frac{1}{6}$", "$\\frac{20}{120}$", "$\\frac{5}{120}$" }, 0, "$18$ lần mặt 5 trong 120 lần gieo: $\\frac{18}{120}$.");
        Q(q, l10_3, "Xác suất thực nghiệm giúp:", new[] { "Dự đoán kết quả trong thực tế", "Tính toán lý thuyết", "Chứng minh định lý", "Giải phương trình" }, 0, "Xác suất thực nghiệm giúp dự đoán kết quả dựa trên dữ liệu quan sát.");

        context.Questions.AddRange(q);

        // ──────────────────────────────────────────────
        // BADGES
        // ──────────────────────────────────────────────
        var badgeCh1 = Guid.NewGuid(); var badgeCh2 = Guid.NewGuid();
        var badgeCh3 = Guid.NewGuid(); var badgeCh4 = Guid.NewGuid();
        var badgeCh5 = Guid.NewGuid(); var badgeCh6 = Guid.NewGuid();
        var badgeCh7 = Guid.NewGuid(); var badgeCh8 = Guid.NewGuid();
        var badgeCh9 = Guid.NewGuid(); var badgeCh10 = Guid.NewGuid();
        var badgeExcellent = Guid.NewGuid();
        var badgeMathematician = Guid.NewGuid();

        context.Badges.AddRange(
            new Badge { Id = badgeCh1, Title = "Hoàn thành Chương 1: Đa thức", Description = "Vượt qua quiz Chương 1", IconUrl = "/images/badges/chapter1.png", ConditionType = "complete_chapter", ConditionValue = JsonSerializer.Serialize(new { chapterId = c1.ToString() }) },
            new Badge { Id = badgeCh2, Title = "Hoàn thành Chương 2: Hằng đẳng thức", Description = "Vượt qua quiz Chương 2", IconUrl = "/images/badges/chapter2.png", ConditionType = "complete_chapter", ConditionValue = JsonSerializer.Serialize(new { chapterId = c2.ToString() }) },
            new Badge { Id = badgeCh3, Title = "Hoàn thành Chương 3: Phân thức", Description = "Vượt qua quiz Chương 3", IconUrl = "/images/badges/chapter3.png", ConditionType = "complete_chapter", ConditionValue = JsonSerializer.Serialize(new { chapterId = c3.ToString() }) },
            new Badge { Id = badgeCh4, Title = "Hoàn thành Chương 4: Phương trình", Description = "Vượt qua quiz Chương 4", IconUrl = "/images/badges/chapter4.png", ConditionType = "complete_chapter", ConditionValue = JsonSerializer.Serialize(new { chapterId = c4.ToString() }) },
            new Badge { Id = badgeCh5, Title = "Hoàn thành Chương 5: Tứ giác", Description = "Vượt qua quiz Chương 5", IconUrl = "/images/badges/chapter5.png", ConditionType = "complete_chapter", ConditionValue = JsonSerializer.Serialize(new { chapterId = c5.ToString() }) },
            new Badge { Id = badgeCh6, Title = "Hoàn thành Chương 6: Định lí Thalès", Description = "Vượt qua quiz Chương 6", IconUrl = "/images/badges/chapter6.png", ConditionType = "complete_chapter", ConditionValue = JsonSerializer.Serialize(new { chapterId = c6.ToString() }) },
            new Badge { Id = badgeCh7, Title = "Hoàn thành Chương 7: Tam giác đồng dạng", Description = "Vượt qua quiz Chương 7", IconUrl = "/images/badges/chapter7.png", ConditionType = "complete_chapter", ConditionValue = JsonSerializer.Serialize(new { chapterId = c7.ToString() }) },
            new Badge { Id = badgeCh8, Title = "Hoàn thành Chương 8: Hình chóp đều", Description = "Vượt qua quiz Chương 8", IconUrl = "/images/badges/chapter8.png", ConditionType = "complete_chapter", ConditionValue = JsonSerializer.Serialize(new { chapterId = c8.ToString() }) },
            new Badge { Id = badgeCh9, Title = "Hoàn thành Chương 9: Thống kê", Description = "Vượt qua quiz Chương 9", IconUrl = "/images/badges/chapter9.png", ConditionType = "complete_chapter", ConditionValue = JsonSerializer.Serialize(new { chapterId = c9.ToString() }) },
            new Badge { Id = badgeCh10, Title = "Hoàn thành Chương 10: Xác suất", Description = "Vượt qua quiz Chương 10", IconUrl = "/images/badges/chapter10.png", ConditionType = "complete_chapter", ConditionValue = JsonSerializer.Serialize(new { chapterId = c10.ToString() }) },
            new Badge { Id = badgeExcellent, Title = "Học sinh xuất sắc", Description = "Đạt 3 lần điểm tuyệt đối liên tiếp trong bài kiểm tra", IconUrl = "/images/badges/excellent.png", ConditionType = "perfect_quiz_streak", ConditionValue = JsonSerializer.Serialize(new { streak = 3 }) },
            new Badge { Id = badgeMathematician, Title = "Nhà toán học", Description = "Tích lũy 100 xu", IconUrl = "/images/badges/mathematician.png", ConditionType = "total_coins", ConditionValue = JsonSerializer.Serialize(new { coins = 100 }) });

        // ──────────────────────────────────────────────
        // REWARD POLICIES
        // ──────────────────────────────────────────────
        var lessonPolicy = new RewardPolicy
        {
            Id = Guid.NewGuid(),
            Name = "Thưởng quiz bài học mặc định",
            QuizType = QuizType.Lesson,
            CoinsPerCorrectAnswer = 10,
            FirstPassBonusCoins = 15,
            PerfectScoreBonusCoins = 10,
            RetryRewardPercent = 50,
            DailyCoinLimit = 300,
            EffectiveFrom = DateTime.UtcNow.AddDays(-1)
        };
        var chapterPolicy = new RewardPolicy
        {
            Id = Guid.NewGuid(),
            Name = "Thưởng quiz chương mặc định",
            QuizType = QuizType.Chapter,
            CoinsPerCorrectAnswer = 10,
            FirstPassBonusCoins = 30,
            PerfectScoreBonusCoins = 15,
            ChapterCompletionBonusCoins = 50,
            RetryRewardPercent = 25,
            DailyCoinLimit = 300,
            EffectiveFrom = DateTime.UtcNow.AddDays(-1)
        };
        context.RewardPolicies.AddRange(lessonPolicy, chapterPolicy);

        // ──────────────────────────────────────────────
        // QUIZZES
        // ──────────────────────────────────────────────
        var allLessons = new[] { l1_1, l1_2, l1_3, l1_4, l1_5, l2_1, l2_2, l2_3, l2_4, l3_1, l3_2, l3_3, l3_4, l4_1, l4_2, l4_3, l4_4, l4_5, l5_1, l5_2, l5_3, l5_4, l5_5, l6_1, l6_2, l6_3, l7_1, l7_2, l7_3, l7_4, l7_5, l8_1, l8_2, l9_1, l9_2, l9_3, l10_1, l10_2, l10_3 };
        var allLessonsList = new List<Lesson>(allLessons);
        var allChapters = new[] { c1, c2, c3, c4, c5, c6, c7, c8, c9, c10 };
        var allChapterNames = new[] { "Đa thức", "Hằng đẳng thức", "Phân thức đại số", "Phương trình và hàm số", "Tứ giác", "Định lí Thalès", "Tam giác đồng dạng", "Hình chóp đều", "Dữ liệu và biểu đồ", "Xác suất" };

        var quizzes = new List<Quiz>();
        foreach (var lesson in allLessonsList)
        {
            quizzes.Add(CreateQuiz(QuizType.Lesson, lesson.Id, null, $"Quiz: {lesson.Title}", lessonPolicy.Id));
        }
        for (var i = 0; i < allChapters.Length; i++)
        {
            quizzes.Add(CreateQuiz(QuizType.Chapter, null, allChapters[i], $"Kiểm tra Chương {i + 1}: {allChapterNames[i]}", chapterPolicy.Id));
        }
        context.Quizzes.AddRange(quizzes);

        // ──────────────────────────────────────────────
        // QUIZQUESTIONS
        // ──────────────────────────────────────────────
        var lessonQuizIdx = 0;
        var questionsByLesson = new Dictionary<Guid, List<Question>>();
        foreach (var lesson in allLessonsList)
        {
            questionsByLesson[lesson.Id] = q.Where(x => x.LessonId == lesson.Id).OrderBy(x => x.OrderIndex).ToList();
        }

        // Assign lesson quiz questions
        foreach (var lesson in allLessonsList)
        {
            var lessonQs = questionsByLesson[lesson.Id];
            AddQuizQuestions(context, quizzes[lessonQuizIdx].Id, lessonQs);
            lessonQuizIdx++;
        }

        // Assign chapter quiz questions (mix of questions from that chapter's lessons)
        var chapterLessonMap = new Dictionary<Guid, List<Lesson>>
        {
            { c1, new() { l1_1, l1_2, l1_3, l1_4, l1_5 } },
            { c2, new() { l2_1, l2_2, l2_3, l2_4 } },
            { c3, new() { l3_1, l3_2, l3_3, l3_4 } },
            { c4, new() { l4_1, l4_2, l4_3, l4_4, l4_5 } },
            { c5, new() { l5_1, l5_2, l5_3, l5_4, l5_5 } },
            { c6, new() { l6_1, l6_2, l6_3 } },
            { c7, new() { l7_1, l7_2, l7_3, l7_4, l7_5 } },
            { c8, new() { l8_1, l8_2 } },
            { c9, new() { l9_1, l9_2, l9_3 } },
            { c10, new() { l10_1, l10_2, l10_3 } }
        };

        var chapterQuizStartIdx = allLessonsList.Count;
        for (var ci = 0; ci < allChapters.Length; ci++)
        {
            var chapterId = allChapters[ci];
            var chapterQs = chapterLessonMap[chapterId]
                .SelectMany(l => questionsByLesson[l.Id])
                .Take(6)
                .ToList();
            AddQuizQuestions(context, quizzes[chapterQuizStartIdx + ci].Id, chapterQs);
        }

        // ──────────────────────────────────────────────
        // BADGE RULES
        // ──────────────────────────────────────────────
        var badgeRuleList = new List<BadgeRule>();

        var allBadgeChapters = new[] { badgeCh1, badgeCh2, badgeCh3, badgeCh4, badgeCh5, badgeCh6, badgeCh7, badgeCh8, badgeCh9, badgeCh10 };
        for (var i = 0; i < allChapters.Length; i++)
        {
            badgeRuleList.Add(new BadgeRule
            {
                BadgeId = allBadgeChapters[i],
                RuleType = "complete_chapter",
                TargetChapterId = allChapters[i],
                TargetQuizId = quizzes[chapterQuizStartIdx + i].Id,
                OrderIndex = 1
            });
        }

        badgeRuleList.AddRange(new[]
        {
            new BadgeRule { BadgeId = badgeExcellent, RuleType = "perfect_quiz_streak", ThresholdValue = 3, OrderIndex = 1 },
            new BadgeRule { BadgeId = badgeMathematician, RuleType = "total_coins", ThresholdValue = 100, OrderIndex = 1 }
        });

        context.BadgeRules.AddRange(badgeRuleList);
        await context.SaveChangesAsync();
    }

    // ──────────────────────────────────────────────
    // HELPERS
    // ──────────────────────────────────────────────
    private static async Task SeedAdditionalStudentsAsync(AppDbContext context)
    {
        // Chỉ query các email thuộc bộ seed, không tải toàn bộ bảng Users.
        var seedEmails = AdditionalStudentDefinitions
            .Select(student => student.Email)
            .ToArray();
        var existingEmails = await context.Users
            .Where(user => seedEmails.Contains(user.Email))
            .Select(user => user.Email)
            .ToListAsync();
        // So sánh không phân biệt hoa/thường để email seed không bị tạo trùng logic.
        var existingEmailSet = existingEmails.ToHashSet(StringComparer.OrdinalIgnoreCase);
        // Lọc đúng những định nghĩa chưa tồn tại trong database hiện tại.
        var missingStudents = AdditionalStudentDefinitions
            .Where(student => !existingEmailSet.Contains(student.Email))
            .ToArray();

        // Idempotent: chạy startup lần sau sẽ thoát tại đây và không insert thêm.
        if (missingStudents.Length == 0)
            return;

        context.Users.AddRange(CreateAdditionalStudents(missingStudents));
        await context.SaveChangesAsync();
    }

    private static IEnumerable<User> CreateAdditionalStudents(
        IEnumerable<(string Name, string Email, int Coins)> definitions)
    {
        // Dùng cùng mốc thời gian cho toàn bộ batch seed.
        var now = DateTime.UtcNow;
        foreach (var definition in definitions)
        {
            // yield return tạo từng entity nhưng chỉ AddRange mới đưa vào DbContext.
            yield return new User
            {
                Id = Guid.NewGuid(),
                Name = definition.Name,
                Email = definition.Email,
                // Không lưu mật khẩu thuần trong database.
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(DemoStudentPassword),
                Role = "Student",
                Coins = definition.Coins,
                CoinsUpdatedAt = now,
                CreatedAt = now,
                UpdatedAt = now
            };
        }
    }

    private static Guid Id(string seed)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(seed));
        return new Guid(bytes.Take(16).ToArray());
    }

    private static void Q(List<Question> list, Lesson lesson, string text, string[] options, int correct, string explanation)
    {
        list.Add(new Question
        {
            Id = Guid.NewGuid(),
            LessonId = lesson.Id,
            QuestionText = text,
            Options = JsonSerializer.Serialize(options),
            CorrectOption = correct,
            Explanation = explanation,
            OrderIndex = (list.Count(x => x.LessonId == lesson.Id)) + 1
        });
    }

    private static Lesson Lesson(Guid chapterId, Guid topicId, string title, int order, string? simType, string content)
    {
        return new Lesson
        {
            Id = Guid.NewGuid(),
            ChapterId = chapterId,
            CurriculumTopicId = topicId,
            Title = title,
            ContentBody = content,
            SimulationType = simType,
            OrderIndex = order,
            IsPublished = true
        };
    }

    private static async Task RepairLegacyBadgeRuleThresholdsAsync(AppDbContext context)
    {
        var rules = await context.BadgeRules
            .Include(rule => rule.Badge)
            .Where(rule =>
                (rule.RuleType == "total_coins"
                    || rule.RuleType == "passed_quizzes"
                    || rule.RuleType == "perfect_quiz_streak")
                && (!rule.ThresholdValue.HasValue || rule.ThresholdValue <= 0)
                && rule.Badge.ConditionValue != null)
            .ToListAsync();

        var repaired = false;
        foreach (var rule in rules)
        {
            var threshold = ExtractThreshold(rule.Badge.ConditionValue);
            if (threshold <= 0)
                continue;

            rule.ThresholdValue = threshold;
            repaired = true;
        }

        if (repaired)
            await context.SaveChangesAsync();
    }

    private static int ExtractThreshold(string? value)
    {
        if (int.TryParse(value?.Trim('"'), out var direct))
            return direct;

        try
        {
            var values = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(value ?? "{}");
            foreach (var key in new[] { "value", "coins", "streak", "count" })
            {
                if (values?.TryGetValue(key, out var element) == true
                    && element.TryGetInt32(out var threshold))
                    return threshold;
            }
        }
        catch (JsonException) { }

        return 0;
    }

    private static Quiz CreateQuiz(QuizType type, Guid? lessonId, Guid? chapterId, string title, Guid rewardPolicyId)
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

    private static void AddQuizQuestions(AppDbContext context, Guid quizId, IReadOnlyList<Question> questions)
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
