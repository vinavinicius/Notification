using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Twilio.Clients;
using Twilio.Exceptions;
using UEAT.Notification.Core.SMS;
using UEAT.Notification.Infrastructure.Configurations;
using UEAT.Notification.Infrastructure.SMS.Twilio;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace UEAT.Notification.Library.Tests.Infrastructure.Clients;

public class TwilioSmsClientTests : IDisposable
{
    private readonly WireMockServer _server;
    private readonly TwilioSmsClient _client;

    // Twilio SDK monta a URL como: /2010-04-01/Accounts/{AccountSid}/Messages.json
    private const string AccountSid = "ACtest123";
    private const string MessagesPath = $"/2010-04-01/Accounts/{AccountSid}/Messages.json";

    private static readonly string SuccessBody = """
                                                 {
                                                     "sid": "SMxxx",
                                                     "status": "queued",
                                                     "body": "Welcome!",
                                                     "to": "+15815551234",
                                                     "from": "+15550000000"
                                                 }
                                                 """;

    public TwilioSmsClientTests()
    {
        _server = WireMockServer.Start();

        var httpClient = new HttpClient
        {
            BaseAddress = new Uri(_server.Url!)
        };

        var twilioRestClient = new TwilioRestClient(
            username: AccountSid,
            password: "test-auth-token",
            accountSid: AccountSid,
            httpClient: new Twilio.Http.SystemNetHttpClient(httpClient));

        var options = Options.Create(new TwilioConfigurations
        {
            AccountKey = "test-auth-token",
            AccountName = AccountSid,
            NumberFrom = "+15550000000"
        });

        _client = new TwilioSmsClient(
            twilioRestClient,
            options,
            NullLogger<TwilioSmsClient>.Instance);
    }

    [Fact]
    public async Task SendAsync_SuccessResponse_ShouldNotThrow()
    {
        _server
            .Given(Request.Create()
                .WithPath(MessagesPath)
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(201)
                .WithHeader("Content-Type", "application/json")
                .WithBody(SuccessBody));

        var act = async () => await _client.SendAsync(new SmsMessage("+15815551234", "Welcome!"));

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendAsync_ShouldPostToCorrectTwilioEndpoint()
    {
        _server
            .Given(Request.Create()
                .WithPath(MessagesPath)
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(201)
                .WithHeader("Content-Type", "application/json")
                .WithBody(SuccessBody));

        await _client.SendAsync(new SmsMessage("+15815551234", "Welcome!"));

        var entry = _server.LogEntries.Should().HaveCount(1).And.Subject.Single();
        entry.RequestMessage.Path.Should().Be(MessagesPath);
        entry.RequestMessage.Method.Should().Be("POST");
    }

    [Fact]
    public async Task SendAsync_ShouldSendCorrectFormParameters()
    {
        _server
            .Given(Request.Create()
                .WithPath(MessagesPath)
                .UsingPost()
                .WithBody(b =>
                    b.Contains("To=") &&
                    b.Contains("From=") &&
                    b.Contains("Body=")))
            .RespondWith(Response.Create()
                .WithStatusCode(201)
                .WithHeader("Content-Type", "application/json")
                .WithBody(SuccessBody));

        await _client.SendAsync(new SmsMessage("+15815551234", "Welcome!"));

        _server.LogEntries.Should().HaveCount(1);
    }

    [Fact]
    public async Task SendAsync_ServerReturns400_ShouldThrowApiException()
    {
        _server
            .Given(Request.Create()
                .WithPath(MessagesPath)
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(400)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                          {
                              "code": 21211,
                              "message": "The 'To' number is not a valid phone number",
                              "more_info": "https://www.twilio.com/docs/errors/21211",
                              "status": 400
                          }
                          """));

        var act = async () => await _client.SendAsync(new SmsMessage("+invalid", "Welcome!"));

        await act.Should().ThrowAsync<ApiException>()
            .Where(ex => ex.Code == 21211);
    }

    [Fact]
    public async Task SendAsync_ServerReturns401_ShouldThrowApiException()
    {
        _server
            .Given(Request.Create()
                .WithPath(MessagesPath)
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(401)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                          {
                              "code": 20003,
                              "message": "Authenticate",
                              "more_info": "https://www.twilio.com/docs/errors/20003",
                              "status": 401
                          }
                          """));

        var act = async () => await _client.SendAsync(new SmsMessage("+15815551234", "Welcome!"));

        await act.Should().ThrowAsync<ApiException>()
            .Where(ex => ex.Code == 20003);
    }

    [Fact]
    public async Task SendAsync_ServerReturns500_ShouldThrowApiException()
    {
        _server
            .Given(Request.Create()
                .WithPath(MessagesPath)
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""
                          {
                              "code": 500,
                              "message": "Internal Server Error",
                              "status": 500
                          }
                          """));

        var act = async () => await _client.SendAsync(new SmsMessage("+15815551234", "Welcome!"));

        await act.Should().ThrowAsync<ApiException>();
    }

    [Fact]
    public async Task SendAsync_ServerTimesOut_ShouldThrow()
    {
        _server
            .Given(Request.Create()
                .WithPath(MessagesPath)
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(201)
                .WithDelay(TimeSpan.FromSeconds(10)));

        var slowHttpClient = new HttpClient
        {
            BaseAddress = new Uri(_server.Url!),
            Timeout = TimeSpan.FromMilliseconds(100)
        };

        var twilioRestClient = new TwilioRestClient(
            username: AccountSid,
            password: "test-auth-token",
            accountSid: AccountSid,
            httpClient: new Twilio.Http.SystemNetHttpClient(slowHttpClient));

        var clientWithTimeout = new TwilioSmsClient(
            twilioRestClient,
            Options.Create(new TwilioConfigurations
            {
                AccountKey = "test-auth-token",
                AccountName = AccountSid,
                NumberFrom = "+15550000000"
            }),
            NullLogger<TwilioSmsClient>.Instance);

        var act = async () => await clientWithTimeout.SendAsync(new SmsMessage("+15815551234", "Welcome!"));

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task SendAsync_CancellationRequested_ShouldThrowOperationCanceledException()
    {
        _server
            .Given(Request.Create()
                .WithPath(MessagesPath)
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(201)
                .WithHeader("Content-Type", "application/json")
                .WithDelay(TimeSpan.FromSeconds(5))
                .WithBody(SuccessBody));

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(50));

        var act = async () => await _client.SendAsync(new SmsMessage("+15815551234", "Welcome!"), cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task SendAsync_ConnectionRefused_ShouldThrowWithoutApiException()
    {
        // Para este teste, apontamos para uma porta que não existe
        var deadHttpClient = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:1") // porta fechada
        };

        var twilioRestClient = new TwilioRestClient(
            username: AccountSid,
            password: "test-auth-token",
            accountSid: AccountSid,
            httpClient: new Twilio.Http.SystemNetHttpClient(deadHttpClient));

        var clientDead = new TwilioSmsClient(
            twilioRestClient,
            Options.Create(new TwilioConfigurations
            {
                AccountKey = "test-auth-token",
                AccountName = AccountSid,
                NumberFrom = "+15550000000"
            }),
            NullLogger<TwilioSmsClient>.Instance);

        var act = async () => await clientDead.SendAsync(new SmsMessage("+15815551234", "Welcome!"));

        await act.Should().ThrowAsync<Exception>();
    }

    public void Dispose()
    {
        _server.Stop();
        _server.Dispose();
    }
}