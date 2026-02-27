using UEAT.Notification.Core;

namespace UEAT.Notification.Library;

public interface INotificationValidator
{
    Task ValidateAsync(INotification notification, CancellationToken ct);
}