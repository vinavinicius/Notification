using System.Globalization;
using UEAT.Notification.Core;
using UEAT.Notification.Core.Email;
using UEAT.Notification.Core.ValueObjects;

namespace UEAT.Notification.Library.Email.Welcome;

public class WelcomeEmailNotification(CultureInfo cultureInfo, EmailAddress to, string subject)
    : IEmailNotification
{
    public CultureInfo CultureInfo { get; } = cultureInfo;
    public EmailAddress To { get; } = to;
    public string Subject { get; } = subject;
    public TemplateRendererType TemplateRendererType { get; } = TemplateRendererType.Razor;
    
    public string Template { get; } = "UEAT.Notification.Library.Email.Welcome.Template.cshtml";
}
