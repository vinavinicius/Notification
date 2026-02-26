using System.Diagnostics.CodeAnalysis;
using System.Reflection;
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
using UEAT.Notification.Infrastructure.TemplateRenderers;
using UEAT.Notification.Infrastructure.TemplateRenderers.Razor;

namespace UEAT.Notification.Infrastructure.DependencyInjection;

[ExcludeFromCodeCoverage]
public static class InfrastructureConfigurationExtensions
{
    public static NotificationInfrastructureServicesBuilder AddNotificationInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        Assembly templateAssembly)
    {
        var builder = new NotificationInfrastructureServicesBuilder(services);

        builder
            .AddConfigurationOptions(configuration)
            .AddTemplateRenderers(templateAssembly);

        return builder;
    }

    private static NotificationInfrastructureServicesBuilder AddTemplateRenderers(
        this NotificationInfrastructureServicesBuilder builder,
        Assembly templateAssembly)
    {
        builder.Services
            .AddSingleton<IRazorLightEngine>(_ =>
                new RazorLightEngineBuilder()
                    .UseEmbeddedResourcesProject(templateAssembly)
                    .UseMemoryCachingProvider()
                    .Build()
            );

        builder.Services.AddSingleton<ITemplateRendererFactory, TemplateRendererFactory>();
        builder.Services.AddSingleton<ITemplateRenderer, RazorTemplateRenderer>();
        
        return builder;
    }

    private static NotificationInfrastructureServicesBuilder AddConfigurationOptions(
        this NotificationInfrastructureServicesBuilder builder,
        IConfiguration configuration)
    {
        builder.Services
            .AddOptions<TwilioConfigurations>()
            .Bind(configuration.GetSection(nameof(TwilioConfigurations)))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddSingleton(sp =>
            sp.GetRequiredService<IOptions<TwilioConfigurations>>().Value);

        builder.Services
            .AddOptions<FolioConfigurations>()
            .Bind(configuration.GetSection(nameof(FolioConfigurations)))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddSingleton(sp =>
            sp.GetRequiredService<IOptions<FolioConfigurations>>().Value);

        builder.Services
            .AddOptions<SendGridConfigurations>()
            .Bind(configuration.GetSection(nameof(SendGridConfigurations)))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddSingleton(sp =>
            sp.GetRequiredService<IOptions<SendGridConfigurations>>().Value);

        return builder;
    }

    public static NotificationInfrastructureServicesBuilder AddTwilioSmsProvider(
        this NotificationInfrastructureServicesBuilder builder)
    {
        builder.Services.AddHttpClient("TwilioClient")
            .AddPolicyHandler(
                HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .WaitAndRetryAsync(
                        3,
                        retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                    )
            )
            .AddPolicyHandler(
                HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .CircuitBreakerAsync(
                        5,
                        TimeSpan.FromSeconds(30)
                    )
            );

        builder.Services.AddSingleton<ITwilioRestClient>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();

            var httpClient = httpClientFactory.CreateClient("TwilioClient");
            var twilioConfiguration = sp.GetRequiredService<IOptions<TwilioConfigurations>>().Value;

            return new TwilioRestClient(
                twilioConfiguration.AccountName,
                twilioConfiguration.AccountKey,
                httpClient: new SystemNetHttpClient(httpClient)
            );
        });

        builder.Services.AddSingleton<ISmsClient, TwilioSmsClient>();

        return builder;
    }

    public static NotificationInfrastructureServicesBuilder AddFolioSmsProvider(
        this NotificationInfrastructureServicesBuilder builder)
    {
        builder.Services.AddHttpClient<ISmsClient, FolioSmsClient>((sp, client) =>
            {
                var folioConfiguration = sp.GetRequiredService<IOptions<FolioConfigurations>>().Value;
                client.BaseAddress = new Uri(folioConfiguration.BaseUrl);
                client.DefaultRequestHeaders.Add("FOLIOMEDIAN_API_KEY", folioConfiguration.ApiKey);
            })
            .AddPolicyHandler(
                HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .WaitAndRetryAsync(
                        3,
                        retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                    )
            )
            .AddPolicyHandler(
                HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .CircuitBreakerAsync(
                        5,
                        TimeSpan.FromSeconds(30)
                    )
            );

        return builder;
    }

    public static NotificationInfrastructureServicesBuilder AddSendGridEmailProvider(
        this NotificationInfrastructureServicesBuilder builder)
    {
        builder.Services.AddSendGrid((services, options) =>
        {
            var sendGridConfiguration = services.GetRequiredService<IOptions<SendGridConfigurations>>().Value;
            options.ApiKey = sendGridConfiguration.ApiKey;
        });

        builder.Services.AddSingleton<IEmailClient, SendGridEmailClient>();

        return builder;
    }

    public sealed class NotificationInfrastructureServicesBuilder
    {
        public IServiceCollection Services { get; }

        internal NotificationInfrastructureServicesBuilder(IServiceCollection services)
        {
            Services = services;
        }
    }
}