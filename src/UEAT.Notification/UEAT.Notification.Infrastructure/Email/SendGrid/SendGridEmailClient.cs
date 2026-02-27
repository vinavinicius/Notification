using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
        var msg = MailHelper.CreateSingleEmail(from, to, message.Subject, plainTextContent: null,
            htmlContent: message.Content);

        var response = await sendGridClient.SendEmailAsync(msg, cancellationToken);

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
}