namespace ForecastCopilot.Api.Services;

// Small provider-agnostic LLM interface. Default implementation: OllamaLlmClient (local).
public interface ILlmClient
{
    Task<string> CompleteAsync(string systemPrompt, string userMessage, CancellationToken ct = default);
}

public class LlmUnavailableException(string message, Exception? inner = null) : Exception(message, inner);
