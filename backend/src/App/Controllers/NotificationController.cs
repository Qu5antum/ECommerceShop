using App.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Controllers;


[Authorize]
[ApiController]
[Route("api/[controller]")]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _service;
    private readonly Helper _helper;

    public NotificationController(INotificationService service, Helper helper)
    {
        _service = service;
        _helper = helper;
    }

    [Authorize]
    [HttpGet("Notifications")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetNotifications(int? take = null!, bool? isRead = null)
    {
        Guid UserId = _helper.GetUserId();

        var notifications = await _service.GetNotificationsAsync(UserId, take, isRead);

        return Ok(notifications);
    }

    [Authorize]
    [HttpGet("Notifications/count")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetCountNotifications()
    {
        Guid UserId = _helper.GetUserId();

        var notificationsCount = await _service.GetCountOfUnreadNotificationsAsync(UserId);

        return Ok(notificationsCount);
    }
}