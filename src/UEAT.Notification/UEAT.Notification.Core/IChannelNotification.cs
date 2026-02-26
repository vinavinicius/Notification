namespace UEAT.Notification.Core;

public interface IChannelNotification
{
    bool CanHandle(INotification notification);
    Task SendNotificationAsync(INotification notification, string renderedContent, CancellationToken cancellationToken);
}