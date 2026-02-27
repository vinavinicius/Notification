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
using UEAT.Notification.Library.Email.Welcome;
using UEAT.Notification.Library.SMS.Welcome;
using WireMock.RequestBuilders;
using WireMock.Server;
using Response = WireMock.ResponseBuilders.Response;

namespace UEAT.Notification.Tests;

public class NotificationPipelineIntegrationTests : IDisposable
{
    private readonly WireMockServer _server;

    public NotificationPipelineIntegrationTests()
    {
        _server = WireMockServer.Start();
    }
    
    [Fact]
    public async Task SmsPipeline_EnglishNotification_RendersAndSendsCorrectContent()
    {
        // Arrange
        _server
            .Given(Request.Create().WithPath("/send").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody("OK"));

        var sender = BuildSmsSender();
        var notification = new WelcomeSmsNotification(
            CultureInfo.GetCultureInfo("en-CA"),
            new MobilePhone("1", "581", "5551234"))
        {
            Message = "Hello World"
        };

        // Act
        await sender.SendAsync(notification);
        
        var logEntry = _server.LogEntries.Should().ContainSingle().Subject;
        var body = logEntry.RequestMessage.Body!;

        body.Should().Contain("message=Welcome+Hello+World");
    }

    [Fact]
    public async Task SmsPipeline_FrenchNotification_RendersLocalizedContent()
    {
        // Arrange
        _server
            .Given(Request.Create().WithPath("/send").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody("OK"));

        var sender = BuildSmsSender();
        var notification = new WelcomeSmsNotification(
            CultureInfo.GetCultureInfo("fr-CA"),
            new MobilePhone("1", "581", "5551234"))
        {
            Message = "Monde"
        };

        // Act
        await sender.SendAsync(notification);
        
        var logEntry = _server.LogEntries.Should().ContainSingle().Subject;
        var body = logEntry.RequestMessage.Body!;

        body.Should().Contain("message=Bienvenue+Monde");
    }

    [Fact]
    public async Task SmsPipeline_CorrectPhoneNumber_IsSentToProvider()
    {
        // Arrange
        _server
            .Given(Request.Create().WithPath("/send").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody("OK"));

        var sender = BuildSmsSender();
        var notification = new WelcomeSmsNotification(
            CultureInfo.GetCultureInfo("en-CA"),
            new MobilePhone("1", "514", "5559999"))
        {
            Message = "Test"
        };

        // Act
        await sender.SendAsync(notification);
        
        var body = _server.LogEntries.Single().RequestMessage.Body!;
        body.Should().Contain("to=%2B15145559999");
    }

    [Fact]
    public async Task SmsPipeline_ValidationFails_EmptyMessage_DoesNotCallProvider()
    {
        var sender = BuildSmsSender();
        var invalidNotification = new WelcomeSmsNotification(
            CultureInfo.GetCultureInfo("en-CA"),
            new MobilePhone("1", "581", "5551234"))
        {
            Message = string.Empty
        };

        // Act
        var act = async () => await sender.SendAsync(invalidNotification);

        // Assert
        await act.Should().ThrowAsync<FluentValidation.ValidationException>();

        _server.LogEntries.Should().BeEmpty();
    }

    [Fact]
    public async Task SmsPipeline_ProviderReturns500_ThrowsAndDoesNotSwallow()
    {
        // Arrange
        _server
            .Given(Request.Create().WithPath("/send").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(500).WithBody("Internal Server Error"));

        var sender = BuildSmsSender();
        var notification = new WelcomeSmsNotification(
            CultureInfo.GetCultureInfo("en-CA"),
            new MobilePhone("1", "581", "5551234"))
        {
            Message = "Test"
        };

        // Act
        var act = async () => await sender.SendAsync(notification);

        // Assert 
        await act.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("*500*");
    }
    
    [Fact]
    public async Task EmailPipeline_EnglishNotification_RendersAndSendsCorrectContent()
    {
        // Arrange
        _server
            .Given(Request.Create().WithPath("/v3/mail/send").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(202));

        var sender = BuildEmailSender();
        var notification = new WelcomeEmailNotification(
            CultureInfo.GetCultureInfo("en-CA"),
            new EmailAddress("user@example.com"),
            subject: "Welcome!");

        // Act
        await sender.SendAsync(notification);
        
        var body = _server.LogEntries.Should().ContainSingle().Subject.RequestMessage.Body!;

        body.Should().Contain("user@example.com");
        body.Should().Contain("Welcome!");
    }

    [Fact]
    public async Task EmailPipeline_FrenchNotification_RendersLocalizedContent()
    {
        // Arrange
        string? capturedBody = null;

        _server
            .Given(Request.Create().WithPath("/v3/mail/send").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(202));

        var sender = BuildEmailSender();
        var notification = new WelcomeEmailNotification(
            CultureInfo.GetCultureInfo("fr-CA"),
            new EmailAddress("utilisateur@exemple.com"),
            subject: "Bienvenue!");

        // Act
        await sender.SendAsync(notification);
        
        capturedBody = _server.LogEntries.Single().RequestMessage.Body!;
        capturedBody.Should().Contain("Bienvenue");
    }

    [Fact]
    public async Task EmailPipeline_ValidationFails_InvalidSubject_DoesNotCallProvider()
    {
        var sender = BuildEmailSender();
        var invalidNotification = new WelcomeEmailNotification(
            CultureInfo.GetCultureInfo("en-CA"),
            new EmailAddress("user@example.com"),
            subject: string.Empty);

        // Act
        var act = async () => await sender.SendAsync(invalidNotification);

        // Assert
        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
        _server.LogEntries.Should().BeEmpty();
    }

    [Fact]
    public async Task Pipeline_NoChannelForNotificationType_ThrowsInvalidOperationException()
    {
        var sender = BuildSmsSender();
        var emailNotification = new WelcomeEmailNotification(
            CultureInfo.GetCultureInfo("en-CA"),
            new EmailAddress("user@example.com"),
            subject: "Hello");

        // Act
        var act = async () => await sender.SendAsync(emailNotification);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No channel registered*");
    }
    
    private INotificationSender BuildSmsSender()
    {
        var razorEngine = new RazorLightEngineBuilder()
            .UseEmbeddedResourcesProject(typeof(WelcomeSmsNotification).Assembly)
            .UseMemoryCachingProvider()
            .Build();

        var templateRenderer = new RazorTemplateRenderer(
            razorEngine,
            [typeof(WelcomeSmsNotification).Assembly]);

        var httpClient = new HttpClient { BaseAddress = new Uri(_server.Url!) };
        var smsClient = new FolioSmsClient(httpClient, NullLogger<FolioSmsClient>.Instance);

        var services = new ServiceCollection();
        services.AddValidatorsForSms();
        var sp = services.BuildServiceProvider();

        var validator = new FluentValidationNotificationValidator(sp);
        var channel = new SmsChannelNotification(smsClient);

        return new NotificationSender(
            channels: [channel],
            templateRenderers: [templateRenderer],
            validator: validator,
            logger: NullLogger<NotificationSender>.Instance);
    }
    
    private INotificationSender BuildEmailSender()
    {
        var razorEngine = new RazorLightEngineBuilder()
            .UseEmbeddedResourcesProject(typeof(WelcomeEmailNotification).Assembly)
            .UseMemoryCachingProvider()
            .Build();

        var templateRenderer = new RazorTemplateRenderer(
            razorEngine,
            [typeof(WelcomeEmailNotification).Assembly]);

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

        var validator = new FluentValidationNotificationValidator(sp);
        var channel = new EmailChannelNotification(emailClient);

        return new NotificationSender(
            channels: [channel],
            templateRenderers: [templateRenderer],
            validator: validator,
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
            FluentValidation.IValidator<WelcomeSmsNotification>,
            WelcomeSmsNotificationValidator>();
        return services;
    }

    public static IServiceCollection AddValidatorsForEmail(this IServiceCollection services)
    {
        services.AddScoped<
            FluentValidation.IValidator<WelcomeEmailNotification>,
            WelcomeEmailNotificationValidator>();
        return services;
    }
}