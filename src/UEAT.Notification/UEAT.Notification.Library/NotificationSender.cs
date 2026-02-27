using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UEAT.Notification.Core;

namespace UEAT.Notification.Library;

public class NotificationSender(
    IEnumerable<IChannelNotification> channels,
    IEnumerable<ITemplateRenderer> templateRenderers,
    IServiceProvider serviceProvider,
    ILogger<NotificationSender> logger)
    : INotificationSender
{
    public async Task SendAsync(INotification notification, CancellationToken cancellationToken = default)
    {
        var channel = channels.FirstOrDefault(s => s.CanHandle(notification));

        if (channel is null)
        {
            throw new InvalidOperationException(
                $"No channel registered for notification type: {notification.GetType().Name}");
        }

        await ValidateAsync(notification, cancellationToken);
        var content = await RenderContentAsync(notification);

        try
        {
            await channel.SendNotificationAsync(notification, content, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to send notification via {SenderType}. Notification: {notification}",
                channel.GetType().Name,
                JsonSerializer.Serialize(notification, new JsonSerializerOptions
                {
                    ReferenceHandler = ReferenceHandler.IgnoreCycles,
                    MaxDepth = 128
                }));

            throw;
        }

        logger.LogInformation(
            "Notification sent successfully via {SenderType}. Notification: {notification}",
            channel.GetType().Name,
            JsonSerializer.Serialize(notification, new JsonSerializerOptions
            {
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                MaxDepth = 128
            }));
    }

    private async Task ValidateAsync<T>(T notification, CancellationToken cancellationToken) where T : INotification
    {
        using var scope = serviceProvider.CreateScope();
        var provider = scope.ServiceProvider;

        var validatorType = typeof(IValidator<>)
            .MakeGenericType(notification.GetType());

        if (provider.GetService(validatorType) is not IValidator validator)
            return;

        var context = new ValidationContext<object>(notification);
        var result = await validator.ValidateAsync(context, cancellationToken);

        if (!result.IsValid)
            throw new ValidationException(result.Errors);
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
}