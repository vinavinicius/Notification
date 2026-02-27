using System.Globalization;
using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using UEAT.Notification.Core;
using UEAT.Notification.Core.ValueObjects;
using UEAT.Notification.Library;
using UEAT.Notification.Library.SMS.Welcome;

namespace UEAT.Notification.Tests.Library;

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

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No template renderer*");
    }

    [Fact]
    public async Task SendAsync_ValidationFails_ShouldThrowValidationException()
    {
        var channelMock = new Mock<IChannelNotification>();
        channelMock.Setup(x => x.CanHandle(It.IsAny<INotification>())).Returns(true);

        var rendererMock = new Mock<ITemplateRenderer>();
        rendererMock.Setup(x => x.CanRender(It.IsAny<INotification>())).Returns(true);
        rendererMock.Setup(x => x.RenderAsync(It.IsAny<INotification>())).ReturnsAsync("content");

        var validatorMock = new Mock<INotificationValidator>();
        validatorMock
            .Setup(x => x.ValidateAsync(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException("Always fails."));

        var sender = BuildSender(
            channels: [channelMock.Object],
            renderers: [rendererMock.Object],
            validator: validatorMock.Object);

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
    public async Task SendAsync_ValidationFails_ShouldNotCallChannel()
    {
        var channelMock = new Mock<IChannelNotification>();
        channelMock.Setup(x => x.CanHandle(It.IsAny<INotification>())).Returns(true);

        var rendererMock = new Mock<ITemplateRenderer>();
        rendererMock.Setup(x => x.CanRender(It.IsAny<INotification>())).Returns(true);
        rendererMock.Setup(x => x.RenderAsync(It.IsAny<INotification>())).ReturnsAsync("content");

        var validatorMock = new Mock<INotificationValidator>();
        validatorMock
            .Setup(x => x.ValidateAsync(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException("Always fails."));

        var sender = BuildSender(
            channels: [channelMock.Object],
            renderers: [rendererMock.Object],
            validator: validatorMock.Object);

        try { await sender.SendAsync(ValidNotification()); } catch { /* expected */ }

        channelMock.Verify(
            x => x.SendNotificationAsync(
                It.IsAny<INotification>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
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

        var validatorMock = new Mock<INotificationValidator>();
        validatorMock
            .Setup(x => x.ValidateAsync(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sender = BuildSender(
            channels: [channelMock.Object],
            renderers: [rendererMock.Object],
            validator: validatorMock.Object);

        var act = async () => await sender.SendAsync(ValidNotification());

        await act.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("*Provider unavailable*");
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

        var validatorMock = new Mock<INotificationValidator>();
        validatorMock
            .Setup(x => x.ValidateAsync(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sender = BuildSender(
            channels: [channelMock.Object],
            renderers: [rendererMock.Object],
            validator: validatorMock.Object);

        await sender.SendAsync(ValidNotification());

        channelMock.Verify(
            x => x.SendNotificationAsync(
                It.IsAny<INotification>(),
                renderedContent,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendAsync_HappyPath_ShouldCallValidatorOnce()
    {
        var channelMock = new Mock<IChannelNotification>();
        channelMock.Setup(x => x.CanHandle(It.IsAny<INotification>())).Returns(true);
        channelMock
            .Setup(x => x.SendNotificationAsync(
                It.IsAny<INotification>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var rendererMock = new Mock<ITemplateRenderer>();
        rendererMock.Setup(x => x.CanRender(It.IsAny<INotification>())).Returns(true);
        rendererMock.Setup(x => x.RenderAsync(It.IsAny<INotification>())).ReturnsAsync("content");

        var validatorMock = new Mock<INotificationValidator>();
        validatorMock
            .Setup(x => x.ValidateAsync(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var notification = ValidNotification();
        var sender = BuildSender(
            channels: [channelMock.Object],
            renderers: [rendererMock.Object],
            validator: validatorMock.Object);

        await sender.SendAsync(notification);

        validatorMock.Verify(
            x => x.ValidateAsync(notification, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private NotificationSender BuildSender(
        IEnumerable<IChannelNotification>? channels = null,
        IEnumerable<ITemplateRenderer>? renderers = null,
        INotificationValidator? validator = null)
    {
        channels ??= [];
        renderers ??= [];
        validator ??= Mock.Of<INotificationValidator>();

        return new NotificationSender(
            channels,
            renderers,
            validator,
            NullLogger<NotificationSender>.Instance);
    }
}