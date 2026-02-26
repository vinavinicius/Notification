using UEAT.Notification.Core;

namespace UEAT.Notification.Infrastructure.TemplateRenderers;

public class TemplateRendererFactory(IEnumerable<ITemplateRenderer> renderers) : ITemplateRendererFactory
{
    public ITemplateRenderer Create(TemplateRendererType type)
    {
        return renderers.FirstOrDefault(r => r.RendererType == type)
               ?? throw new NotSupportedException($"Template renderer type '{type}' is not supported.");
    }
}