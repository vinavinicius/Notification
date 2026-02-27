using System.Reflection;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using RazorLight;
using SendGrid.Extensions.DependencyInjection;
using Twilio.Clients;
using Twilio.Http;
using UEAT.Notification.Core;
using UEAT.Notification.Core.Email;
using UEAT.Notification.Core.SMS;
using UEAT.Notification.Infrastructure.Configurations;
using UEAT.Notification.Infrastructure.Email.SendGrid;
using UEAT.Notification.Infrastructure.SMS.Folio;
using UEAT.Notification.Infrastructure.SMS.Twilio;
using UEAT.Notification.Infrastructure.TemplateRenderers.Razor;
using UEAT.Notification.Library.SMS.Welcome;
using UEAT.Notification.Library.Webhooks;

namespace UEAT.Notification.Library.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static NotificationLibraryServicesBuilder AddNotificationLibrary(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var builder = new NotificationLibraryServicesBuilder(services, configuration);

        services.AddSingleton<IRazorLightEngine>(_ =>
            new RazorLightEngineBuilder()
                .UseEmbeddedResourcesProject(typeof(NotificationLibraryServicesBuilder).GetTypeInfo().Assembly)
                .UseMemoryCachingProvider()
                .Build());
        
        services.AddSingleton<ITemplateRenderer, RazorTemplateRenderer>(sp =>
            new RazorTemplateRenderer(
                sp.GetRequiredService<IRazorLightEngine>(),
                [typeof(NotificationLibraryServicesBuilder).Assembly]));

        services.AddScoped<INotificationSender, NotificationSender>();
        services.AddLocalization();
        services.AddValidatorsFromAssemblyContaining<WelcomeSmsNotificationValidator>();

        return builder;
    }

    public static NotificationLibraryServicesBuilder AddFolioSmsProvider(
        this NotificationLibraryServicesBuilder builder)
    {
        builder.Services
            .AddOptions<FolioConfigurations>()
            .Bind(builder.Configuration.GetSection(nameof(FolioConfigurations)))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddHttpClient<ISmsClient, FolioSmsClient>((sp, client) =>
            {
                var config = sp.GetRequiredService<IOptions<FolioConfigurations>>().Value;
                client.BaseAddress = new Uri(config.BaseUrl);
                client.DefaultRequestHeaders.Add("FOLIOMEDIAN_API_KEY", config.ApiKey);
            })
            .AddPolicyHandler(
                HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))))
            .AddPolicyHandler(
                HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));

        builder.Services.AddScoped<IChannelNotification, SmsChannelNotification>();

        return builder;
    }

    public static NotificationLibraryServicesBuilder AddTwilioSmsProvider(
        this NotificationLibraryServicesBuilder builder)
    {
        builder.Services
            .AddOptions<TwilioConfigurations>()
            .Bind(builder.Configuration.GetSection(nameof(TwilioConfigurations)))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddHttpClient("TwilioClient")
            .AddPolicyHandler(
                HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)))
            )
            .AddPolicyHandler(
                HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30))
            );

        builder.Services.AddSingleton<ITwilioRestClient>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient("TwilioClient");
            var config = sp.GetRequiredService<IOptions<TwilioConfigurations>>().Value;
            return new TwilioRestClient(
                config.AccountName,
                config.AccountKey,
                httpClient: new SystemNetHttpClient(httpClient)
            );
        });

        builder.Services.AddScoped<ISmsClient, TwilioSmsClient>();
        builder.Services.AddScoped<IChannelNotification, SmsChannelNotification>();

        return builder;
    }

    public static NotificationLibraryServicesBuilder AddSendGridEmailProvider(
        this NotificationLibraryServicesBuilder builder)
    {
        builder.Services
            .AddOptions<SendGridConfigurations>()
            .Bind(builder.Configuration.GetSection(nameof(SendGridConfigurations)))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddSendGrid((sp, options) =>
        {
            var config = sp.GetRequiredService<IOptions<SendGridConfigurations>>().Value;
            options.ApiKey = config.ApiKey;
        });

        builder.Services.AddScoped<IEmailClient, SendGridEmailClient>();
        builder.Services.AddScoped<IChannelNotification, EmailChannelNotification>();

        return builder;
    }

    public static void MapNotificationWebhook(this WebApplication app, IWebhookHandler handler)
    {
        app.MapPost("folio/webhook/incoming_sms",
            async (HttpRequest request) =>
            {
                try
                {
                    await handler.HandleAsync(request).ConfigureAwait(false);
                    return Results.Ok();
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message, statusCode: 500);
                }
            });
    }

    public sealed class NotificationLibraryServicesBuilder
    {
        public IServiceCollection Services { get; }

        internal IConfiguration Configuration { get; }

        internal NotificationLibraryServicesBuilder(IServiceCollection services, IConfiguration configuration)
        {
            Services = services;
            Configuration = configuration;
        }
    }
}