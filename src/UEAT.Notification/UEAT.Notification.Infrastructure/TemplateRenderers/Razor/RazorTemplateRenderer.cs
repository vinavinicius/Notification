using System.Reflection;
using System.Resources;
using RazorLight;
using UEAT.Notification.Core;

namespace UEAT.Notification.Infrastructure.TemplateRenderers.Razor;

public class RazorTemplateRenderer: ITemplateRenderer
{
    private readonly IRazorLightEngine _razorLightEngine;
    private readonly HashSet<string> _embeddedTemplates;

    public RazorTemplateRenderer(IRazorLightEngine razorLightEngine, IEnumerable<Assembly> assemblies)
    {
        _razorLightEngine = razorLightEngine;
        
        _embeddedTemplates = assemblies
            .SelectMany(a => a.GetManifestResourceNames())
            .Where(name => name.EndsWith(".cshtml"))
            .ToHashSet(); 
    }
    
    public bool CanRender(INotification notification)
    {
        return _embeddedTemplates.Contains(notification.Template);
    }

    public async Task<string> RenderAsync(INotification notification)
    {
        var model = CreateTemplateModel(notification);
        return await _razorLightEngine.CompileRenderAsync(notification.Template, model);
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