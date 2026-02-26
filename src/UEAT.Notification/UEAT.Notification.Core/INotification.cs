using System.Globalization;

namespace UEAT.Notification.Core;

public interface INotification
{
    CultureInfo CultureInfo { get; }
    TemplateRendererType TemplateRendererType { get; }
}