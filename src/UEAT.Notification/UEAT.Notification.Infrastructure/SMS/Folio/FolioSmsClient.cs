using Microsoft.Extensions.Logging;
using UEAT.Notification.Core.SMS;

namespace UEAT.Notification.Infrastructure.SMS.Folio;

public class FolioSmsClient(
    HttpClient httpClient,
    ILogger<FolioSmsClient> logger) : ISmsClient
{
    public async Task SendAsync(SmsMessage message, CancellationToken cancellationToken = default)
    {
        var parameters = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("to", message.PhoneNumber),
            new KeyValuePair<string, string>("message", message.Content)
        ]);

        using var response = await httpClient.PostAsync("send", parameters, cancellationToken);

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError(
                "Folio API failed after all retries. StatusCode: {StatusCode}, Response: {Response}, To: {To}",
                (int)response.StatusCode,
                responseBody,
                message.PhoneNumber);

            throw new HttpRequestException(
                $"Folio API returned {(int)response.StatusCode} " +
                $"{response.ReasonPhrase}: {responseBody}",
                inner: null,
                statusCode: response.StatusCode);
        }

        logger.LogInformation(
            "SMS sent successfully via Folio. To: {To}",
            message.PhoneNumber);
    }
}