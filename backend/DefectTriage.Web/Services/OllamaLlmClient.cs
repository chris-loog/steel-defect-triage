using System.Text.Json;
using System.Text.Json.Serialization;

namespace DefectTriage.Web.Services;

// Typed HttpClient for a local Ollama server (POST /api/chat).
// BaseAddress and Timeout are configured in Program.cs; the model name comes from config Llm:Ollama:Model.
public class OllamaLlmClient(HttpClient http, string model) : ILlmClient
{
    public async Task<string> CompleteAsync(
        string systemPrompt, string userMessage, JsonElement? responseSchema = null, CancellationToken ct = default)
    {
        var request = new ChatRequest(
            model,
            [new ChatMessage("system", systemPrompt), new ChatMessage("user", userMessage)],
            Stream: false,                         // one JSON response instead of a token stream
            Think: false,                          // skip the model's hidden reasoning → much faster
            new ChatOptions(Temperature: 0.2),     // low temperature → factual, repeatable wording
            Format: responseSchema);               // Ollama constrains the output to this JSON Schema

        try
        {
            using var response = await http.PostAsJsonAsync("/api/chat", request, ct);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadFromJsonAsync<ChatResponse>(ct);
            var text = body?.Message?.Content?.Trim();
            return string.IsNullOrEmpty(text) ? throw new JsonException("Empty LLM answer.") : text;
        }
        // Ollama down / unknown model (404), timeout (but not the caller cancelling), or unexpected JSON.
        catch (Exception ex) when (ex is HttpRequestException or JsonException
                                   || (ex is TaskCanceledException && !ct.IsCancellationRequested))
        {
            throw new LlmUnavailableException($"Ollama at {http.BaseAddress} (model {model}) failed: {ex.Message}", ex);
        }
    }

    // Ollama chat API contract (subset): https://github.com/ollama/ollama/blob/main/docs/api.md
    private record ChatRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("messages")] IReadOnlyList<ChatMessage> Messages,
        [property: JsonPropertyName("stream")] bool Stream,
        [property: JsonPropertyName("think")] bool Think,
        [property: JsonPropertyName("options")] ChatOptions Options,
        [property: JsonPropertyName("format"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        JsonElement? Format);

    private record ChatOptions([property: JsonPropertyName("temperature")] double Temperature);

    private record ChatMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private record ChatResponse([property: JsonPropertyName("message")] ChatMessage? Message);
}
