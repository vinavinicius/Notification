using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using SendGrid;
using SendGrid.Helpers.Mail;
using UEAT.Notification.Core.Email;
using UEAT.Notification.Infrastructure.Configurations;

namespace UEAT.Notification.Infrastructure.Email.SendGrid;

public class SendGridEmailClient(
    ISendGridClient sendGridClient,
    IOptions<SendGridConfigurations> sendGridOptions,
    ILogger<SendGridEmailClient> logger)
    : IEmailClient
{
    private readonly SendGridConfigurations _sendGridConfigurations = sendGridOptions.Value;

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var from = new EmailAddress(_sendGridConfigurations.FromEmail, _sendGridConfigurations.FromName);
        var to = new EmailAddress(message.To);
        var msg = MailHelper.CreateSingleEmail(from, to, message.Subject, plainTextContent: null, htmlContent: message.Content);

        var response = await _retryPolicy.ExecuteAsync(async () =>
            await sendGridClient.SendEmailAsync(msg, cancellationToken));

        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Body.ReadAsStringAsync(cancellationToken);

            logger.LogError(
                "SendGrid API failed after all retries. StatusCode: {StatusCode}, Response: {Response}, To: {To}",
                (int)response.StatusCode,
                responseBody,
                message.To);

            throw new HttpRequestException(
                $"SendGrid API returned {(int)response.StatusCode}: {responseBody}");
        }

        logger.LogInformation(
            "Email sent successfully via SendGrid. To: {To}, StatusCode: {StatusCode}",
            message.To,
            (int)response.StatusCode);
    }

    private static readonly HttpStatusCode[] RetryableStatusCodes =
    [
        HttpStatusCode.RequestTimeout,
        HttpStatusCode.TooManyRequests,
        HttpStatusCode.InternalServerError,
        HttpStatusCode.BadGateway,
        HttpStatusCode.ServiceUnavailable,
        HttpStatusCode.GatewayTimeout
    ];

    private readonly AsyncRetryPolicy<Response> _retryPolicy = Policy<Response>
        .HandleResult(r => RetryableStatusCodes.Contains(r.StatusCode))
        .Or<HttpRequestException>()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
            onRetry: (outcome, timespan, attempt, _) =>
            {
                logger.LogWarning(
                    "SendGrid API retry attempt {Attempt} after {Delay}s. StatusCode: {StatusCode}",
                    attempt,
                    timespan.TotalSeconds,
                    outcome.Result?.StatusCode);
            });
}