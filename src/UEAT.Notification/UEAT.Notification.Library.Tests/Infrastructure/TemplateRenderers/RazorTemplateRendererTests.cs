using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using RazorLight;
using UEAT.Notification.Core;
using UEAT.Notification.Core.ValueObjects;
using UEAT.Notification.Infrastructure.TemplateRenderers.Razor;
using Xunit;

namespace UEAT.Notification.Library.Tests.Infrastructure.TemplateRenderers;

public class RazorTemplateRendererTests
{
    [Fact]
    public void CanRender_TemplateExistsInAssembly_ShouldReturnTrue()
    {
        var engineMock = new Mock<IRazorLightEngine>();

        var renderer = new RazorTemplateRenderer(
            engineMock.Object,
            [typeof(Library.SMS.Welcome.WelcomeSmsNotification).Assembly]);

        var notificationMock = new Mock<INotification>();
        notificationMock
            .Setup(x => x.Template)
            .Returns("UEAT.Notification.Library.SMS.Welcome.Template.cshtml");

        var result = renderer.CanRender(notificationMock.Object);

        result.Should().BeTrue();
    }

    [Fact]
    public void CanRender_TemplateDoesNotExist_ShouldReturnFalse()
    {
        var engineMock = new Mock<IRazorLightEngine>();
        var renderer = new RazorTemplateRenderer(
            engineMock.Object,
            [typeof(Library.SMS.Welcome.WelcomeSmsNotification).Assembly]);

        var notificationMock = new Mock<INotification>();
        notificationMock
            .Setup(x => x.Template)
            .Returns("NonExistent.Template.cshtml");

        var result = renderer.CanRender(notificationMock.Object);

        result.Should().BeFalse();
    }

    [Fact]
    public void CanRender_EmptyAssemblyList_ShouldReturnFalse()
    {
        var engineMock = new Mock<IRazorLightEngine>();
        var renderer = new RazorTemplateRenderer(engineMock.Object, []);

        var notificationMock = new Mock<INotification>();
        notificationMock
            .Setup(x => x.Template)
            .Returns("UEAT.Notification.Library.SMS.Welcome.Template.cshtml");

        var result = renderer.CanRender(notificationMock.Object);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task RenderAsync_ShouldCallEngineWithCorrectTemplateKey()
    {
        const string templateKey = "UEAT.Notification.Library.SMS.Welcome.Template.cshtml";
        const string expectedOutput = "Welcome! Your message.";

        var engineMock = new Mock<IRazorLightEngine>();
        engineMock
            .Setup(x => x.CompileRenderAsync(
                templateKey,
                It.IsAny<object>(),
                null))
            .ReturnsAsync(expectedOutput);

        var renderer = new RazorTemplateRenderer(
            engineMock.Object,
            [typeof(Library.SMS.Welcome.WelcomeSmsNotification).Assembly]);

        var notification = new Library.SMS.Welcome.WelcomeSmsNotification(
            System.Globalization.CultureInfo.GetCultureInfo("en-CA"),
            new MobilePhone("1", "581", "5551234"))
        {
            Message = "Your message.",
        };

        var result = await renderer.RenderAsync(notification);

        result.Should().Be(expectedOutput);
        engineMock.Verify(
            x => x.CompileRenderAsync(templateKey, It.IsAny<object>(), null),
            Times.Once);
    }
}
