using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace UEAT.Notification.Library.Webhooks;

public interface IWebhookHandler
{
    public Task HandleAsync(HttpRequest request);
}