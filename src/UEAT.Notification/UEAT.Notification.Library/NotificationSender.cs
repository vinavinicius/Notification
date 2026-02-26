using System.Text.Json;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UEAT.Notification.Core;

namespace UEAT.Notification.Library;

public sealed class NotificationSender(
    IEnumerable<IChannelNotification> channels,
    ITemplateRendererFactory templateRendererFactory,
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
                JsonSerializer.Serialize(notification));
            
            throw;
        }
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
        var templateRenderer = templateRendererFactory.Create(notification.TemplateRendererType);
        return await templateRenderer.RenderAsync(notification);
    }
}