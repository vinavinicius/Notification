using System.Globalization;
using System.Resources;
using UEAT.Notification.Core;

namespace UEAT.Notification.Infrastructure.TemplateRenderers.Razor;

public sealed class RazorTemplateModel(INotification notification, ResourceManager resourceManager, CultureInfo culture, string templatePath)
{
    public INotification Data { get; } = notification;
    private ResourceManager ResourceManager { get; } = resourceManager;
    private CultureInfo Culture { get; } = culture;
    public string TemplatePath { get; } = templatePath;

    public string L(string key) => ResourceManager.GetString(key, Culture) ?? key;
    public string L(string key, params object[] args) => string.Format(ResourceManager.GetString(key, Culture) ?? key, args);
}