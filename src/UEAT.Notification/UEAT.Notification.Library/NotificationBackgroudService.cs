using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UEAT.Notification.Core;

namespace UEAT.Notification.Library;

public class NotificationBackgroundService(
    NotificationChannel notificationChannel,
    IServiceScopeFactory scopeFactory,
    ILogger<NotificationBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var notification in notificationChannel.Reader.ReadAllAsync(stoppingToken))
        {
            await ProcessAsync(notification, stoppingToken);
        }
    }

    private async Task ProcessAsync(INotification notification, CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var sender = scope.ServiceProvider.GetRequiredService<INotificationSender>();
            await sender.SendAsync(notification, stoppingToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex,
                "Fire-and-forget notification failed for {NotificationType}",
                notification.GetType().Name);
        }
    }
}