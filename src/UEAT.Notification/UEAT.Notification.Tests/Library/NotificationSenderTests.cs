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
    private readonly Mock<IChannelNotification> _channelMock = new();
    private readonly Mock<ITemplateRenderer> _rendererMock = new();
    private readonly Mock<INotificationValidator> _validatorMock = new();
    private readonly NotificationChannel _notificationChannel = new();

    private static WelcomeSmsNotification ValidNotification() =>
        new(CultureInfo.GetCultureInfo("en-CA"), new MobilePhone("1", "581", "5551234"))
        {
            Message = "Welcome!"
        };

    private NotificationSender BuildSender(
        IEnumerable<IChannelNotification>? channels = null,
        IEnumerable<ITemplateRenderer>? renderers = null,
        INotificationValidator? validator = null) =>
        new(
            channels ?? [],
            renderers ?? [],
            validator ?? Mock.Of<INotificationValidator>(),
            _notificationChannel,
            NullLogger<NotificationSender>.Instance);

    [Fact]
    public async Task SendAsync_NoChannelRegistered_ThrowsInvalidOperationException()
    {
        var sender = BuildSender();

        var act = () => sender.SendAsync(ValidNotification());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No channel registered*");
    }

    [Fact]
    public async Task SendAsync_ChannelCannotHandleNotification_ThrowsInvalidOperationException()
    {
        _channelMock.Setup(x => x.CanHandle(It.IsAny<INotification>())).Returns(false);

        var sender = BuildSender(channels: [_channelMock.Object]);

        var act = () => sender.SendAsync(ValidNotification());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No channel registered*");
    }
    
    [Fact]
    public async Task SendAsync_NoRendererRegistered_ThrowsInvalidOperationException()
    {
        _channelMock.Setup(x => x.CanHandle(It.IsAny<INotification>())).Returns(true);

        var sender = BuildSender(channels: [_channelMock.Object]);

        var act = () => sender.SendAsync(ValidNotification());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No template renderer*");
    }

    [Fact]
    public async Task SendAsync_RendererCannotRender_ThrowsInvalidOperationException()
    {
        _channelMock.Setup(x => x.CanHandle(It.IsAny<INotification>())).Returns(true);
        _rendererMock.Setup(x => x.CanRender(It.IsAny<INotification>())).Returns(false);

        var sender = BuildSender(channels: [_channelMock.Object], renderers: [_rendererMock.Object]);

        var act = () => sender.SendAsync(ValidNotification());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No template renderer*");
    }
    
    [Fact]
    public async Task SendAsync_ValidationFails_ThrowsValidationException()
    {
        SetupChannelAndRenderer("content");
        _validatorMock
            .Setup(x => x.ValidateAsync(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException("Validation failed."));

        var sender = BuildSender(
            channels: [_channelMock.Object],
            renderers: [_rendererMock.Object],
            validator: _validatorMock.Object);

        var act = () => sender.SendAsync(ValidNotification());

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task SendAsync_ValidationFails_DoesNotCallChannel()
    {
        SetupChannelAndRenderer("content");
        _validatorMock
            .Setup(x => x.ValidateAsync(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException("Validation failed."));

        var sender = BuildSender(
            channels: [_channelMock.Object],
            renderers: [_rendererMock.Object],
            validator: _validatorMock.Object);

        try { await sender.SendAsync(ValidNotification()); } catch { /* expected */ }

        _channelMock.Verify(
            x => x.SendNotificationAsync(
                It.IsAny<INotification>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SendAsync_HappyPath_ValidatorCalledExactlyOnce()
    {
        var notification = ValidNotification();
        SetupChannelAndRenderer("content");
        _validatorMock
            .Setup(x => x.ValidateAsync(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sender = BuildSender(
            channels: [_channelMock.Object],
            renderers: [_rendererMock.Object],
            validator: _validatorMock.Object);

        await sender.SendAsync(notification);

        _validatorMock.Verify(
            x => x.ValidateAsync(notification, It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    [Fact]
    public async Task SendAsync_HappyPath_DoesNotThrow()
    {
        SetupChannelAndRenderer("content");

        var sender = BuildSender(
            channels: [_channelMock.Object],
            renderers: [_rendererMock.Object]);

        var act = () => sender.SendAsync(ValidNotification());

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendAsync_HappyPath_ChannelReceivesRenderedContent()
    {
        const string renderedContent = "Welcome! Your message here.";
        SetupChannelAndRenderer(renderedContent);

        var sender = BuildSender(
            channels: [_channelMock.Object],
            renderers: [_rendererMock.Object]);

        await sender.SendAsync(ValidNotification());

        _channelMock.Verify(
            x => x.SendNotificationAsync(
                It.IsAny<INotification>(),
                renderedContent,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendAsync_HappyPath_PassesCorrectNotificationToChannel()
    {
        var notification = ValidNotification();
        SetupChannelAndRenderer("content");

        var sender = BuildSender(
            channels: [_channelMock.Object],
            renderers: [_rendererMock.Object]);

        await sender.SendAsync(notification);

        _channelMock.Verify(
            x => x.SendNotificationAsync(
                notification,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    [Fact]
    public async Task SendAsync_ChannelThrows_ExceptionIsPropagated()
    {
        _channelMock.Setup(x => x.CanHandle(It.IsAny<INotification>())).Returns(true);
        _channelMock
            .Setup(x => x.SendNotificationAsync(
                It.IsAny<INotification>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Provider unavailable"));

        _rendererMock.Setup(x => x.CanRender(It.IsAny<INotification>())).Returns(true);
        _rendererMock.Setup(x => x.RenderAsync(It.IsAny<INotification>())).ReturnsAsync("content");

        var sender = BuildSender(
            channels: [_channelMock.Object],
            renderers: [_rendererMock.Object]);

        var act = () => sender.SendAsync(ValidNotification());

        await act.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("*Provider unavailable*");
    }

    [Fact]
    public async Task SendAsync_RendererThrows_ExceptionIsPropagated()
    {
        _channelMock.Setup(x => x.CanHandle(It.IsAny<INotification>())).Returns(true);
        _rendererMock.Setup(x => x.CanRender(It.IsAny<INotification>())).Returns(true);
        _rendererMock
            .Setup(x => x.RenderAsync(It.IsAny<INotification>()))
            .ThrowsAsync(new InvalidOperationException("Template compilation failed"));

        var sender = BuildSender(
            channels: [_channelMock.Object],
            renderers: [_rendererMock.Object]);

        var act = () => sender.SendAsync(ValidNotification());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Template compilation failed*");
    }

    [Fact]
    public async Task SendAsync_ChannelThrows_ChannelIsCalledOnlyOnce()
    {
        _channelMock.Setup(x => x.CanHandle(It.IsAny<INotification>())).Returns(true);
        _channelMock
            .Setup(x => x.SendNotificationAsync(
                It.IsAny<INotification>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Provider unavailable"));

        _rendererMock.Setup(x => x.CanRender(It.IsAny<INotification>())).Returns(true);
        _rendererMock.Setup(x => x.RenderAsync(It.IsAny<INotification>())).ReturnsAsync("content");

        var sender = BuildSender(
            channels: [_channelMock.Object],
            renderers: [_rendererMock.Object]);

        try { await sender.SendAsync(ValidNotification()); } catch { /* expected */ }

        _channelMock.Verify(
            x => x.SendNotificationAsync(
                It.IsAny<INotification>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        _channelMock.Setup(x => x.CanHandle(It.IsAny<INotification>())).Returns(true);
        _channelMock
            .Setup(x => x.SendNotificationAsync(
                It.IsAny<INotification>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns((INotification _, string _, CancellationToken ct) =>
            {
                ct.ThrowIfCancellationRequested();
                return Task.CompletedTask;
            });

        _rendererMock.Setup(x => x.CanRender(It.IsAny<INotification>())).Returns(true);
        _rendererMock.Setup(x => x.RenderAsync(It.IsAny<INotification>())).ReturnsAsync("content");

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var sender = BuildSender(
            channels: [_channelMock.Object],
            renderers: [_rendererMock.Object]);

        var act = () => sender.SendAsync(ValidNotification(), cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
    
    [Fact]
    public async Task SendAsync_MultipleChannels_UsesFirstMatchingChannel()
    {
        var nonMatchingChannel = new Mock<IChannelNotification>();
        nonMatchingChannel.Setup(x => x.CanHandle(It.IsAny<INotification>())).Returns(false);

        SetupChannelAndRenderer("content");

        var sender = BuildSender(
            channels: [nonMatchingChannel.Object, _channelMock.Object],
            renderers: [_rendererMock.Object]);

        await sender.SendAsync(ValidNotification());

        nonMatchingChannel.Verify(
            x => x.SendNotificationAsync(
                It.IsAny<INotification>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _channelMock.Verify(
            x => x.SendNotificationAsync(
                It.IsAny<INotification>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private void SetupChannelAndRenderer(string renderedContent)
    {
        _channelMock.Setup(x => x.CanHandle(It.IsAny<INotification>())).Returns(true);
        _channelMock
            .Setup(x => x.SendNotificationAsync(
                It.IsAny<INotification>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _rendererMock.Setup(x => x.CanRender(It.IsAny<INotification>())).Returns(true);
        _rendererMock.Setup(x => x.RenderAsync(It.IsAny<INotification>())).ReturnsAsync(renderedContent);
    }
}