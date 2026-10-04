using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ForecastCopilot.Api.Models;
using ForecastCopilot.Api.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace ForecastCopilot.Api.Tests;

// Integration tests: the whole app (routing, DI, controller, Razor view) runs in memory via WebApplicationFactory.
// The Python API and the LLM are replaced by fakes -> no network, no Ollama.
public class HomeControllerTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    // Answers DefectApiClient's requests with canned JSON (serialized from our own records), or fails like a dead service.
    private class FakePythonApi(bool down) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            if (down)
                throw new HttpRequestException("Connection refused");
            object body = request.RequestUri!.AbsolutePath switch
            {
                "/batch" => Batch,
                "/threshold-stats" => Stats,
                "/model-info" => new ModelInfo(new Dictionary<string, ClassMetric>
                {
                    ["Bumps"] = new(0.70, 0.75, 80), ["Pastry"] = new(0.60, 0.55, 30),
                }),
                _ => throw new InvalidOperationException($"Unexpected path {request.RequestUri}"),
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(body) });
        }
    }

    private class FakeLlm(bool down) : ILlmClient
    {
        public Task<string> CompleteAsync(
            string systemPrompt, string userMessage, JsonElement? responseSchema = null, CancellationToken ct = default) =>
            down
                ? throw new LlmUnavailableException("Ollama not running")
                : Task.FromResult(JsonSerializer.Serialize(new
                {
                    headline = "Mostly Bumps.", reviewFirst = "a", watchOut = "b", thresholdTradeOff = "c",
                }));
    }

    private static readonly BatchDefect[] Batch =
    [
        new(101, "Bumps", "Bumps", 0.91, new Dictionary<string, double> { ["Bumps"] = 0.91, ["Pastry"] = 0.09 }),
        new(102, "Pastry", "Bumps", 0.48, new Dictionary<string, double> { ["Bumps"] = 0.48, ["Pastry"] = 0.40 }),
    ];

    // Same grid as the slider: 0.30-0.95 in steps of 0.05.
    private static readonly ThresholdStat[] Stats = Enumerable.Range(6, 14)
        .Select(i => new ThresholdStat(i * 0.05, 300 - i * 10, 0.8, 0.2, 0.10))
        .ToArray();

    // Key line: ConfigureTestServices runs after Program.cs, so these registrations replace the real ones.
    private HttpClient Client(bool pythonDown = false, bool llmDown = false) =>
        factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.AddHttpClient<DefectApiClient>()
                .ConfigurePrimaryHttpMessageHandler(() => new FakePythonApi(pythonDown));
            services.AddSingleton<ILlmClient>(new FakeLlm(llmDown));
        })).CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    [Fact]
    public async Task Index_WithoutSeed_RedirectsToShareableSeedUrl()
    {
        var response = await Client().GetAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Matches(@"\?seed=\d+$", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Index_RendersBatch()
    {
        var response = await Client().GetAsync("/?seed=1");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Bumps", html);
        Assert.DoesNotContain("not reachable", html);
    }

    [Fact]
    public async Task Index_PythonDown_StillRendersWithMessage()
    {
        var response = await Client(pythonDown: true).GetAsync("/?seed=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("defect classifier service is not reachable", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("0.72")] // off the 0.05 grid
    [InlineData("0.25")] // below the slider range
    [InlineData("1")]    // above the slider range
    public async Task Summary_ThresholdOffSlider_Returns400(string threshold)
    {
        var response = await Client().GetAsync($"/Home/Summary?seed=1&threshold={threshold}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Summary_Valid_ReturnsHandoverNoteJson()
    {
        var response = await Client().GetAsync("/Home/Summary?seed=1&threshold=0.7");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Mostly Bumps.", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task Summary_DependencyDown_Returns503(bool pythonDown, bool llmDown)
    {
        var response = await Client(pythonDown, llmDown).GetAsync("/Home/Summary?seed=1&threshold=0.7");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }
}
