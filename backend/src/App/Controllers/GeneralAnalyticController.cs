using App.Services.Analtytics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Controllers;


[Authorize]
[ApiController]
[Route("api/[controller]")]
public class GeneralAnatyticController : ControllerBase
{
    private readonly IAnalyticService _analyticService;

    public GeneralAnatyticController(IAnalyticService analyticService)
    {
        _analyticService = analyticService;
    }

    [Authorize(Roles = "Admin, Moderator, Manager")]
    [HttpGet("Admin/Dashboard/Summary")]
    public async Task<IActionResult> GetTotalAnalytics()
    {
        var totalAnalytics = await _analyticService.GetGeneralAnalyticsAdminAsync();

        return Ok(totalAnalytics);
    }
}