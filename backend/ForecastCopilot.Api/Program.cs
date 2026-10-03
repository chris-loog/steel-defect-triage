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
