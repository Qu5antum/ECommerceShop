using App.DTOs;
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

    [HttpGet("Notifications")]
    public async Task<IActionResult> GetNotifications(int? take = null!, bool? isRead = null)
    {
        Guid UserId = _helper.GetUserId();

        var notifications = await _service.GetNotificationsAsync(UserId, take, isRead);

        return Ok(notifications);
    }

    [HttpGet("{notificationId:guid}")]
    public async Task<IActionResult> GetNotifiction(Guid notificationId)
    {
        Guid UserId = _helper.GetUserId();

        var notification = await _service.GetUserNotificationAsync(UserId, notificationId);

        return Ok(notification);
    }

    [HttpGet("Notifications/count")]
    public async Task<IActionResult> GetCountNotifications()
    {
        Guid UserId = _helper.GetUserId();

        var notificationsCount = await _service.GetCountOfUnreadNotificationsAsync(UserId);

        return Ok(notificationsCount);
    }

    [Authorize(Roles = "Admin, Moderator, Manager")]
    [HttpPost("All")]
    public async Task<IActionResult> SendNotificationsForUsers(CreateNotificationDto notificationDto)
    {
        await _service.SendNotificationsAdminAsync(notificationDto);

        return Ok("Notifications successfully sended to users");
    }

    [Authorize(Roles = "Admin, Moderator, Manager")]
    [HttpGet("Admin/Notifications")]
    public async Task<IActionResult> GetNotificationsAdmin()
    {
        var notifications = await _service.GetAllNotificationsAdminAsync();

        return Ok(notifications);
    }

    [Authorize(Roles = "Admin, Moderator, Manager")]
    [HttpGet("{notificationId:guid}/Admin")]
    public async Task<IActionResult> GetNotificationAdmin(Guid notificationId)
    {
        var notification = await _service.GetNotificationAdminAsync(notificationId);

        return Ok(notification);
    }

    [Authorize(Roles = "Admin, Moderator, Manager")]
    [HttpDelete("{notificationId:guid}/Admin")]
    public async Task<IActionResult> DeleteNotificationAdmin(Guid notificationId)
    {
        await _service.DeleteNotificationAdminAsync(notificationId);

        return Ok("Notification successfully deleted");
    }
}