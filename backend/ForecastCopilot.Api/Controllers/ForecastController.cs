using ForecastCopilot.Api.Models;
using ForecastCopilot.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ForecastCopilot.Api.Controllers;

// JSON endpoint for the UI (Chart.js in Stage 7). Passes the Python forecast through unchanged.
[ApiController]
[Route("api/forecast")]
public class ForecastController(ForecastApiClient forecastApi, ILogger<ForecastController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ForecastResponse>> Get(CancellationToken ct)
    {
        try
        {
            return await forecastApi.GetForecastAsync(ct);
        }
        catch (ForecastApiUnavailableException ex)
        {
            // Full details go to the log; the client only gets a short ProblemDetails (no stack trace).
            logger.LogError(ex, "Forecast API call failed");
            return Problem(
                title: "Forecast service unavailable",
                detail: "The Python forecasting service could not be reached. Is it running on the configured URL?",
                statusCode: StatusCodes.Status502BadGateway);
        }
    }
}
