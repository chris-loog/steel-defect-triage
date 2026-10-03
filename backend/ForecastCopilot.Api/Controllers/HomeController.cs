using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ForecastCopilot.Api.Models;
using ForecastCopilot.Api.Services;

namespace ForecastCopilot.Api.Controllers;

public class HomeController(DefectApiClient defectApi, ILogger<HomeController> logger) : Controller
{
    private const int BatchSize = 50;

    public async Task<IActionResult> Index(int? seed, CancellationToken ct)
    {
        // No seed -> pick one and redirect, so every batch has a URL that can be reloaded or shared.
        if (seed is null)
            return RedirectToAction(nameof(Index), new { seed = Random.Shared.Next(1, 100_000) });

        try
        {
            // Both calls are independent -> start both, then await them together.
            var batchTask = defectApi.GetBatchAsync(BatchSize, seed.Value, ct);
            var statsTask = defectApi.GetThresholdStatsAsync(ct);
            await Task.WhenAll(batchTask, statsTask);
            return View(new TriagePageViewModel(seed.Value, batchTask.Result, statsTask.Result, null));
        }
        catch (DefectApiUnavailableException ex)
        {
            logger.LogError(ex, "Defect API call failed");
            return View(new TriagePageViewModel(seed.Value, null, null,
                "The defect classifier service is not reachable right now. Start the Python API and reload the page."));
        }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
