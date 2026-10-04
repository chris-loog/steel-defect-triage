using System.Text.Json;
using System.Text.Json.Serialization;

namespace DefectTriage.Web.Services;

// Typed HttpClient for Azure OpenAI (v1 API: POST /openai/v1/chat/completions, same format as OpenAI's API).
// BaseAddress, Timeout and the "api-key" header are configured in Program.cs; deployment comes from
// config Llm:AzureOpenAI:Deployment (the name you gave the model deployment in Azure, not the model name).
public class AzureOpenAiLlmClient(HttpClient http, string deployment) : ILlmClient
{
    public async Task<string> CompleteAsync(
        string systemPrompt, string userMessage, JsonElement? responseSchema = null, CancellationToken ct = default)
    {
        var request = new ChatRequest(
            deployment,
            [new ChatMessage("system", systemPrompt), new ChatMessage("user", userMessage)],
            Temperature: 0.2,                      // low temperature → factual, repeatable wording
            // strict: the model must return exactly this JSON Schema (needs additionalProperties: false)
            responseSchema is { } schema
                ? new ResponseFormat("json_schema", new JsonSchemaFormat("response", Strict: true, schema))
                : null);

        try
        {
            using var response = await http.PostAsJsonAsync("/openai/v1/chat/completions", request, ct);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadFromJsonAsync<ChatResponse>(ct);
            var text = body?.Choices?.FirstOrDefault()?.Message?.Content?.Trim();
            return string.IsNullOrEmpty(text) ? throw new JsonException("Empty LLM answer.") : text;
        }
        // Wrong key (401), unknown deployment (404), quota (429), timeout (but not the caller cancelling), bad JSON.
        catch (Exception ex) when (ex is HttpRequestException or JsonException
                                   || (ex is TaskCanceledException && !ct.IsCancellationRequested))
        {
            throw new LlmUnavailableException(
                $"Azure OpenAI at {http.BaseAddress} (deployment {deployment}) failed: {ex.Message}", ex);
        }
    }

    // Chat Completions contract (subset): https://learn.microsoft.com/azure/ai-foundry/openai/reference
    private record ChatRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("messages")] IReadOnlyList<ChatMessage> Messages,
        [property: JsonPropertyName("temperature")] double Temperature,
        [property: JsonPropertyName("response_format"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        ResponseFormat? ResponseFormat);

    private record ResponseFormat(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("json_schema")] JsonSchemaFormat JsonSchema);

    private record JsonSchemaFormat(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("strict")] bool Strict,
        [property: JsonPropertyName("schema")] JsonElement Schema);

    private record ChatMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private record Choice([property: JsonPropertyName("message")] ChatMessage? Message);

    private record ChatResponse([property: JsonPropertyName("choices")] IReadOnlyList<Choice>? Choices);
}
