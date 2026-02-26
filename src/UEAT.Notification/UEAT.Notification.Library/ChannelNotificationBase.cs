using UEAT.Notification.Core;

namespace UEAT.Notification.Library;

public abstract class ChannelNotificationBase<TNotification> : IChannelNotification<TNotification>
    where TNotification : INotification
{
    public bool CanHandle(INotification notification) => notification is TNotification;

    public Task SendNotificationAsync(
        INotification notification, 
        string renderedContent, 
        CancellationToken cancellationToken)
    {
        return SendAsync((TNotification)notification, renderedContent, cancellationToken);
    }

    public abstract Task SendAsync(
        TNotification notification, 
        string renderedContent, 
        CancellationToken cancellationToken);
}