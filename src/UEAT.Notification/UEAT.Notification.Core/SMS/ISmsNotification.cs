namespace UEAT.Notification.Core.SMS;

public interface ISmsNotification : INotification
{
    string MobilePhone { get; }
}