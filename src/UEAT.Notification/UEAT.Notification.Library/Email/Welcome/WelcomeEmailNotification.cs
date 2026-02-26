using System.Globalization;
using UEAT.Notification.Core;
using UEAT.Notification.Core.Email;

namespace UEAT.Notification.Library.Email.Welcome;

public class WelcomeEmailNotification(CultureInfo cultureInfo, string to, string subject)
    : IEmailNotification
{
    public CultureInfo CultureInfo { get; } = cultureInfo;
    public string To { get; } = to;
    public string Subject { get; } = subject;
    public TemplateRendererType TemplateRendererType { get; } = TemplateRendererType.Razor;
}
