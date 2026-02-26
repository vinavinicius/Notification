using UEAT.Notification.Core.Email;

namespace UEAT.Notification.Library;

public class EmailChannelNotification(IEmailClient emailClient)
    : ChannelNotificationBase<IEmailNotification>
{
    public override async Task SendAsync(
        IEmailNotification emailNotification,
        string renderedContent,
        CancellationToken cancellationToken)
    {
        var emailMessage = new EmailMessage(emailNotification.To.Address, emailNotification.Subject, renderedContent);
        await emailClient.SendAsync(emailMessage, cancellationToken);
    }
}