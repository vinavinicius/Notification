namespace UEAT.Notification.Core;

public interface ITemplateRenderer
{
    bool CanRender(INotification notification);
    Task<string> RenderAsync(INotification notification);
}
