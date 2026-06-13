using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UNI_EDU_Backend.Application.Interfaces;

namespace UNI_EDU_Backend.Infrastructure.Ai;

// Generates MCQs via OpenAI when an API key is configured (Ai:OpenAiKey or OPENAI_API_KEY env),
// and falls back to deterministic template questions so the flow still works without a key.
public class OpenAiQuestionGenerator(IConfiguration configuration, ILogger<OpenAiQuestionGenerator> logger) : IAiQuestionGenerator
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(60) };
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<OpenAiQuestionGenerator> _logger = logger;

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public Task<List<GeneratedQuestion>> GenerateAsync(string subjectName, int count, CancellationToken cancellationToken) =>
        GenerateAsync(subjectName, count, null, null, null, cancellationToken);

    public async Task<List<GeneratedQuestion>> GenerateAsync(string subjectName, int count, string? topic, int? grade, string? difficulty, CancellationToken cancellationToken)
    {
        var apiKey = _configuration["Ai:OpenAiKey"]
            ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("No OpenAI API key configured (Ai:OpenAiKey / OPENAI_API_KEY) — using template questions for '{Subject}'.", subjectName);
            return Fallback(subjectName, count);
        }

        try
        {
            return await CallOpenAiAsync(apiKey, BuildPrompt(subjectName, count, topic, grade, difficulty), subjectName, count, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI question generation failed for '{Subject}' — falling back to template questions.", subjectName);
            return Fallback(subjectName, count);
        }
    }

    private static string BuildPrompt(string subjectName, int count, string? topic, int? grade, string? difficulty)
    {
        var diff = (difficulty ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "easy" => "dễ", "hard" => "khó", _ => "trung bình"
        };
        var gradePart = grade is > 0 ? $" cho học sinh lớp {grade}" : "";
        var topicPart = string.IsNullOrWhiteSpace(topic) ? "" : $" về chủ đề \"{topic.Trim()}\"";
        return
            $"Tạo {count} câu hỏi trắc nghiệm (mỗi câu 4 lựa chọn) bằng tiếng Việt môn \"{subjectName}\"{gradePart}{topicPart}, độ khó {diff}. " +
            "Chỉ trả về JSON đúng định dạng: {\"questions\":[{\"content\":\"...\",\"options\":[\"A\",\"B\",\"C\",\"D\"],\"correctIndex\":0}]}. " +
            "correctIndex là chỉ số (0-3) của đáp án đúng. Không thêm chữ nào ngoài JSON.";
    }

    private async Task<List<GeneratedQuestion>> CallOpenAiAsync(string apiKey, string prompt, string subjectName, int count, CancellationToken cancellationToken)
    {
        var model = _configuration["Ai:OpenAiModel"] ?? "gpt-4o-mini";
        var http = Http;

        var body = new
        {
            model,
            messages = new object[]
            {
                new { role = "system", content = "Bạn là trợ lý tạo đề kiểm tra. Chỉ trả về JSON hợp lệ." },
                new { role = "user", content = prompt }
            },
            temperature = 0.7,
            response_format = new { type = "json_object" }
        };

        // Provider-agnostic: defaults to OpenAI, override Ai:BaseUrl (Ai__BaseUrl) for
        // OpenAI-compatible gateways like OpenRouter (https://openrouter.ai/api/v1).
        var baseUrl = (_configuration["Ai:BaseUrl"] ?? "https://api.openai.com/v1").TrimEnd('/');
        using var req = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/chat/completions")
        {
            Content = JsonContent.Create(body)
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        // OpenRouter ranking headers (ignored by OpenAI).
        req.Headers.TryAddWithoutValidation("HTTP-Referer", "https://unieducation.net");
        req.Headers.TryAddWithoutValidation("X-Title", "Uni Education");

        using var resp = await http.SendAsync(req, cancellationToken);
        resp.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(cancellationToken));
        var content = ExtractContent(doc.RootElement);
        if (string.IsNullOrWhiteSpace(content))
            return Fallback(subjectName, count);

        var parsed = JsonSerializer.Deserialize<GeneratedQuestionsEnvelope>(content, JsonOpts);
        var questions = parsed?.Questions?
            .Where(q => q.Options is { Count: >= 2 } && q.CorrectIndex >= 0 && q.CorrectIndex < q.Options.Count)
            .Select(q => new GeneratedQuestion(q.Content ?? "", q.Options!, q.CorrectIndex))
            .ToList();

        return questions is { Count: > 0 } ? questions : Fallback(subjectName, count);
    }

    // Safe navigation of choices[0].message.content — returns null if any node is missing/empty.
    private static string? ExtractContent(JsonElement root)
    {
        if (root.TryGetProperty("choices", out var choices) && choices.ValueKind == JsonValueKind.Array
            && choices.GetArrayLength() > 0
            && choices[0].TryGetProperty("message", out var message)
            && message.TryGetProperty("content", out var content))
        {
            return content.GetString();
        }
        return null;
    }

    private static List<GeneratedQuestion> Fallback(string subjectName, int count)
    {
        var list = new List<GeneratedQuestion>();
        for (int i = 1; i <= count; i++)
        {
            list.Add(new GeneratedQuestion(
                $"[{subjectName}] Câu hỏi mẫu số {i}: Đâu là phát biểu đúng về kiến thức {subjectName} cơ bản (câu {i})?",
                new List<string> { "Đáp án A (đúng)", "Đáp án B", "Đáp án C", "Đáp án D" },
                0));
        }
        return list;
    }

    private sealed class GeneratedQuestionsEnvelope
    {
        public List<RawQuestion>? Questions { get; set; }
    }

    private sealed class RawQuestion
    {
        public string? Content { get; set; }
        public List<string>? Options { get; set; }
        public int CorrectIndex { get; set; }
    }
}
