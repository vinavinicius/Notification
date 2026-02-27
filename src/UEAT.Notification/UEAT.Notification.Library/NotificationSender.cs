using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using UEAT.Notification.Core;

namespace UEAT.Notification.Library;

public class NotificationSender(
    IEnumerable<IChannelNotification> channels,
    IEnumerable<ITemplateRenderer> templateRenderers,
    INotificationValidator validator,
    NotificationChannel notificationChannel,
    ILogger<NotificationSender> logger)
    : INotificationSender
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        MaxDepth = 128
    };
    
    public async Task SendAsync(INotification notification, CancellationToken cancellationToken = default)
    {
        var channel = channels.FirstOrDefault(s => s.CanHandle(notification));

        if (channel is null)
        {
            throw new InvalidOperationException(
                $"No channel registered for notification type: {notification.GetType().Name}");
        }

        await validator.ValidateAsync(notification, cancellationToken);

        var content = await RenderContentAsync(notification);

        try
        {
            await channel.SendNotificationAsync(notification, content, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to send notification via {ChannelType}. Notification: {Notification}",
                channel.GetType().Name,
                SerializeNotification(notification));
            throw;
        }

        logger.LogInformation(
            "Notification sent successfully via {ChannelType}. Notification: {Notification}",
            channel.GetType().Name,
            SerializeNotification(notification));
    }

    public void Send(INotification notification)
    {
        if (!notificationChannel.Writer.TryWrite(notification))
        {
            logger.LogWarning(
                "Failed to enqueue notification {NotificationType}. Channel may be closed.",
                notification.GetType().Name);
        }
    }

    private async Task<string> RenderContentAsync(INotification notification)
    {
        var templateRenderer = templateRenderers.FirstOrDefault(s => s.CanRender(notification));

        if (templateRenderer is null)
        {
            throw new InvalidOperationException(
                $"No template renderer registered for notification type: {notification.GetType().Name}");
        }

        return await templateRenderer.RenderAsync(notification);
    }

    private static string SerializeNotification(INotification notification) =>
        JsonSerializer.Serialize(notification, JsonSerializerOptions);
}
