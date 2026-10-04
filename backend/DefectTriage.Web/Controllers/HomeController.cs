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

    // Called by the "Write summary" button: same batch (seed) + the slider's threshold -> facts -> LLM handover note.
    public async Task<IActionResult> Summary(int seed, double threshold, CancellationToken ct)
    {
        // Only the slider's values (0.30-0.95, step 0.05) -> no arbitrary input reaches the LLM.
        var percent = Math.Round(threshold * 100);
        if (Math.Abs(threshold * 100 - percent) > 1e-6 || percent % 5 != 0 || percent is < 30 or > 95)
            return BadRequest(new { error = "Threshold must be between 0.30 and 0.95 in steps of 0.05." });

        try
        {
            var batchTask = defectApi.GetBatchAsync(BatchSize, seed, ct);
            var statsTask = defectApi.GetThresholdStatsAsync(ct);
            var modelTask = defectApi.GetModelInfoAsync(ct);
            await Task.WhenAll(batchTask, statsTask, modelTask);
            var facts = SummaryFacts.From(batchTask.Result, statsTask.Result, modelTask.Result.PerClassTest, percent / 100);
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

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
