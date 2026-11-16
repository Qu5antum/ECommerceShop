using App.Models;
using App.Enum;


public class NotificationTestHelper
{
    public static Notification CreateTestNotification(
        Guid? Id = null,
        Guid? UserId = null,
        string Title = "TestTitle",
        string Message = "TestMessage",
        bool IsRead = false,
        NotificationType notificationType = NotificationType.General)
    {
        return new Notification
        {
            Id = Id ?? Guid.NewGuid(),
            UserId = UserId ?? Guid.NewGuid(),
            Title = Title,
            Message = Message,
            IsRead = IsRead,
            Type = notificationType
        };
    }
}