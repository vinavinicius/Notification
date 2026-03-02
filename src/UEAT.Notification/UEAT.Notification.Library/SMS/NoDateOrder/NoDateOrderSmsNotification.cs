using System.Globalization;
using UEAT.Notification.Core.SMS;
using UEAT.Notification.Core.ValueObjects;

namespace UEAT.Notification.Library.SMS.NoDateOrder;

public class NoDateOrderNotification(CultureInfo cultureInfo, MobilePhone mobilePhone, int orderNumber, string restaurantName)
    : ISmsNotification
{
    public CultureInfo CultureInfo { get; } = cultureInfo;
    public MobilePhone MobilePhone { get; } = mobilePhone;
    public int OrderNumber { get; } = orderNumber;
    public string RestaurantName { get; } = restaurantName;
    public string Template { get; } = "UEAT.Notification.Library.SMS.NoDateOrder.Template.cshtml";
}
