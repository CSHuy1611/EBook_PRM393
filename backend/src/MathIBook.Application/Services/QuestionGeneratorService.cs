using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MathIBook.Application.DTOs;

namespace MathIBook.Application.Services;

public interface IQuestionGeneratorService
{
    Task<List<QuestionCreateDto>> GenerateQuestionsAsync(Guid? lessonId, Guid? chapterId, string title, int count, string level);
}

public class QuestionGeneratorService : IQuestionGeneratorService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public QuestionGeneratorService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["OpenAI:ApiKey"] ?? "";
    }

    public async Task<List<QuestionCreateDto>> GenerateQuestionsAsync(Guid? lessonId, Guid? chapterId, string title, int count, string level)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new Exception("OpenAI API Key chưa được cấu hình. Vui lòng thêm OpenAI:ApiKey vào appsettings.json.");
        }

        var systemPrompt = $@"
Bạn là một giáo viên Toán lớp 8 xuất sắc.
Nhiệm vụ của bạn là tạo ra {count} câu hỏi trắc nghiệm Toán học phù hợp với {level} có tên: '{title}'.
Yêu cầu bắt buộc:
1. Mỗi câu hỏi phải có chính xác 4 đáp án.
2. Có 1 đáp án đúng và 3 đáp án sai.
3. Phần giải thích (Explanation) phải rõ ràng, ngắn gọn và dễ hiểu cho học sinh lớp 8.
4. Mọi công thức Toán học phải được bọc trong cặp dấu $...$ (ví dụ: $x^2 - 4 = 0$) để tương thích hiển thị.
5. KẾT QUẢ TRẢ VỀ PHẢI LÀ MỘT MẢNG JSON HỢP LỆ VỚI CẤU TRÚC SAU (không chứa code block markdown, chỉ trả về JSON raw):
[
  {{
    ""QuestionText"": ""nội dung câu hỏi..."",
    ""Options"": [""đáp án A"", ""đáp án B"", ""đáp án C"", ""đáp án D""],
    ""CorrectOption"": 0,
    ""Explanation"": ""giải thích chi tiết...""
  }}
]
";

        var requestBody = new
        {
            model = "gpt-3.5-turbo",
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = $"Hãy tạo {count} câu hỏi." }
            },
            temperature = 0.7
        };

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        requestMessage.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(requestMessage);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new Exception($"Lỗi khi gọi OpenAI API: {response.StatusCode} - {errorBody}");
        }

        var responseBody = await response.Content.ReadAsStringAsync();
        using var jsonDocument = JsonDocument.Parse(responseBody);
        var choices = jsonDocument.RootElement.GetProperty("choices");
        var content = choices[0].GetProperty("message").GetProperty("content").GetString() ?? "";

        content = content.Trim();
        if (content.StartsWith("```json")) content = content.Substring(7);
        if (content.StartsWith("```")) content = content.Substring(3);
        if (content.EndsWith("```")) content = content.Substring(0, content.Length - 3);
        content = content.Trim();

        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var generatedItems = JsonSerializer.Deserialize<List<OpenAiQuestionDto>>(content, jsonOptions);

        if (generatedItems == null || generatedItems.Count == 0)
        {
            throw new Exception("OpenAI API trả về dữ liệu không hợp lệ hoặc rỗng.");
        }

        var result = new List<QuestionCreateDto>();
        for (int i = 0; i < generatedItems.Count; i++)
        {
            var item = generatedItems[i];
            result.Add(new QuestionCreateDto
            {
                LessonId = lessonId ?? Guid.Empty, // Temporary, will be assigned correctly in Controller
                ChapterId = chapterId,
                QuestionText = item.QuestionText,
                Options = item.Options,
                CorrectOption = item.CorrectOption,
                Explanation = item.Explanation,
                OrderIndex = i + 1
            });
        }

        return result;
    }

    private class OpenAiQuestionDto
    {
        public string QuestionText { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new();
        public int CorrectOption { get; set; }
        public string Explanation { get; set; } = string.Empty;
    }
}
