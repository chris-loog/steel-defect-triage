using System.Net;
using System.Text;
using System.Text.Json;
using DefectTriage.Web.Services;

namespace DefectTriage.Web.Tests;

// Unit tests for the Azure OpenAI transport: a fake handler records the request and returns a canned answer.
// No network, no real Azure call.
public class AzureOpenAiLlmClientTests
{
    private class FakeAzure(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Request = request;
            RequestBody = await request.Content!.ReadAsStringAsync(ct);
            return new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        }
    }

    private static (AzureOpenAiLlmClient Client, FakeAzure Fake) Create(HttpStatusCode status, string body)
    {
        var fake = new FakeAzure(status, body);
        var http = new HttpClient(fake) { BaseAddress = new Uri("https://test.openai.azure.com") };
        http.DefaultRequestHeaders.Add("api-key", "test-key");
        return (new AzureOpenAiLlmClient(http, "my-deployment"), fake);
    }

    private const string Answer = """{"choices":[{"message":{"role":"assistant","content":" {\"headline\":\"ok\"} "}}]}""";

    [Fact]
    public async Task CompleteAsync_ReturnsTrimmedContentOfFirstChoice()
    {
        var (client, _) = Create(HttpStatusCode.OK, Answer);

        var text = await client.CompleteAsync("system", "user");

        Assert.Equal("""{"headline":"ok"}""", text);
    }

    [Fact]
    public async Task CompleteAsync_SendsDeploymentAndStrictSchemaToV1Endpoint()
    {
        var (client, fake) = Create(HttpStatusCode.OK, Answer);
        var schema = JsonSerializer.SerializeToElement(new { type = "object" });

        await client.CompleteAsync("system", "user", schema);

        Assert.Equal("/openai/v1/chat/completions", fake.Request!.RequestUri!.AbsolutePath);
        Assert.Equal("test-key", fake.Request.Headers.GetValues("api-key").Single());
        using var json = JsonDocument.Parse(fake.RequestBody!);
        Assert.Equal("my-deployment", json.RootElement.GetProperty("model").GetString());
        var format = json.RootElement.GetProperty("response_format");
        Assert.Equal("json_schema", format.GetProperty("type").GetString());
        Assert.True(format.GetProperty("json_schema").GetProperty("strict").GetBoolean());
    }

    [Fact]
    public async Task CompleteAsync_WithoutSchema_OmitsResponseFormat()
    {
        var (client, fake) = Create(HttpStatusCode.OK, Answer);

        await client.CompleteAsync("system", "user");

        using var json = JsonDocument.Parse(fake.RequestBody!);
        Assert.False(json.RootElement.TryGetProperty("response_format", out _));
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "{}")]      // wrong key
    [InlineData(HttpStatusCode.TooManyRequests, "{}")]   // quota exceeded
    [InlineData(HttpStatusCode.OK, """{"choices":[]}""")] // empty answer
    public async Task CompleteAsync_OnFailure_ThrowsLlmUnavailable(HttpStatusCode status, string body)
    {
        var (client, _) = Create(status, body);

        await Assert.ThrowsAsync<LlmUnavailableException>(() => client.CompleteAsync("system", "user"));
    }
}
