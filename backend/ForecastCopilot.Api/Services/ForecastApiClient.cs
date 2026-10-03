using System.Text.Json;
using ForecastCopilot.Api.Models;

namespace ForecastCopilot.Api.Services;

// Typed HttpClient for the Python forecasting service (forecasting/app.py).
// BaseAddress and Timeout are configured in Program.cs via AddHttpClient<ForecastApiClient>.
public class ForecastApiClient(HttpClient http)
{
    public async Task<ForecastResponse> GetForecastAsync(CancellationToken ct = default)
    {
        try
        {
            // GET {BaseUrl}/forecast and deserialize the JSON into our records.
            return await http.GetFromJsonAsync<ForecastResponse>("/forecast", ct)
                   ?? throw new JsonException("Empty response body.");
        }
        // Service down / non-2xx, timeout (but not the caller cancelling), or JSON not matching our records.
        catch (Exception ex) when (ex is HttpRequestException or JsonException
                                   || (ex is TaskCanceledException && !ct.IsCancellationRequested))
        {
            throw new ForecastApiUnavailableException($"Forecast API at {http.BaseAddress} failed: {ex.Message}", ex);
        }
    }
}

public class ForecastApiUnavailableException(string message, Exception inner) : Exception(message, inner);
