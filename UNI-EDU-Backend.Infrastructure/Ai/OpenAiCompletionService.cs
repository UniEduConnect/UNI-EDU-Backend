using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UNI_EDU_Backend.Application.Interfaces;

namespace UNI_EDU_Backend.Infrastructure.Ai;

// OpenAI chat-completion (json_object) with graceful null fallback when no key is set.
public class OpenAiCompletionService(IConfiguration configuration, ILogger<OpenAiCompletionService> logger) : IAiCompletionService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(40) };
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<OpenAiCompletionService> _logger = logger;

    public async Task<T?> CompleteJsonAsync<T>(string systemPrompt, string userPrompt, CancellationToken cancellationToken) where T : class
    {
        var apiKey = _configuration["Ai:OpenAiKey"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("No OpenAI API key — CompleteJsonAsync returns null (caller falls back).");
            return null;
        }

        try
        {
            var model = _configuration["Ai:OpenAiModel"] ?? "gpt-4o-mini";
            var body = new
            {
                model,
                messages = new object[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                temperature = 0.4,
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

            using var resp = await Http.SendAsync(req, cancellationToken);
            resp.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(cancellationToken));
            var content = ExtractContent(doc.RootElement);
            return string.IsNullOrWhiteSpace(content) ? null : JsonSerializer.Deserialize<T>(content, JsonOpts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI completion failed — returning null for fallback.");
            return null;
        }
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
}
