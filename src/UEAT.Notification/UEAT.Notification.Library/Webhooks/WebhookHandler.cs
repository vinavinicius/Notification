using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace UEAT.Notification.Library.Webhooks;

public class WebhookHandler(ILogger<WebhookHandler> logger)
{
    public async Task Handle(HttpRequest request)
    {
        var json = await JsonSerializer.DeserializeAsync<JsonElement>(request.Body);
        logger.LogInformation("Received incoming SMS Folio Webhook: {Json}", json);
    }
}