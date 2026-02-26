namespace UEAT.Notification.Core;

public interface ITemplateRendererFactory
{
    ITemplateRenderer Create(TemplateRendererType type);
}