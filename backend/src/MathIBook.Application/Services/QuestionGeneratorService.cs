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

    // Danh sách các model miễn phí trên OpenRouter, thử lần lượt nếu bị rate-limit
    private static readonly string[] FreeModels = new[]
    {
        "google/gemma-4-31b-it:free",
        "google/gemma-4-26b-a4b-it:free",
        "nvidia/nemotron-3-nano-30b-a3b:free",
        "nvidia/nemotron-3-super-120b-a12b:free",
        "openai/gpt-oss-20b:free"
    };

    public QuestionGeneratorService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["OpenRouter:ApiKey"] ?? "";
    }

    public async Task<List<QuestionCreateDto>> GenerateQuestionsAsync(Guid? lessonId, Guid? chapterId, string title, int count, string level)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new Exception("OpenRouter API Key chưa được cấu hình. Vui lòng thêm OpenRouter:ApiKey vào appsettings.json.");
        }

        var systemPrompt = $@"
Bạn là một giáo viên Toán lớp 8 xuất sắc.
Nhiệm vụ của bạn là tạo ra {count} câu hỏi trắc nghiệm Toán học phù hợp với {level} có tên: '{title}'.
Yêu cầu bắt buộc:
1. Mỗi câu hỏi phải có chính xác 4 đáp án.
2. Có 1 đáp án đúng và 3 đáp án sai.
3. Phần giải thích (Explanation) phải rõ ràng, ngắn gọn và dễ hiểu cho học sinh lớp 8.
4. Mọi công thức Toán học phải được bọc trong cặp dấu $...$ (ví dụ: $x^2 - 4 = 0$) để tương thích hiển thị.
5. KẾT QUẢ TRẢ VỀ PHẢI LÀ MỘT MẢNG JSON HỢP LỆ VỚI CẤU TRÚC SAU. TUYỆT ĐỐI không chứa văn bản nào khác. Mọi ký tự nháy kép hoặc gạch chéo ngược (\) bên trong nội dung phải được escape đúng chuẩn JSON (ví dụ: \\):
[
  {{
    ""QuestionText"": ""nội dung câu hỏi..."",
    ""Options"": [""đáp án A"", ""đáp án B"", ""đáp án C"", ""đáp án D""],
    ""CorrectOption"": 0,
    ""Explanation"": ""giải thích chi tiết...""
  }}
]
";

        // Thử lần lượt từng model miễn phí cho đến khi thành công
        string? lastError = null;
        foreach (var model in FreeModels)
        {
            try
            {
                var content = await CallOpenRouterAsync(model, systemPrompt, count);
                return ParseQuestions(content, lessonId, chapterId);
            }
            catch (RateLimitException ex)
            {
                // Model bị rate-limit, thử model tiếp theo
                lastError = $"Model {model}: {ex.Message}";
                continue;
            }
        }

        throw new Exception($"Tất cả model miễn phí đều đang bị giới hạn. Vui lòng thử lại sau ít giây. Chi tiết: {lastError}");
    }

    private async Task<string> CallOpenRouterAsync(string model, string systemPrompt, int count)
    {
        var requestBody = new
        {
            model = model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = $"Hãy tạo {count} câu hỏi." }
            },
            temperature = 0.7
        };

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://openrouter.ai/api/v1/chat/completions");
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        requestMessage.Headers.Add("HTTP-Referer", "https://mathibook.app");
        requestMessage.Headers.Add("X-Title", "MathIBook");
        requestMessage.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(requestMessage);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            
            // Nếu bị rate-limit (429), ném RateLimitException để thử model khác
            if ((int)response.StatusCode == 429)
            {
                throw new RateLimitException($"Rate-limited: {errorBody}");
            }

            throw new Exception($"Lỗi khi gọi AI API ({model}): {response.StatusCode} - {errorBody}");
        }

        var responseBody = await response.Content.ReadAsStringAsync();
        using var jsonDocument = JsonDocument.Parse(responseBody);
        var choices = jsonDocument.RootElement.GetProperty("choices");
        var content = choices[0].GetProperty("message").GetProperty("content").GetString() ?? "";

        return content;
    }

    private List<QuestionCreateDto> ParseQuestions(string content, Guid? lessonId, Guid? chapterId)
    {
        // Trích xuất chuỗi JSON chuẩn xác từ kết quả của AI
        int startIndex = content.IndexOf('[');
        int endIndex = content.LastIndexOf(']');

        if (startIndex >= 0 && endIndex >= 0 && endIndex > startIndex)
        {
            // Nếu tìm thấy mảng JSON
            content = content.Substring(startIndex, endIndex - startIndex + 1);
        }
        else
        {
            // Thử tìm JSON Object nếu không thấy mảng
            startIndex = content.IndexOf('{');
            endIndex = content.LastIndexOf('}');
            if (startIndex >= 0 && endIndex >= 0 && endIndex > startIndex)
            {
                content = content.Substring(startIndex, endIndex - startIndex + 1);
            }
        }

        // Tự động sửa lỗi escape backslash từ AI (chủ yếu do các thẻ LaTeX như \cdot, \frac, \theta...)
        // Tìm các dấu backslash không đứng trước dấu nháy kép hoặc một backslash khác và nhân đôi nó lên.
        content = System.Text.RegularExpressions.Regex.Replace(content, @"(?<!\\)\\(?![""\\])", @"\\");

        List<GeneratedQuestionDto>? generatedItems = null;
        var jsonOptions = new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        try
        {
            generatedItems = JsonSerializer.Deserialize<List<GeneratedQuestionDto>>(content, jsonOptions);
        }
        catch (Exception ex)
        {
            try
            {
                using var wrapper = JsonDocument.Parse(content);
                var root = wrapper.RootElement;
                foreach (var prop in root.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.Array)
                    {
                        generatedItems = JsonSerializer.Deserialize<List<GeneratedQuestionDto>>(prop.Value.GetRawText(), jsonOptions);
                        break;
                    }
                }
            }
            catch (Exception innerEx)
            {
                throw new Exception($"Lỗi Parse JSON: {ex.Message}. Nội dung: {content.Substring(0, Math.Min(200, content.Length))}");
            }
        }

        if (generatedItems == null || generatedItems.Count == 0)
        {
            throw new Exception("AI trả về dữ liệu không hợp lệ hoặc rỗng.");
        }

        var result = new List<QuestionCreateDto>();
        for (int i = 0; i < generatedItems.Count; i++)
        {
            var item = generatedItems[i];
            result.Add(new QuestionCreateDto
            {
                LessonId = lessonId ?? Guid.Empty,
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

    private class RateLimitException : Exception
    {
        public RateLimitException(string message) : base(message) { }
    }

    private class GeneratedQuestionDto
    {
        public string QuestionText { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new();
        public int CorrectOption { get; set; }
        public string Explanation { get; set; } = string.Empty;
    }
}
