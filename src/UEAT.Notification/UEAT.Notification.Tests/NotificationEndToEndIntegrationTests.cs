using System.Globalization;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RazorLight;
using SendGrid;
using UEAT.Notification.Core;
using UEAT.Notification.Core.ValueObjects;
using UEAT.Notification.Infrastructure.Configurations;
using UEAT.Notification.Infrastructure.Email.SendGrid;
using UEAT.Notification.Infrastructure.SMS.Folio;
using UEAT.Notification.Infrastructure.TemplateRenderers.Razor;
using UEAT.Notification.Library;
using UEAT.Notification.Library.SMS.NoDateOrder;
using WireMock.RequestBuilders;
using WireMock.Server;
using Response = WireMock.ResponseBuilders.Response;

namespace UEAT.Notification.Tests;

public class NotificationEndToEndIntegrationTests : IDisposable
{
    private readonly WireMockServer _server;

    public NotificationEndToEndIntegrationTests()
    {
        _server = WireMockServer.Start();
    }
    
    [Fact]
    public async Task SmsPipeline_EnglishNotification_RendersAndSendsCorrectContent()
    {
        _server
            .Given(Request.Create().WithPath("/send").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody("OK"));

        var sender = BuildSmsSender();
        var notification = new NoDateOrderNotification(
            CultureInfo.GetCultureInfo("en-CA"),
            new MobilePhone("1", "581", "5551234"),
            orderNumber: 12345,
            restaurantName: "Testaurant");


        await sender.SendAsync(notification);
        
        var logEntry = _server.LogEntries.Should().ContainSingle().Subject;
        var body = logEntry.RequestMessage.Body!;

        body.Should().Contain("message=Welcome+Hello+World");
    }

    [Fact]
    public async Task SmsPipeline_FrenchNotification_RendersLocalizedContent()
    {
        _server
            .Given(Request.Create().WithPath("/send").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody("OK"));

        var sender = BuildSmsSender();
        var notification = new NoDateOrderNotification(
            CultureInfo.GetCultureInfo("fr-CA"),
            new MobilePhone("1", "581", "5551234"),
            orderNumber: 12345,
            restaurantName: "Testaurant");

        await sender.SendAsync(notification);
        
        var logEntry = _server.LogEntries.Should().ContainSingle().Subject;
        var body = logEntry.RequestMessage.Body!;

        body.Should().Contain("message=Bienvenue+Monde");
    }

    [Fact]
    public async Task SmsPipeline_CorrectPhoneNumber_IsSentToProvider()
    {
        _server
            .Given(Request.Create().WithPath("/send").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody("OK"));

        var sender = BuildSmsSender();
        var notification = new NoDateOrderNotification(
            CultureInfo.GetCultureInfo("en-CA"),
            new MobilePhone("1", "514", "5559999"),
            orderNumber: 12345,
            restaurantName: "Testaurant");


        await sender.SendAsync(notification);
        
        var body = _server.LogEntries.Single().RequestMessage.Body!;
        body.Should().Contain("to=%2B15145559999");
    }

    [Fact]
    public async Task SmsPipeline_ValidationFails_EmptyMessage_DoesNotCallProvider()
    {
        var sender = BuildSmsSender();
        var invalidNotification = new NoDateOrderNotification(
            CultureInfo.GetCultureInfo("en-CA"),
            new MobilePhone("1", "581", "5551234"),
            orderNumber: 12345,
            restaurantName: string.Empty);

        var act = async () => await sender.SendAsync(invalidNotification);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();

        _server.LogEntries.Should().BeEmpty();
    }

    [Fact]
    public async Task SmsPipeline_ProviderReturns500_ThrowsAndDoesNotSwallow()
    {
        _server
            .Given(Request.Create().WithPath("/send").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(500).WithBody("Internal Server Error"));

        var sender = BuildSmsSender();
        var notification = new NoDateOrderNotification(
            CultureInfo.GetCultureInfo("en-CA"),
            new MobilePhone("1", "581", "5551234"),
            orderNumber: 12345,
            restaurantName: "Testaurant");

        var act = async () => await sender.SendAsync(notification);

        await act.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("*500*");
    }


    [Fact]
    public async Task Pipeline_NoChannelForNotificationType_ThrowsInvalidOperationException()
    {
        var sender = BuildSmsSender();
        var smsNotification = new NoDateOrderNotification(
            CultureInfo.GetCultureInfo("en-CA"),
            new MobilePhone("1", "581", "5551234"),
            orderNumber: 12345,
            restaurantName: "Testaurant");

        var act = async () => await sender.SendAsync(smsNotification);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No channel registered*");
    }
    
    private INotificationSender BuildSmsSender()
    {
        var razorEngine = new RazorLightEngineBuilder()
            .UseEmbeddedResourcesProject(typeof(NoDateOrderSmsNotificationValidator).Assembly)
            .UseMemoryCachingProvider()
            .Build();

        var templateRenderer = new RazorTemplateRenderer(
            razorEngine,
            [typeof(NoDateOrderSmsNotificationValidator).Assembly]);

        var httpClient = new HttpClient { BaseAddress = new Uri(_server.Url!) };
        var smsClient = new FolioSmsClient(httpClient, NullLogger<FolioSmsClient>.Instance);

        var services = new ServiceCollection();
        services.AddValidatorsForSms();
        var sp = services.BuildServiceProvider();
        var serviceScopeFactory = sp.GetRequiredService<IServiceScopeFactory>();

        var validator = new FluentValidationNotificationValidator(serviceScopeFactory);
        var channel = new SmsChannelNotification(smsClient);
        var notificationChannel = new NotificationChannel();

        return new NotificationSender(
            channels: [channel],
            templateRenderers: [templateRenderer],
            validator: validator,
            notificationChannel: notificationChannel,
            logger: NullLogger<NotificationSender>.Instance);
    }
    
    private INotificationSender BuildEmailSender()
    {
        var razorEngine = new RazorLightEngineBuilder()
            .UseEmbeddedResourcesProject(typeof(NoDateOrderSmsNotificationValidator).Assembly)
            .UseMemoryCachingProvider()
            .Build();

        var templateRenderer = new RazorTemplateRenderer(
            razorEngine,
            [typeof(NoDateOrderSmsNotificationValidator).Assembly]);

        var sendGridConfig = Options.Create(new SendGridConfigurations
        {
            ApiKey = "test-key",
            FromEmail = "noreply@example.com",
            FromName = "Test"
        });

        var sendGridClient = new SendGridClient(new SendGridClientOptions
        {
            ApiKey = "test-key",
            Host = _server.Url
        });

        var emailClient = new SendGridEmailClient(
            sendGridClient,
            sendGridConfig,
            NullLogger<SendGridEmailClient>.Instance);

        var services = new ServiceCollection();
        services.AddValidatorsForEmail();
        var sp = services.BuildServiceProvider();
        var serviceScopeFactory = sp.GetRequiredService<IServiceScopeFactory>();

        var validator = new FluentValidationNotificationValidator(serviceScopeFactory);
        var channel = new EmailChannelNotification(emailClient);
        var notificationChannel = new NotificationChannel();

        return new NotificationSender(
            channels: [channel],
            templateRenderers: [templateRenderer],
            validator: validator,
            notificationChannel: notificationChannel,
            logger: NullLogger<NotificationSender>.Instance);
    }

    public void Dispose()
    {
        _server.Stop();
        _server.Dispose();
    }
}

internal static class ServiceCollectionTestExtensions
{
    public static IServiceCollection AddValidatorsForSms(this IServiceCollection services)
    {
        services.AddScoped<
            FluentValidation.IValidator<NoDateOrderNotification>,
            NoDateOrderSmsNotificationValidator>();
        return services;
    }

    public static IServiceCollection AddValidatorsForEmail(this IServiceCollection services)
    {
        services.AddScoped<
            FluentValidation.IValidator<NoDateOrderNotification>,
            NoDateOrderSmsNotificationValidator>();
        return services;
    }
}