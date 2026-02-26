namespace UEAT.Notification.Core.Email;

public interface IEmailClient
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}