using System.Globalization;
using System.Net;
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

namespace UEAT.Notification.Tests.SMS;

public class NoDateOrderSmsNotificationTests : IDisposable
{
    private readonly WireMockServer _server;

    public NoDateOrderSmsNotificationTests()
    {
        _server = WireMockServer.Start();
    }
    
    [Fact]
    public async Task SmsPipeline_NoDateOrderSmsNotificationInEnglish_RendersAndSendsCorrectContent()
    {
        _server
            .Given(Request.Create().WithPath("/send").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody("OK"));

        var sender = BuildSmsSender();
        var notification = new NoDateOrderSmsNotification(
            CultureInfo.GetCultureInfo("en-CA"),
            new MobilePhone("1", "581", "5551234"),
            orderNumber: 12345,
            restaurantName: "Restaurant");


        await sender.SendAsync(notification);

        var logEntry = _server.LogEntries.Should().ContainSingle().Subject;
        var body = logEntry.RequestMessage.Body!;
        var decodedBody = WebUtility.UrlDecode(body);

        decodedBody.Should()
            .Contain(
                "message=UEAT: Thank you for your order 12345 at Restaurant. Reply STOP to opt out. Messaging rates may apply.");
    }
    
    [Fact]
    public async Task SendAsync_InFrench_RendersLocalizedContent()
    {
        _server
            .Given(Request.Create().WithPath("/send").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody("OK"));

        var sender = BuildSmsSender();
        var notification = new NoDateOrderSmsNotification(
            CultureInfo.GetCultureInfo("fr-CA"),
            new MobilePhone("1", "581", "5551234"),
            orderNumber: 12345,
            restaurantName: "Restaurant");

        await sender.SendAsync(notification);

        var logEntry = _server.LogEntries.Should().ContainSingle().Subject;
        var body = logEntry.RequestMessage.Body!;
        var decodedBody = WebUtility.UrlDecode(body);

        decodedBody.Should()
            .Contain(
                "message=UEAT: Merci pour votre commande 12345 chez Restaurant. STOP pour se désabonner. Frais de msg peuvent s’appliquer.");
    }

    [Fact]
    public async Task SendAsync_InSpanish_RendersLocalizedContent()
    {
        _server
            .Given(Request.Create().WithPath("/send").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody("OK"));

        var sender = BuildSmsSender();
        var notification = new NoDateOrderSmsNotification(
            CultureInfo.GetCultureInfo("es-CA"),
            new MobilePhone("1", "581", "5551234"),
            orderNumber: 12345,
            restaurantName: "Restaurant");

        await sender.SendAsync(notification);

        var logEntry = _server.LogEntries.Should().ContainSingle().Subject;
        var body = logEntry.RequestMessage.Body!;
        var decodedBody = WebUtility.UrlDecode(body);

        decodedBody.Should()
            .Contain(
                "message=UEAT: Gracias por su pedido 12345 en Restaurant. Responda STOP para darse de baja. Cargos por msj/datos.");
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