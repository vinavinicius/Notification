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
            await MessageResource.CreateAsync(
                body: message.Content,
                from: new PhoneNumber(_twilioConfigurations.NumberFrom),
                to: new PhoneNumber(message.PhoneNumber),
                client: twilioRestClient
            );
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
}