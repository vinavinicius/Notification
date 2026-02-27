using UEAT.Notification.Core.ValueObjects;

namespace UEAT.Notification.Core.Email;

public interface IEmailNotification : INotification
{
    EmailAddress To { get; }

    string Subject { get; }
}