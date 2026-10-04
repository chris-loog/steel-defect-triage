using System.Text.Json;
using DefectTriage.Web.Models;

namespace DefectTriage.Web.Services;

// Typed HttpClient for the Python defect classifier service (classifier/app.py).
// BaseAddress and Timeout are configured in Program.cs via AddHttpClient<DefectApiClient>.
public class DefectApiClient(HttpClient http)
{
    // GET /batch: random held-out test defects, classified now. Same seed -> same batch.
    public Task<IReadOnlyList<BatchDefect>> GetBatchAsync(int size, int seed, CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<BatchDefect>>($"/batch?size={size}&seed={seed}", ct);

    // GET /threshold-stats: expected error rate per threshold (test set) = the slider's threshold grid.
    public async Task<ThresholdGrid> GetThresholdGridAsync(CancellationToken ct = default) =>
        new(await GetAsync<IReadOnlyList<ThresholdStat>>("/threshold-stats", ct));

    // Training metrics (metrics.json); only the per-class test results are mapped.
    public Task<ModelInfo> GetModelInfoAsync(CancellationToken ct = default) =>
        GetAsync<ModelInfo>("/model-info", ct);

    private async Task<T> GetAsync<T>(string path, CancellationToken ct)
    {
        try
        {
            // GET {BaseUrl}{path} and deserialize the JSON into our records.
            return await http.GetFromJsonAsync<T>(path, ct)
                   ?? throw new JsonException("Empty response body.");
        }
        // Service down / non-2xx, timeout (but not the caller cancelling), or JSON not matching our records.
        catch (Exception ex) when (ex is HttpRequestException or JsonException
                                   || (ex is TaskCanceledException && !ct.IsCancellationRequested))
        {
            throw new DefectApiUnavailableException($"Defect API {path} at {http.BaseAddress} failed: {ex.Message}", ex);
        }
    }
}

public class DefectApiUnavailableException(string message, Exception inner) : Exception(message, inner);
