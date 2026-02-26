using Microsoft.Extensions.Localization;
using UEAT.Notification.Core;

namespace UEAT.Notification.Infrastructure.TemplateRenderers.Razor;

public sealed class RazorTemplateModel(INotification notification, IStringLocalizer stringLocalizer, string templatePath)
{
    public INotification Data { get; } = notification;
    public IStringLocalizer Localizer { get; } = stringLocalizer;
    public string TemplatePath { get; } = templatePath;
}
