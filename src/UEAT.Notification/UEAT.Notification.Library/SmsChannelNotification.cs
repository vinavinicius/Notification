using UEAT.Notification.Core;
using UEAT.Notification.Core.SMS;

namespace UEAT.Notification.Library;

public class SmsChannelNotification(ISmsClient smsClient) : IChannelNotification
{
    public bool CanHandle(INotification notification) => notification is ISmsNotification;
    
    public async Task SendNotificationAsync(INotification notification, string renderedContent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        ArgumentNullException.ThrowIfNull(renderedContent);

        if (notification is not ISmsNotification smsNotification)
        {
            throw new InvalidOperationException($"Notification type {notification.GetType().Name} is not supported.");
        }

        var smsMessage = new SmsMessage(smsNotification.MobilePhone, renderedContent);
        await smsClient.SendAsync(smsMessage, cancellationToken);
    }
}