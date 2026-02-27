using Microsoft.AspNetCore.Http;

namespace UEAT.Notification.Library.Webhooks;

public interface IWebhookHandler
{
    public Task HandleAsync(HttpRequest request);
}