namespace UEAT.Notification.Core;

public interface ITemplateRenderer
{
    TemplateRendererType RendererType { get; }
    Task<string> RenderAsync(INotification notification);
}
