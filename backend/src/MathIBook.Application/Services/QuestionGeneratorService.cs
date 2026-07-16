using System;
using System.Collections.Generic;
using System.Linq;
using MathIBook.Application.DTOs;

namespace MathIBook.Application.Services;

public interface IQuestionGeneratorService
{
    List<QuestionCreateDto> GenerateQuestions(Guid lessonId, string lessonTitle, int count);
}

public class QuestionGeneratorService : IQuestionGeneratorService
{
    private static readonly List<MockQuestion> _bank = new()
    {
        // Topic: Phân thức đại số
        new MockQuestion("phân thức", @"Điều kiện xác định của phân thức $\frac{x+1}{x-2}$ là gì?", new[] { @"$x \neq 2$", @"$x \neq -1$", @"$x = 2$", @"$x \neq 0$" }, 0, @"Mẫu thức phải khác 0, tức là $x - 2 \neq 0 \Leftrightarrow x \neq 2$."),
        new MockQuestion("phân thức", @"Rút gọn phân thức $\frac{2x}{4x^2}$ (với $x \neq 0$) ta được:", new[] { @"$\frac{1}{2}$", @"$\frac{1}{2x}$", @"$2x$", @"$\frac{1}{x}$" }, 1, @"Chia cả tử và mẫu cho $2x$, ta có $\frac{2x}{4x^2} = \frac{1}{2x}$."),
        new MockQuestion("phân thức", @"Phân thức đối của phân thức $\frac{x-1}{x+1}$ là:", new[] { @"$\frac{1-x}{x+1}$", @"$\frac{x+1}{x-1}$", @"$\frac{-(x+1)}{x-1}$", @"$\frac{x-1}{-(x+1)}$" }, 0, @"Phân thức đối của $\frac{A}{B}$ là $-\frac{A}{B} = \frac{-A}{B} = \frac{1-x}{x+1}$."),
        new MockQuestion("phân thức", @"Kết quả phép tính $\frac{x}{x-1} + \frac{1}{1-x}$ là:", new[] { @"$1$", @"$0$", @"$\frac{x+1}{x-1}$", @"$-1$" }, 0, @"Ta có $\frac{1}{1-x} = \frac{-1}{x-1}$. Nên biểu thức bằng $\frac{x-1}{x-1} = 1$."),
        
        // Topic: Phương trình
        new MockQuestion("phương trình", @"Nghiệm của phương trình $2x - 4 = 0$ là:", new[] { @"$x = -2$", @"$x = 4$", @"$x = 2$", @"$x = 0$" }, 2, @"$2x - 4 = 0 \Leftrightarrow 2x = 4 \Leftrightarrow x = 2$."),
        new MockQuestion("phương trình", @"Phương trình nào sau đây là phương trình bậc nhất một ẩn?", new[] { @"$0x + 3 = 0$", @"$2x + y = 0$", @"$x^2 - 1 = 0$", @"$3x - 5 = 0$" }, 3, @"Phương trình bậc nhất một ẩn có dạng $ax+b=0$ với $a \neq 0$."),
        new MockQuestion("phương trình", @"Hai phương trình tương đương là:", new[] { @"Hai phương trình có cùng một tập nghiệm.", @"Hai phương trình có cùng số ẩn.", @"Hai phương trình có cùng bậc.", @"Hai phương trình vô nghiệm." }, 0, @"Theo định nghĩa, hai phương trình được gọi là tương đương nếu chúng có cùng một tập nghiệm."),
        new MockQuestion("phương trình", @"Nghiệm của phương trình $x(x-2)=0$ là:", new[] { @"$x=0$", @"$x=2$", @"$x=0$ hoặc $x=2$", @"Vô nghiệm" }, 2, @"Phương trình tích $A \cdot B = 0 \Leftrightarrow A=0$ hoặc $B=0$."),
        
        // Topic: Đa giác / Hình học
        new MockQuestion("đa giác", @"Tổng số đo các góc trong của tứ giác là:", new[] { @"$180^\circ$", @"$360^\circ$", @"$540^\circ$", @"$720^\circ$" }, 1, @"Tổng số đo các góc của một đa giác $n$ cạnh là $(n-2)\times 180^\circ$. Với tứ giác $n=4$, tổng là $360^\circ$."),
        new MockQuestion("tứ giác", @"Hình bình hành có hai đường chéo bằng nhau là hình gì?", new[] { @"Hình thoi", @"Hình chữ nhật", @"Hình vuông", @"Hình thang cân" }, 1, @"Dấu hiệu nhận biết: Hình bình hành có hai đường chéo bằng nhau là hình chữ nhật."),
        new MockQuestion("tứ giác", @"Hình thoi có một góc vuông là hình gì?", new[] { @"Hình chữ nhật", @"Hình bình hành", @"Hình vuông", @"Hình thang vuông" }, 2, @"Hình thoi có 1 góc vuông thì nó là hình vuông."),
        
        // Topic: Bất phương trình
        new MockQuestion("bất phương trình", @"Nghiệm của bất phương trình $2x > 4$ là:", new[] { @"$x > 2$", @"$x < 2$", @"$x \geq 2$", @"$x \leq 2$" }, 0, @"Chia 2 vế cho 2 (số dương, giữ nguyên chiều): $x > 2$."),
        new MockQuestion("bất phương trình", @"Nếu $a > b$ thì:", new[] { @"$a - c > b - c$", @"$a - c < b - c$", @"$ac > bc$", @"$-a > -b$" }, 0, @"Tính chất của bất đẳng thức: cộng/trừ cùng 1 số thì chiều không đổi."),
        
        // Default / Generic math
        new MockQuestion("toán", @"Giá trị của biểu thức $x^2 - 4x + 4$ tại $x=2$ là:", new[] { @"0", @"1", @"4", @"-4" }, 0, @"Thay $x=2$ vào biểu thức: $2^2 - 4(2) + 4 = 4 - 8 + 4 = 0$."),
        new MockQuestion("toán", @"Phân tích đa thức $x^2 - 9$ thành nhân tử:", new[] { @"$(x-3)^2$", @"$(x-3)(x+3)$", @"$(x+3)^2$", @"$x(x-9)$" }, 1, @"Áp dụng hằng đẳng thức hiệu hai bình phương: $A^2 - B^2 = (A-B)(A+B)$."),
        new MockQuestion("toán", @"Điều kiện để tam giác ABC đồng dạng với tam giác MNP theo trường hợp (c.c.c) là:", new[] { @"$\frac{AB}{MN} = \frac{BC}{NP} = \frac{CA}{PM}$", @"$\frac{AB}{MN} = \frac{BC}{MP}$", @"$\frac{AB}{MN} = \frac{CA}{NP}$", @"$AB=MN, BC=NP$" }, 0, @"Hai tam giác đồng dạng theo trường hợp c.c.c khi tỉ số 3 cặp cạnh tương ứng bằng nhau.")
    };

    public List<QuestionCreateDto> GenerateQuestions(Guid lessonId, string lessonTitle, int count)
    {
        var titleLower = lessonTitle.ToLowerInvariant();
        
        // Filter questions that match keywords in the lesson title
        var matched = _bank.Where(q => titleLower.Contains(q.Keyword)).ToList();
        
        // If not enough questions match, fallback to generic ones
        if (matched.Count < count)
        {
            var fallback = _bank.Where(q => !matched.Contains(q)).ToList();
            matched.AddRange(fallback);
        }

        // Shuffle and take count
        var rnd = new Random();
        var selected = matched.OrderBy(x => rnd.Next()).Take(count).ToList();

        var result = new List<QuestionCreateDto>();
        for (int i = 0; i < selected.Count; i++)
        {
            var sq = selected[i];
            result.Add(new QuestionCreateDto
            {
                LessonId = lessonId,
                QuestionText = sq.QuestionText,
                Options = sq.Options.ToList(),
                CorrectOption = sq.CorrectOption,
                Explanation = sq.Explanation,
                OrderIndex = i + 1
            });
        }

        return result;
    }
}

public class MockQuestion
{
    public string Keyword { get; }
    public string QuestionText { get; }
    public string[] Options { get; }
    public int CorrectOption { get; }
    public string Explanation { get; }

    public MockQuestion(string keyword, string questionText, string[] options, int correctOption, string explanation)
    {
        Keyword = keyword;
        QuestionText = questionText;
        Options = options;
        CorrectOption = correctOption;
        Explanation = explanation;
    }
}
