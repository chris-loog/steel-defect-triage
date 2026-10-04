using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using DefectTriage.Web.Models;
using DefectTriage.Web.Services;

namespace DefectTriage.Web.Controllers;

public class HomeController(
    DefectApiClient defectApi, TriageSummaryService summaryService, ILogger<HomeController> logger) : Controller
{
    private const int BatchSize = 50;

    public async Task<IActionResult> Index(int? seed, CancellationToken ct)
    {
        // No seed -> pick one and redirect, so every batch has a URL that can be reloaded or shared.
        if (seed is null)
            return RedirectToAction(nameof(Index), new { seed = Random.Shared.Next(1, 100_000) });

        try
        {
            var (batch, grid) = await LoadBatchAsync(seed.Value, ct);
            return View(new TriagePageViewModel(seed.Value, batch, grid));
        }
        catch (DefectApiUnavailableException ex)
        {
            logger.LogError(ex, "Defect API call failed");
            return View("Unavailable",
                "The defect classifier service is not reachable right now. Start the Python API and reload the page.");
        }
    }

    // Called by the "Write summary" button: same batch (seed) + the slider's threshold -> facts -> LLM handover note.
    public async Task<IActionResult> Summary(int seed, double threshold, CancellationToken ct)
    {
        try
        {
            var loadTask = LoadBatchAsync(seed, ct);
            var modelTask = defectApi.GetModelInfoAsync(ct);
            await Task.WhenAll(loadTask, modelTask);
            var (batch, grid) = loadTask.Result;

            // Only thresholds on the slider's grid -> no arbitrary input reaches the LLM.
            if (grid.At(threshold) is not { } stat)
                return BadRequest(new { error = "Threshold must be one of the slider's values." });

            var facts = SummaryFacts.From(batch, grid, modelTask.Result.PerClassTest, stat.Threshold);
            return Json(await summaryService.SummarizeAsync(facts, ct));
        }
        catch (DefectApiUnavailableException ex)
        {
            logger.LogError(ex, "Defect API call failed");
            return StatusCode(503, new { error = "The defect classifier service is not reachable right now." });
        }
        catch (LlmUnavailableException ex)
        {
            logger.LogError(ex, "LLM call failed");
            return StatusCode(503, new { error = "The summary model is not reachable right now. Try again later." });
        }
    }

    // Both calls are independent -> start both, then await them together.
    private async Task<(IReadOnlyList<BatchDefect> Batch, ThresholdGrid Grid)> LoadBatchAsync(int seed, CancellationToken ct)
    {
        var batchTask = defectApi.GetBatchAsync(BatchSize, seed, ct);
        var gridTask = defectApi.GetThresholdGridAsync(ct);
        await Task.WhenAll(batchTask, gridTask);
        return (batchTask.Result, gridTask.Result);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
