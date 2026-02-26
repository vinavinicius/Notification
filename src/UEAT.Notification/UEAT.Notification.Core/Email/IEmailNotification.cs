namespace UEAT.Notification.Core.Email;

public interface IEmailNotification : INotification
{
    string To { get; }
    string Subject { get; }
}