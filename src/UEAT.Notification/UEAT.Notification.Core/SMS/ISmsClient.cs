namespace UEAT.Notification.Core.SMS;

public interface ISmsClient
{
    Task SendAsync(SmsMessage message, CancellationToken cancellationToken = default);
}
