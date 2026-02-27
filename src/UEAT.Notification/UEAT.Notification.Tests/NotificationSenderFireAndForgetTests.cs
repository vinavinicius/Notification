using System.Globalization;
using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using UEAT.Notification.Core;
using UEAT.Notification.Core.ValueObjects;
using UEAT.Notification.Library;
using UEAT.Notification.Library.SMS.Welcome;

namespace UEAT.Notification.Tests;

public class NotificationSenderFireAndForgetTests
{
    private readonly NotificationChannel _notificationChannel = new();

    private static WelcomeSmsNotification ValidNotification() =>
        new(CultureInfo.GetCultureInfo("en-CA"), new MobilePhone("1", "581", "5551234"))
        {
            Message = "Welcome!"
        };

    private NotificationSender BuildSender(
        IEnumerable<IChannelNotification>? channels = null,
        IEnumerable<ITemplateRenderer>? renderers = null,
        INotificationValidator? validator = null,
        NotificationChannel? notificationChannel = null) =>
        new(
            channels ?? [],
            renderers ?? [],
            validator ?? Mock.Of<INotificationValidator>(),
            notificationChannel ?? _notificationChannel,
            NullLogger<NotificationSender>.Instance);
    
    [Fact]
    public void Send_WritesNotificationToChannel()
    {
        var sender = BuildSender();
        var notification = ValidNotification();

        sender.Send(notification);

        _notificationChannel.Reader.TryRead(out var queued).Should().BeTrue();
        queued.Should().BeSameAs(notification);
    }

    [Fact]
    public void Send_MultipleNotifications_AllEnqueued()
    {
        var sender = BuildSender();
        var n1 = ValidNotification();
        var n2 = ValidNotification();
        var n3 = ValidNotification();

        sender.Send(n1);
        sender.Send(n2);
        sender.Send(n3);

        _notificationChannel.Reader.Count.Should().Be(3);
    }

    [Fact]
    public void Send_DoesNotThrow_WhenChannelIsOpen()
    {
        var sender = BuildSender();

        var act = () => sender.Send(ValidNotification());

        act.Should().NotThrow();
    }

    [Fact]
    public void Send_ReturnsImmediately_WithoutAwaiting()
    {
        var sender = BuildSender();

        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        sender.Send(ValidNotification());
        elapsed.Stop();

        elapsed.ElapsedMilliseconds.Should().BeLessThan(50);
    }

    [Fact]
    public async Task BackgroundService_ConsumesNotification_CallsSendAsync()
    {
        var senderMock = new Mock<INotificationSender>();
        senderMock
            .Setup(x => x.SendAsync(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var services = new ServiceCollection();
        services.AddSingleton(senderMock.Object);
        var sp = services.BuildServiceProvider();

        var channel = new NotificationChannel();
        var worker = new NotificationBackgroundService(
            channel,
            sp.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<NotificationBackgroundService>.Instance);

        var notification = ValidNotification();
        channel.Writer.TryWrite(notification);
        channel.Writer.Complete();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await worker.StartAsync(cts.Token);
        await worker.ExecutePublicAsync(cts.Token);

        senderMock.Verify(
            x => x.SendAsync(notification, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task BackgroundService_SenderThrows_DoesNotPropagateException()
    {
        var senderMock = new Mock<INotificationSender>();
        senderMock
            .Setup(x => x.SendAsync(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Provider down"));

        var services = new ServiceCollection();
        services.AddSingleton(senderMock.Object);
        var sp = services.BuildServiceProvider();

        var channel = new NotificationChannel();
        var worker = new NotificationBackgroundService(
            channel,
            sp.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<NotificationBackgroundService>.Instance);

        channel.Writer.TryWrite(ValidNotification());
        channel.Writer.Complete();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var act = async () => await worker.ExecutePublicAsync(cts.Token);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task BackgroundService_ValidationFails_DoesNotPropagateException()
    {
        var senderMock = new Mock<INotificationSender>();
        senderMock
            .Setup(x => x.SendAsync(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException("Invalid notification"));

        var services = new ServiceCollection();
        services.AddSingleton(senderMock.Object);
        var sp = services.BuildServiceProvider();

        var channel = new NotificationChannel();
        var worker = new NotificationBackgroundService(
            channel,
            sp.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<NotificationBackgroundService>.Instance);

        channel.Writer.TryWrite(ValidNotification());
        channel.Writer.Complete();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var act = async () => await worker.ExecutePublicAsync(cts.Token);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task BackgroundService_CancellationRequested_StopsGracefully()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Mock.Of<INotificationSender>());
        var sp = services.BuildServiceProvider();

        var channel = new NotificationChannel();
        var worker = new NotificationBackgroundService(
            channel,
            sp.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<NotificationBackgroundService>.Instance);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await worker.ExecutePublicAsync(cts.Token);

        await act.Should().NotThrowAsync();
    }
}

file sealed class NotificationBackgroundService(
    NotificationChannel notificationChannel,
    IServiceScopeFactory scopeFactory,
    ILogger<NotificationBackgroundService> logger)
    : UEAT.Notification.Library.NotificationBackgroundService(notificationChannel, scopeFactory, logger)
{
    public Task ExecutePublicAsync(CancellationToken ct) => ExecuteAsync(ct);
}
