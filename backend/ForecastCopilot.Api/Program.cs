using ForecastCopilot.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Typed HttpClient for the Python forecasting service; IHttpClientFactory manages the connections.
var forecastApiUrl = builder.Configuration["ForecastApi:BaseUrl"]
    ?? throw new InvalidOperationException("Missing config ForecastApi:BaseUrl.");
builder.Services.AddHttpClient<ForecastApiClient>(client =>
{
    client.BaseAddress = new Uri(forecastApiUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddScoped<ForecastSummaryService>();

// LLM provider behind ILlmClient. Only "ollama" for now; another provider = one more class + one more case here.
var llmProvider = builder.Configuration["Llm:Provider"] ?? "ollama";
if (llmProvider != "ollama")
    throw new InvalidOperationException($"Unknown Llm:Provider '{llmProvider}'. Supported: ollama.");
var ollamaUrl = builder.Configuration["Llm:Ollama:BaseUrl"]
    ?? throw new InvalidOperationException("Missing config Llm:Ollama:BaseUrl.");
var ollamaModel = builder.Configuration["Llm:Ollama:Model"]
    ?? throw new InvalidOperationException("Missing config Llm:Ollama:Model.");
builder.Services.AddHttpClient<ILlmClient, OllamaLlmClient>(http => new OllamaLlmClient(http, ollamaModel))
    .ConfigureHttpClient(client =>
    {
        client.BaseAddress = new Uri(ollamaUrl);
        client.Timeout = TimeSpan.FromSeconds(60); // local models are slow, especially on first load
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
