using UEAT.Notification.Core.SMS;

namespace UEAT.Notification.Library;

public class SmsChannelNotification(ISmsClient smsClient)
    : ChannelNotificationBase<ISmsNotification>
{
    public override async Task SendAsync(
        ISmsNotification notification,
        string renderedContent,
        CancellationToken cancellationToken)
    {
        var smsMessage = new SmsMessage(notification.MobilePhone, renderedContent);
        await smsClient.SendAsync(smsMessage, cancellationToken);
    }
}