namespace UEAT.Notification.Core;

public interface INotificationSender
{
    Task SendAsync(INotification notification, CancellationToken cancellationToken = default);
}