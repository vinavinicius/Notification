using System;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using UEAT.Notification.Core;
using UEAT.Notification.Core.Email;
using UEAT.Notification.Core.SMS;
using UEAT.Notification.Core.ValueObjects;
using UEAT.Notification.Library.SMS.Welcome;
using Xunit;

namespace UEAT.Notification.Library.Tests.ChannelNotifications;

public class SmsChannelNotificationTests
{
    private readonly Mock<ISmsClient> _smsClientMock = new();
    private readonly SmsChannelNotification _channel;

    public SmsChannelNotificationTests()
    {
        _channel = new SmsChannelNotification(_smsClientMock.Object);
    }

    private static WelcomeSmsNotification ValidSmsNotification() =>
        new(CultureInfo.GetCultureInfo("en-CA"), new MobilePhone("1", "581", "5551234"))
        {
            Message = "Welcome!"
        };

    [Fact]
    public void CanHandle_SmsNotification_ShouldReturnTrue()
    {
        var result = ((IChannelNotification)_channel).CanHandle(ValidSmsNotification());

        result.Should().BeTrue();
    }

    [Fact]
    public void CanHandle_NonSmsNotification_ShouldReturnFalse()
    {
        var emailNotification = new Mock<INotification>();

        var result = ((IChannelNotification)_channel).CanHandle(emailNotification.Object);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendNotificationAsync_ValidNotification_ShouldCallSmsClient()
    {
        var notification = ValidSmsNotification();
        const string renderedContent = "Welcome! This is your message.";

        await ((IChannelNotification)_channel).SendNotificationAsync(
            notification, renderedContent, CancellationToken.None);

        _smsClientMock.Verify(
            x => x.SendAsync(
                It.Is<SmsMessage>(m =>
                    m.PhoneNumber == notification.MobilePhone.FullNumber &&
                    m.Content == renderedContent),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendNotificationAsync_WrongNotificationType_ShouldThrowInvalidOperationException()
    {
        var wrongNotification = new Mock<INotification>().Object;

        var act = async () => await ((IChannelNotification)_channel).SendNotificationAsync(
            wrongNotification, "content", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task SendNotificationAsync_SmsClientThrows_ShouldPropagateException()
    {
        _smsClientMock
            .Setup(x => x.SendAsync(It.IsAny<SmsMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Folio API returned 500"));

        var act = async () => await ((IChannelNotification)_channel).SendNotificationAsync(
            ValidSmsNotification(), "content", CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("*500*");
    }
}

public class EmailChannelNotificationTests
{
    private readonly Mock<IEmailClient> _emailClientMock = new();
    private readonly EmailChannelNotification _channel;

    public EmailChannelNotificationTests()
    {
        _channel = new EmailChannelNotification(_emailClientMock.Object);
    }

    private static Mock<IEmailNotification> ValidEmailNotification()
    {
        var mock = new Mock<IEmailNotification>();
        mock.Setup(x => x.To).Returns(new EmailAddress("user@example.com"));
        mock.Setup(x => x.Subject).Returns("Welcome");
        mock.Setup(x => x.CultureInfo).Returns(CultureInfo.GetCultureInfo("en-CA"));
        return mock;
    }

    [Fact]
    public void CanHandle_EmailNotification_ShouldReturnTrue()
    {
        var result = ((IChannelNotification)_channel).CanHandle(ValidEmailNotification().Object);

        result.Should().BeTrue();
    }

    [Fact]
    public void CanHandle_NonEmailNotification_ShouldReturnFalse()
    {
        var smsNotification = new Mock<ISmsNotification>();

        var result = ((IChannelNotification)_channel).CanHandle(smsNotification.Object);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendNotificationAsync_ValidNotification_ShouldCallEmailClient()
    {
        var notification = ValidEmailNotification().Object;
        const string renderedContent = "<h1>Welcome!</h1>";

        await ((IChannelNotification)_channel).SendNotificationAsync(
            notification, renderedContent, CancellationToken.None);

        _emailClientMock.Verify(
            x => x.SendAsync(
                It.Is<EmailMessage>(m =>
                    m.To == notification.To.Address &&
                    m.Subject == notification.Subject &&
                    m.Content == renderedContent),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
