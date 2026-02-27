using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SendGrid;
using UEAT.Notification.Core.Email;
using UEAT.Notification.Infrastructure.Configurations;
using UEAT.Notification.Infrastructure.Email.SendGrid;
using WireMock.RequestBuilders;
using WireMock.Server;
using Xunit;
using Response = WireMock.ResponseBuilders.Response;

namespace UEAT.Notification.Library.Tests.Infrastructure.Clients;

public class SendGridEmailClientTests : IDisposable
{
    private readonly WireMockServer _server;
    private readonly SendGridEmailClient _client;

    public SendGridEmailClientTests()
    {
        _server = WireMockServer.Start();

        var sendGridConfig = Options.Create(new SendGridConfigurations
        {
            ApiKey = "test-api-key",
            FromEmail = "noreply@example.com",
            FromName = "Test Sender"
        });

        // SendGrid SDK allows custom base URL for testing
        var sendGridClient = new SendGridClient(new SendGridClientOptions
        {
            ApiKey = "test-api-key",
            Host = _server.Url
        });

        _client = new SendGridEmailClient(
            sendGridClient,
            sendGridConfig,
            NullLogger<SendGridEmailClient>.Instance);
    }

    [Fact]
    public async Task SendAsync_SuccessResponse_ShouldNotThrow()
    {
        _server
            .Given(Request.Create()
                .WithPath("/v3/mail/send")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(202)); // SendGrid returns 202 Accepted

        var message = new EmailMessage("user@example.com", "Welcome", "<h1>Hello!</h1>");

        var act = async () => await _client.SendAsync(message);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendAsync_ServerReturns400_ShouldThrowHttpRequestException()
    {
        _server
            .Given(Request.Create()
                .WithPath("/v3/mail/send")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(400)
                .WithBody("""{"errors":[{"message":"Bad Request"}]}"""));

        var message = new EmailMessage("user@example.com", "Welcome", "<h1>Hello!</h1>");

        var act = async () => await _client.SendAsync(message);

        await act.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("*400*");
    }

    [Fact]
    public async Task SendAsync_ServerReturns401_ShouldThrowHttpRequestException()
    {
        _server
            .Given(Request.Create()
                .WithPath("/v3/mail/send")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(401)
                .WithBody("""{"errors":[{"message":"Unauthorized"}]}"""));

        var message = new EmailMessage("user@example.com", "Welcome", "<h1>Hello!</h1>");

        var act = async () => await _client.SendAsync(message);

        await act.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("*401*");
    }

    [Fact]
    public async Task SendAsync_ServerReturns500_ShouldThrowHttpRequestException()
    {
        _server
            .Given(Request.Create()
                .WithPath("/v3/mail/send")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody("Internal Server Error"));

        var message = new EmailMessage("user@example.com", "Welcome", "<h1>Hello!</h1>");

        var act = async () => await _client.SendAsync(message);

        await act.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("*500*");
    }

    [Fact]
    public async Task SendAsync_ShouldSendCorrectPayload()
    {
        _server
            .Given(Request.Create()
                .WithPath("/v3/mail/send")
                .UsingPost()
                .WithBody(b =>
                    b!.Contains("user@example.com") &&
                    b.Contains("Welcome Subject") &&
                    b.Contains("noreply@example.com")))
            .RespondWith(Response.Create()
                .WithStatusCode(202));

        var message = new EmailMessage("user@example.com", "Welcome Subject", "<h1>Hello!</h1>");

        await _client.SendAsync(message);

        _server.LogEntries.Should().HaveCount(1);
    }

    [Fact]
    public async Task SendAsync_CancellationRequested_ShouldThrowOperationCanceledException()
    {
        _server
            .Given(Request.Create()
                .WithPath("/v3/mail/send")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(202)
                .WithDelay(TimeSpan.FromSeconds(5)));

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(50));

        var act = async () => await _client.SendAsync(
            new EmailMessage("user@example.com", "Subject", "Body"),
            cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    public void Dispose()
    {
        _server.Stop();
        _server.Dispose();
    }
}
