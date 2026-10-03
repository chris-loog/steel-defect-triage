using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ForecastCopilot.Api.Models;
using ForecastCopilot.Api.Services;

namespace ForecastCopilot.Api.Controllers;

public class HomeController(ForecastApiClient forecastApi, ILogger<HomeController> logger) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        try
        {
            var forecast = await forecastApi.GetForecastAsync(ct);
            return View(new ForecastPageViewModel(forecast, null));
        }
        catch (ForecastApiUnavailableException ex)
        {
            logger.LogError(ex, "Forecast API call failed");
            return View(new ForecastPageViewModel(null,
                "The forecasting service is not reachable right now. Start the Python API and reload the page."));
        }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
