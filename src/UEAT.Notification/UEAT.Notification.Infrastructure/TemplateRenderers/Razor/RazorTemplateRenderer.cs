using System.Resources;
using RazorLight;
using UEAT.Notification.Core;

namespace UEAT.Notification.Infrastructure.TemplateRenderers.Razor;

public class RazorTemplateRenderer(IRazorLightEngine razorLightEngine) : ITemplateRenderer
{
    public TemplateRendererType RendererType => TemplateRendererType.Razor;

    public async Task<string> RenderAsync(INotification notification)
    {
        var model = CreateTemplateModel(notification);
        return await razorLightEngine.CompileRenderAsync(notification.Template, model);
    }

    private static RazorTemplateModel CreateTemplateModel(INotification notification)
    {
        var resourceManager = new ResourceManager(
            notification.GetType().FullName!,
            notification.GetType().Assembly
        );

        return new RazorTemplateModel(notification, resourceManager, notification.CultureInfo);
    }
}