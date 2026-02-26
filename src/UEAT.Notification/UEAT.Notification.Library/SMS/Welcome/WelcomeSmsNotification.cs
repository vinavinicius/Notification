using System.Globalization;
using UEAT.Notification.Core;
using UEAT.Notification.Core.SMS;
using UEAT.Notification.Core.ValueObjects;

namespace UEAT.Notification.Library.SMS.Welcome;

public class WelcomeSmsNotification(CultureInfo cultureInfo, MobilePhone mobilePhone)
    : ISmsNotification
{
    public CultureInfo CultureInfo { get; } = cultureInfo;
    public MobilePhone MobilePhone { get; } = mobilePhone;
    public TemplateRendererType TemplateRendererType { get; } = TemplateRendererType.Razor;
    public string Template { get; } = "UEAT.Notification.Library.SMS.Welcome.Template.cshtml";
    public string Message { get; init; } = string.Empty;
}
