using System.Diagnostics;
using UEAT.Notification.Core;

namespace UEAT.Notification.Library;

public abstract class ChannelNotificationBase<TNotification> : IChannelNotification<TNotification>
    where TNotification : INotification
{
    public bool CanHandle(INotification notification) => notification is TNotification;

    public async Task SendNotificationAsync(
        INotification notification,
        string renderedContent,
        CancellationToken cancellationToken)
    {
        if (notification is not TNotification typed)
        {
            throw new InvalidOperationException(
                $"Cannot handle notification of type '{notification.GetType().Name}'. " +
                $"Expected '{typeof(TNotification).Name}'. " +
                $"Always call CanHandle before SendNotificationAsync.");
        }

        await SendAsync(typed, renderedContent, cancellationToken);
    }

    public abstract Task SendAsync(
        TNotification notification,
        string renderedContent,
        CancellationToken cancellationToken);
}