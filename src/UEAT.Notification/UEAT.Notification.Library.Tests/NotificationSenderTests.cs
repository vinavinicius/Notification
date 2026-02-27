using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using UEAT.Notification.Core;
using UEAT.Notification.Core.ValueObjects;
using UEAT.Notification.Library.SMS.Welcome;
using Xunit;

namespace UEAT.Notification.Library.Tests;

public class NotificationSenderTests
{
    private static WelcomeSmsNotification ValidNotification() =>
        new(CultureInfo.GetCultureInfo("en-CA"), new MobilePhone("1", "581", "5551234"))
        {
            Message = "Welcome!"
        };

    [Fact]
    public async Task SendAsync_NoChannelRegistered_ShouldThrowInvalidOperationException()
    {
        var sender = BuildSender();

        var act = async () => await sender.SendAsync(ValidNotification());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No channel registered*");
    }

    [Fact]
    public async Task SendAsync_NoRendererRegistered_ShouldThrowInvalidOperationException()
    {
        var channelMock = new Mock<IChannelNotification>();
        channelMock.Setup(x => x.CanHandle(It.IsAny<INotification>())).Returns(true);

        var sender = BuildSender(channels: [channelMock.Object]);

        var act = async () => await sender.SendAsync(ValidNotification());

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*No template renderer*");
    }

    [Fact]
    public async Task SendAsync_ValidationFails_ShouldThrowValidationException()
    {
        var channelMock = new Mock<IChannelNotification>();
        channelMock.Setup(x => x.CanHandle(It.IsAny<INotification>())).Returns(true);

        var rendererMock = new Mock<ITemplateRenderer>();
        rendererMock.Setup(x => x.CanRender(It.IsAny<INotification>())).Returns(true);
        rendererMock.Setup(x => x.RenderAsync(It.IsAny<INotification>())).ReturnsAsync("content");

        var services = new ServiceCollection();
        services.AddScoped<IValidator<WelcomeSmsNotification>>(_ =>
            new AlwaysFailingValidator());

        var sender = BuildSender(
            channels: [channelMock.Object],
            renderers: [rendererMock.Object],
            serviceProvider: services.BuildServiceProvider());

        var invalidNotification = new WelcomeSmsNotification(
            CultureInfo.GetCultureInfo("en-CA"),
            new MobilePhone("1", "581", "5551234"))
        {
            Message = string.Empty
        };

        var act = async () => await sender.SendAsync(invalidNotification);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task SendAsync_ChannelThrows_ShouldRethrowException()
    {
        var channelMock = new Mock<IChannelNotification>();
        channelMock.Setup(x => x.CanHandle(It.IsAny<INotification>())).Returns(true);
        channelMock
            .Setup(x => x.SendNotificationAsync(
                It.IsAny<INotification>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Provider unavailable"));

        var rendererMock = new Mock<ITemplateRenderer>();
        rendererMock.Setup(x => x.CanRender(It.IsAny<INotification>())).Returns(true);
        rendererMock.Setup(x => x.RenderAsync(It.IsAny<INotification>())).ReturnsAsync("content");

        var services = new ServiceCollection();
        services.AddScoped<IValidator<WelcomeSmsNotification>, WelcomeSmsNotificationValidator>();

        var sender = BuildSender(
            channels: [channelMock.Object],
            renderers: [rendererMock.Object],
            serviceProvider: services.BuildServiceProvider());

        var act = async () => await sender.SendAsync(ValidNotification());

        await act.Should().ThrowAsync<HttpRequestException>().WithMessage("*Provider unavailable*");
    }

    [Fact]
    public async Task SendAsync_HappyPath_ShouldCallChannelWithRenderedContent()
    {
        const string renderedContent = "Welcome! Your message here.";

        var channelMock = new Mock<IChannelNotification>();
        channelMock.Setup(x => x.CanHandle(It.IsAny<INotification>())).Returns(true);
        channelMock
            .Setup(x => x.SendNotificationAsync(
                It.IsAny<INotification>(),
                renderedContent,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var rendererMock = new Mock<ITemplateRenderer>();
        rendererMock.Setup(x => x.CanRender(It.IsAny<INotification>())).Returns(true);
        rendererMock.Setup(x => x.RenderAsync(It.IsAny<INotification>())).ReturnsAsync(renderedContent);

        var services = new ServiceCollection();
        services.AddScoped<IValidator<WelcomeSmsNotification>, WelcomeSmsNotificationValidator>();

        var sender = BuildSender(
            channels: [channelMock.Object],
            renderers: [rendererMock.Object],
            serviceProvider: services.BuildServiceProvider());

        await sender.SendAsync(ValidNotification());

        channelMock.Verify(
            x => x.SendNotificationAsync(
                It.IsAny<INotification>(),
                renderedContent,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private NotificationSender BuildSender(
        IEnumerable<IChannelNotification>? channels = null,
        IEnumerable<ITemplateRenderer>? renderers = null,
        IServiceProvider? serviceProvider = null)
    {
        channels ??= [];
        renderers ??= [];
        serviceProvider ??= new ServiceCollection().BuildServiceProvider();

        return new Library.NotificationSender(
            channels,
            renderers,
            serviceProvider,
            NullLogger<Library.NotificationSender>.Instance);
    }

    private class AlwaysFailingValidator : AbstractValidator<WelcomeSmsNotification>
    {
        public AlwaysFailingValidator()
        {
            RuleFor(x => x.Message).Must(_ => false).WithMessage("Always fails.");
        }
    }
}