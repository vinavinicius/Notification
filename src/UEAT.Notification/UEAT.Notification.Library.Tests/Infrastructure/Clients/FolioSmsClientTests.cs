using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using UEAT.Notification.Core.SMS;
using UEAT.Notification.Infrastructure.SMS.Folio;
using WireMock.Server;
using Xunit;
using HttpClient = System.Net.Http.HttpClient;
using Request = WireMock.RequestBuilders.Request;
using Response = WireMock.ResponseBuilders.Response;

namespace UEAT.Notification.Library.Tests.Infrastructure.Clients;

public class FolioSmsClientTests : IDisposable
{
    private readonly WireMockServer _server;
    private readonly FolioSmsClient _client;

    public FolioSmsClientTests()
    {
        _server = WireMockServer.Start();

        var httpClient = new HttpClient
        {
            BaseAddress = new Uri(_server.Url!)
        };

        _client = new FolioSmsClient(
            httpClient,
            NullLogger<FolioSmsClient>.Instance);
    }

    [Fact]
    public async Task SendAsync_SuccessResponse_ShouldNotThrow()
    {
        _server
            .Given(Request.Create()
                .WithPath("/send")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody("OK"));

        var message = new SmsMessage("+15815551234", "Welcome!");

        var act = async () => await _client.SendAsync(message);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendAsync_ShouldPostCorrectFormParameters()
    {
        _server
            .Given(Request.Create()
                .WithPath("/send")
                .UsingPost()
                .WithBody(b => b.Contains("to=%2B15815551234") && b.Contains("message=Welcome%21")))
            .RespondWith(Response.Create()
                .WithStatusCode(200));

        var message = new SmsMessage("+15815551234", "Welcome!");

        await _client.SendAsync(message);

        var requests = _server.LogEntries;
        requests.Should().HaveCount(1);
    }

    [Fact]
    public async Task SendAsync_ServerReturns500_ShouldThrowHttpRequestException()
    {
        _server
            .Given(Request.Create()
                .WithPath("/send")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody("Internal Server Error"));

        var message = new SmsMessage("+15815551234", "Welcome!");

        var act = async () => await _client.SendAsync(message);

        await act.Should().ThrowAsync<HttpRequestException>().WithMessage("*500*");
    }

    [Fact]
    public async Task SendAsync_ServerReturns400_ShouldThrowHttpRequestException()
    {
        _server
            .Given(Request.Create()
                .WithPath("/send")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(400)
                .WithBody("Bad Request - Invalid phone number"));

        var message = new SmsMessage("+15815551234", "Welcome!");

        var act = async () => await _client.SendAsync(message);

        await act.Should().ThrowAsync<HttpRequestException>().WithMessage("*400*");
    }

    [Fact]
    public async Task SendAsync_ServerReturns401_ShouldThrowHttpRequestException()
    {
        _server
            .Given(Request.Create()
                .WithPath("/send")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(401)
                .WithBody("Unauthorized - Invalid API key"));

        var message = new SmsMessage("+15815551234", "Welcome!");

        var act = async () => await _client.SendAsync(message);

        await act.Should().ThrowAsync<HttpRequestException>().WithMessage("*401*");
    }

    [Fact]
    public async Task SendAsync_ServerTimesOut_ShouldThrow()
    {
        _server
            .Given(Request.Create()
                .WithPath("/send")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithDelay(TimeSpan.FromSeconds(10)));

        var httpClient = new HttpClient
        {
            BaseAddress = new Uri(_server.Url!),
            Timeout = TimeSpan.FromMilliseconds(100)
        };

        var clientWithTimeout = new FolioSmsClient(
            httpClient,
            NullLogger<FolioSmsClient>.Instance);

        var act = async () => await clientWithTimeout.SendAsync(new SmsMessage("+15815551234", "Welcome!"));

        await act.Should().ThrowAsync<TaskCanceledException>();
    }

    [Fact]
    public async Task SendAsync_CancellationRequested_ShouldThrowOperationCanceledException()
    {
        _server
            .Given(Request.Create()
                .WithPath("/send")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithDelay(TimeSpan.FromSeconds(5)));

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(50));

        var act = async () => await _client.SendAsync(new SmsMessage("+15815551234", "Welcome!"), cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    public void Dispose()
    {
        _server.Stop();
        _server.Dispose();
    }
}