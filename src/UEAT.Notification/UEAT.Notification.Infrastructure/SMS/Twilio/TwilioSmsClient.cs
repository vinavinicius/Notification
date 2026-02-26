using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using Twilio.Clients;
using Twilio.Exceptions;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using UEAT.Notification.Core.SMS;
using UEAT.Notification.Infrastructure.Configurations;

namespace UEAT.Notification.Infrastructure.SMS.Twilio;

public class TwilioSmsClient(
    ITwilioRestClient twilioRestClient,
    IOptions<TwilioConfigurations> twilioOptions,
    ILogger<TwilioSmsClient> logger)
    : ISmsClient
{
    private readonly TwilioConfigurations _twilioConfigurations = twilioOptions.Value;
    
    public async Task SendAsync(SmsMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            await _retryPolicy.ExecuteAsync(async () =>
            {
                await MessageResource.CreateAsync(
                    body: message.Content,
                    from: new PhoneNumber(_twilioConfigurations.NumberFrom),
                    to: new PhoneNumber(message.PhoneNumber),
                    client: twilioRestClient
                );
            });
        }
        catch (ApiException ex)
        {
            logger.LogError(
                ex,
                "Twilio API failed after all retries. ErrorCode: {ErrorCode}, Message: {ErrorMessage}, To: {To}",
                ex.Code,
                ex.Message,
                message.PhoneNumber);
            throw;
        }
        
        logger.LogInformation(
            "SMS sent successfully via Twilio. To: {To}",
            message.PhoneNumber);
    }

    private static readonly int[] RetryableErrorCodes =
    [
        20003, // Permission denied (temporary)
        20429, // Too many requests
        30001, // Queue overflow
        30002, // Account suspended (temporary)
        30008, // Unknown error
        30022, // Twilio is unable to process your request
        52001  // Timeout
    ];

    private readonly AsyncRetryPolicy _retryPolicy = Policy
        .Handle<ApiException>(ex => RetryableErrorCodes.Contains(ex.Code))
        .Or<HttpRequestException>()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
            onRetry: (exception, timespan, attempt, _) =>
            {
                var errorCode = (exception as ApiException)?.Code;
                logger.LogWarning(
                    exception,
                    "Twilio API retry attempt {Attempt} after {Delay}s. ErrorCode: {ErrorCode}",
                    attempt,
                    timespan.TotalSeconds,
                    errorCode);
            });
}