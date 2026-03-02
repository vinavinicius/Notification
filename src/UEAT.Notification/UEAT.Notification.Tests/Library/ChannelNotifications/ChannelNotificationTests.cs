using System.Globalization;
using FluentAssertions;
using Moq;
using UEAT.Notification.Core;
using UEAT.Notification.Core.SMS;
using UEAT.Notification.Core.ValueObjects;
using UEAT.Notification.Library;
using UEAT.Notification.Library.SMS.NoDateOrder;

namespace UEAT.Notification.Tests.Library.ChannelNotifications;

public class SmsChannelNotificationTests
{
    private readonly Mock<ISmsClient> _smsClientMock = new();
    private readonly SmsChannelNotification _channel;

    public SmsChannelNotificationTests()
    {
        _channel = new SmsChannelNotification(_smsClientMock.Object);
    }

    private static NoDateOrderSmsNotification ValidSmsNotification() => new(
        new CultureInfo("en-US"),
        new MobilePhone("1", "581", "5551234"),
        orderNumber: 12345,
        restaurantName: "Testaurant");

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

        await ((IChannelNotification)_channel).SendNotificationAsync(
            notification, It.IsAny<string>(), CancellationToken.None);

        _smsClientMock.Verify(
            x => x.SendAsync(
                It.Is<SmsMessage>(m =>
                    m.PhoneNumber == notification.MobilePhone.FullNumber &&
                    m.Content == It.IsAny<string>()),
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