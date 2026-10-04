using DefectTriage.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Typed HttpClient for the Python defect classifier service; IHttpClientFactory manages the connections.
var classifierApiUrl = builder.Configuration["ClassifierApi:BaseUrl"]
    ?? throw new InvalidOperationException("Missing config ClassifierApi:BaseUrl.");
builder.Services.AddHttpClient<DefectApiClient>(client =>
{
    client.BaseAddress = new Uri(classifierApiUrl);
    client.Timeout = TimeSpan.FromSeconds(30); // Azure Container Apps: scale-to-zero cold start takes a while
});

// LLM provider behind ILlmClient: local Ollama (default) or Azure OpenAI (cloud deployment).
string RequiredConfig(string key) =>
    builder.Configuration[key] ?? throw new InvalidOperationException($"Missing config {key}.");

var llmProvider = builder.Configuration["Llm:Provider"] ?? "ollama";
switch (llmProvider)
{
    case "ollama":
        // Read config here, not inside the lambda (that runs lazily) → missing keys fail at startup.
        var ollamaUrl = RequiredConfig("Llm:Ollama:BaseUrl");
        var ollamaModel = RequiredConfig("Llm:Ollama:Model");
        builder.Services.AddHttpClient<ILlmClient, OllamaLlmClient>(http => new OllamaLlmClient(http, ollamaModel))
            .ConfigureHttpClient(client =>
            {
                client.BaseAddress = new Uri(ollamaUrl);
                client.Timeout = TimeSpan.FromSeconds(60); // local models are slow, especially on first load
            });
        break;
    case "azureopenai":
        var azureEndpoint = RequiredConfig("Llm:AzureOpenAI:Endpoint");
        var deployment = RequiredConfig("Llm:AzureOpenAI:Deployment");
        // Key from env var / Container App secret, never from appsettings.json (that file is committed).
        var apiKey = RequiredConfig("Llm:AzureOpenAI:ApiKey");
        builder.Services.AddHttpClient<ILlmClient, AzureOpenAiLlmClient>(http => new AzureOpenAiLlmClient(http, deployment))
            .ConfigureHttpClient(client =>
            {
                client.BaseAddress = new Uri(azureEndpoint);
                client.DefaultRequestHeaders.Add("api-key", apiKey);
                client.Timeout = TimeSpan.FromSeconds(30);
            });
        break;
    default:
        throw new InvalidOperationException($"Unknown Llm:Provider '{llmProvider}'. Supported: ollama, azureopenai.");
}
builder.Services.AddScoped<TriageSummaryService>();

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
