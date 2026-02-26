using UEAT.Notification.Core;
using UEAT.Notification.Core.Email;

namespace UEAT.Notification.Library;

public class EmailChannelNotification(IEmailClient emailClient) : IChannelNotification
{
    public bool CanHandle(INotification notification) => notification is IEmailNotification;
    
    public async Task SendNotificationAsync(INotification notification, string renderedContent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);
        ArgumentNullException.ThrowIfNull(renderedContent);

        if (notification is not IEmailNotification emailNotification)
        {
            throw new InvalidOperationException($"Notification type {notification.GetType().Name} is not supported.");
        }

        var emailMessage = new EmailMessage(emailNotification.To, emailNotification.Subject, renderedContent);
        await emailClient.SendAsync(emailMessage, cancellationToken);
    }
}