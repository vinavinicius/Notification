using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using Microsoft.Extensions.Localization;
using RazorLight;
using UEAT.Notification.Core;

namespace UEAT.Notification.Infrastructure.TemplateRenderers.Razor;

public class RazorTemplateRenderer(
    IRazorLightEngine razorLightEngine,
    IStringLocalizerFactory stringLocalizerFactory) : ITemplateRenderer
{
    private readonly ConcurrentDictionary<string, IStringLocalizer> _localizerCache = new();

    public TemplateRendererType RendererType { get; } = TemplateRendererType.Razor;

    public async Task<string> RenderAsync(INotification notification)
    {
        var model = CreateTemplateModel(notification);
        return await razorLightEngine.CompileRenderAsync(model.TemplatePath, model);
    }

    private RazorTemplateModel CreateTemplateModel(INotification notification)
    {
        var templatePath = GetTemplatePath(notification);
        var localizer = GetLocalizer(notification);

        return new RazorTemplateModel(notification, localizer, templatePath);
    }

    private static string GetTemplatePath(INotification notification)
    {
        var templateNamespace = GetTemplateNamespace(notification);
        return $"{templateNamespace}.Template.cshtml";
    }

    private IStringLocalizer GetLocalizer(INotification notification)
    {
        var templateNamespace = GetTemplateNamespace(notification);
        var template = $"{templateNamespace}.{notification.CultureInfo.TwoLetterISOLanguageName.ToUpper()}";

        return _localizerCache.GetOrAdd(template, _ =>
        {
            var name = notification.GetType().Assembly.GetName().Name;
            var localizer = stringLocalizerFactory.Create(template, name!);
            return localizer;
        });
    }

    private static string GetTemplateNamespace(INotification notification)
        => notification.GetType().Namespace!;
}