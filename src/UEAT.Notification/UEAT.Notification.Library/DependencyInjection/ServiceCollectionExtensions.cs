using System.Reflection;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using UEAT.Notification.Core;
using UEAT.Notification.Library.SMS.Welcome;
using UEAT.Notification.Library.Webhooks;

namespace UEAT.Notification.Library.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationLibrary(
        this IServiceCollection services)
    {
        services.AddSingleton<IChannelNotification, SmsChannelNotification>();
        services.AddSingleton<IChannelNotification, EmailChannelNotification>();
        services.AddSingleton<INotificationSender, NotificationSender>();
        services.AddLocalization();
        
        services.AddValidatorsFromAssemblyContaining<WelcomeSmsNotificationValidator>();

        services.AddSingleton<WebhookHandler>();

        return services;
    }

    public static void MapNotificationWebhook(this WebApplication app)
    {
        app.MapPost("folio/webhook/incoming_sms",
            async (HttpRequest request, [FromServices] WebhookHandler webhookHandler) =>
            {
                try
                {
                    await webhookHandler.Handle(request);
                    return Results.Ok();
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message, statusCode: 500);
                }
            });
    }
}