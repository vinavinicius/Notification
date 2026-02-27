using System.Globalization;
using UEAT.Notification.Core;
using UEAT.Notification.Core.ValueObjects;
using UEAT.Notification.Library.DependencyInjection;
using UEAT.Notification.Library.SMS.Welcome;

var builder = WebApplication.CreateBuilder(args);

var libraryAssembly = typeof(WelcomeSmsNotification).Assembly;

builder.Services
    .AddNotificationLibrary(builder.Configuration)
    .AddFolioSmsProvider()
    .AddSendGridEmailProvider();

var app = builder.Build();

app.MapNotificationWebhook();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.MapPost("/send-sms", async (INotificationSender sender, SmsRequest request, ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("Sending SMS to {PhoneNumber}", request.PhoneNumber);
        var culture = new CultureInfo(request.Language ?? "en-CA");
        var notification = new WelcomeSmsNotification(culture, new MobilePhone("1", "581", request.PhoneNumber))
        {
            Message = request.Message ?? "Welcome!"
        };

        await sender.SendAsync(notification);
        logger.LogInformation("SMS sent successfully to {PhoneNumber}", request.PhoneNumber);
        return Results.Ok(new { success = true, message = "SMS sent successfully" });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to send SMS to {PhoneNumber}", request.PhoneNumber);
        return Results.Problem(ex.Message);
    }
});

app.MapGet("/", () => "Notification Service Test - Use POST /send-sms or POST /send-email. Visit /swagger for API documentation.");

app.Run();

// Request DTOs
public record SmsRequest(string PhoneNumber, string? Message, string? Language);