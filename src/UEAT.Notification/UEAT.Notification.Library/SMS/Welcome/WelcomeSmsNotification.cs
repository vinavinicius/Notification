using System.Globalization;
using UEAT.Notification.Core;
using UEAT.Notification.Core.SMS;

namespace UEAT.Notification.Library.SMS.Welcome;

public class WelcomeSmsNotification(CultureInfo cultureInfo, string mobilePhone)
    : ISmsNotification
{
    public CultureInfo CultureInfo { get; } = cultureInfo;
    public string MobilePhone { get; } = mobilePhone;
    public TemplateRendererType TemplateRendererType { get; } = TemplateRendererType.Razor;
    public string Message { get; init; } = string.Empty;
}
