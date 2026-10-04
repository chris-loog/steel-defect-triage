using System.Text.Json;

namespace DefectTriage.Web.Services;

// Small provider-agnostic LLM interface. Default implementation: OllamaLlmClient (local).
public interface ILlmClient
{
    // responseSchema (JSON Schema): force the answer into this JSON shape ("structured output"). Null = plain text.
    Task<string> CompleteAsync(
        string systemPrompt, string userMessage, JsonElement? responseSchema = null, CancellationToken ct = default);
}

public class LlmUnavailableException(string message, Exception? inner = null) : Exception(message, inner);
