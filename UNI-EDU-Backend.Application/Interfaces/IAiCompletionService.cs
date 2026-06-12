namespace UNI_EDU_Backend.Application.Interfaces;

// Generic AI JSON completion. Returns null when no API key is configured or the call fails,
// so callers can gracefully fall back to a deterministic result.
public interface IAiCompletionService
{
    Task<T?> CompleteJsonAsync<T>(string systemPrompt, string userPrompt, CancellationToken cancellationToken) where T : class;
}
