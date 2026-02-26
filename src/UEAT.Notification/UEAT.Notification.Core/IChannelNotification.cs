namespace UEAT.Notification.Core;

public interface IChannelNotification
{
    bool CanHandle(INotification notification);
    Task SendNotificationAsync(INotification notification, string renderedContent, CancellationToken cancellationToken);
}

public interface IChannelNotification<in TNotification> : IChannelNotification
    where TNotification : INotification
{
    Task SendAsync(TNotification notification, string renderedContent, CancellationToken cancellationToken);
}