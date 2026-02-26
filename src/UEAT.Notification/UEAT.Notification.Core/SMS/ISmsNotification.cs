using UEAT.Notification.Core.ValueObjects;

namespace UEAT.Notification.Core.SMS;

public interface ISmsNotification : INotification
{
    MobilePhone MobilePhone { get; }
}