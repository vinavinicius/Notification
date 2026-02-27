# UEAT.Notification

Multi-channel notification SDK for .NET projects. Supports SMS and e-mail delivery via multiple providers, with Razor template rendering, localization and built-in validation.

---

## Table of Contents

- [How it works](#how-it-works)
- [Architecture](#architecture)
- [Installation & setup](#installation--setup)
- [Adding a new template](#adding-a-new-template)
- [Available providers](#available-providers)
- [Running the tests](#running-the-tests)

---

## How it works

Every notification follows the same pipeline:

```
INotificationSender.SendAsync(notification)
        │
        ├─► Validates the notification (FluentValidation)
        │
        ├─► Renders the Razor template with the notification data
        │
        └─► Delivers via the registered channel (SMS or E-mail)
```

**The three core concepts are:**

| Concept | Interface | Responsibility |
|---|---|---|
| Notification | `INotification` | Carries the data and points to the template |
| Renderer | `ITemplateRenderer` | Renders the Razor template into a string |
| Channel | `IChannelNotification` | Delivers the rendered string via a provider |

---

## Architecture

```
UEAT.Notification.Core
│   INotification, INotificationSender, IChannelNotification, ITemplateRenderer
│   ValueObjects: EmailAddress, MobilePhone
│
UEAT.Notification.Infrastructure
│   SendGridEmailClient, FolioSmsClient
│   RazorTemplateRenderer
│
UEAT.Notification.Library
│   NotificationSender
│   ChannelNotificationBase<T>, SmsChannelNotification, EmailChannelNotification
│   SmsNotificationValidatorBase<T>, EmailNotificationValidatorBase<T>
│   DependencyInjection/ServiceCollectionExtensions
│   SMS/NewMessage/ → sample notification implementation
│
UEAT.Notification.Library.Tests
    Unit and integration tests
```

---

## Installation & setup

### 1. Add the reference

In your project's `.csproj`:

```xml
<ProjectReference Include="..\UEAT.Notification.Library\UEAT.Notification.Library.csproj" />
```

### 2. Register the services

In `Program.cs`:

```csharp
builder.Services
    .AddNotificationLibrary(builder.Configuration)
    .AddFolioSmsProvider()
    .AddSendGridEmailProvider();
```

### 3. Configure credentials

In `appsettings.json` (use environment variables or User Secrets in production):

```json
{
  "FolioConfigurations": {
    "BaseUrl": "https://sms.foliomedian.ca/api/",
    "ApiKey": "your-api-key"
  },
  "SendGridConfigurations": {
    "ApiKey": "your-api-key",
    "FromEmail": "noreply@yourdomain.com",
    "FromName": "Your System"
  }
}
```

> ⚠️ **Never commit real credentials.** Use `dotnet user-secrets` in development and environment variables in production.

---

## Adding a new template

Follow the steps below to create a new notification. The example uses an order confirmation SMS, but the same pattern applies to e-mail notifications.

### Step 1 — Create the notification class

Create a folder to organize your notification and add the class:

```csharp
// SMS/OrderConfirmed/OrderConfirmedSmsNotification.cs

using System.Globalization;
using UEAT.Notification.Core.SMS;
using UEAT.Notification.Core.ValueObjects;

namespace MyProject.SMS.OrderConfirmed;

public class OrderConfirmedSmsNotification(CultureInfo cultureInfo, MobilePhone mobilePhone)
    : ISmsNotification
{
    public CultureInfo CultureInfo { get; } = cultureInfo;
    public MobilePhone MobilePhone { get; } = mobilePhone;

    // Must match the full embedded resource name of the template file
    public string Template { get; } = "MyProject.SMS.OrderConfirmed.Template.cshtml";

    // Notification-specific data
    public string OrderNumber { get; init; } = string.Empty;
    public decimal Total { get; init; }
}
```

### Step 2 — Create the Razor template

Create `Template.cshtml` in the same folder:

```cshtml
@using MyProject.SMS.OrderConfirmed
@inherits RazorLight.TemplatePage<dynamic>
@{
    DisableEncoding = true;
    Layout = null;
    var data = (OrderConfirmedSmsNotification)Model.Data;
}
@Model.L("OrderConfirmedMessage", data.OrderNumber, data.Total)
```

> `Model.L("key", args)` looks up the localized string in the corresponding `.resx` file.

### Step 3 — Create the localization files (.resx)

Create one `.resx` file per supported language:

**`OrderConfirmedSmsNotification.resx`** (default fallback):
```xml
<?xml version="1.0" encoding="utf-8"?>
<root>
  <data name="OrderConfirmedMessage" xml:space="preserve">
    <value>Order #{0} confirmed. Total: ${1:F2}</value>
  </data>
</root>
```

**`OrderConfirmedSmsNotification.en.resx`**:
```xml
<?xml version="1.0" encoding="utf-8"?>
<root>
  <data name="OrderConfirmedMessage" xml:space="preserve">
    <value>Order #{0} confirmed. Total: ${1:F2}</value>
  </data>
</root>
```

**`OrderConfirmedSmsNotification.fr.resx`**:
```xml
<?xml version="1.0" encoding="utf-8"?>
<root>
  <data name="OrderConfirmedMessage" xml:space="preserve">
    <value>Commande #{0} confirmée. Total: {1:F2}$</value>
  </data>
</root>
```

### Step 4 — Embed the resources in the project

In your project's `.csproj`, make sure the `.cshtml` and `.resx` files are included as embedded resources:

```xml
<ItemGroup>
  <!-- Embed all Razor templates -->
  <EmbeddedResource Include="**/*.cshtml" />

  <!-- Mark culture-specific .resx files as dependent on the default one -->
  <EmbeddedResource Update="SMS\OrderConfirmed\OrderConfirmedSmsNotification.en.resx">
    <DependentUpon>OrderConfirmedSmsNotification.resx</DependentUpon>
  </EmbeddedResource>
  <EmbeddedResource Update="SMS\OrderConfirmed\OrderConfirmedSmsNotification.fr.resx">
    <DependentUpon>OrderConfirmedSmsNotification.resx</DependentUpon>
  </EmbeddedResource>
</ItemGroup>
```

### Step 5 — Create the validator

```csharp
// SMS/OrderConfirmed/OrderConfirmedSmsNotificationValidator.cs

using FluentValidation;
using UEAT.Notification.Library.SMS;

namespace MyProject.SMS.OrderConfirmed;

public class OrderConfirmedSmsNotificationValidator
    : SmsNotificationValidatorBase<OrderConfirmedSmsNotification>
{
    public OrderConfirmedSmsNotificationValidator()
    {
        RuleFor(x => x.OrderNumber)
            .NotEmpty()
            .WithMessage("Order number is required.");

        RuleFor(x => x.Total)
            .GreaterThan(0)
            .WithMessage("Total must be greater than zero.");
    }
}
```

### Step 6 — Register your project's assembly

When setting up the services, register the validators from your project's assembly:

```csharp
builder.Services
    .AddNotificationLibrary(builder.Configuration)
    .AddFolioSmsProvider();

// Register validators from your project
builder.Services.AddValidatorsFromAssemblyContaining<OrderConfirmedSmsNotificationValidator>();
```

> ⚠️ The `RazorTemplateRenderer` needs to know which assemblies contain embedded templates. If your templates live outside the Library, you will need to register an additional `ITemplateRenderer` pointing to your assembly. See `ServiceCollectionExtensions` for how the renderer is currently configured.

### Step 7 — Send the notification

```csharp
public class OrderService(INotificationSender notificationSender)
{
    public async Task ConfirmOrderAsync(Order order, CancellationToken ct)
    {
        // ... order logic

        var notification = new OrderConfirmedSmsNotification(
            cultureInfo: CultureInfo.GetCultureInfo("fr-CA"),
            mobilePhone: new MobilePhone("1", "514", "5551234"))
        {
            OrderNumber = order.Number,
            Total = order.Total
        };

        await notificationSender.SendAsync(notification, ct);
    }
}
```

---

### New template checklist

- [ ] Notification class implementing `ISmsNotification` or `IEmailNotification`
- [ ] `Template` property pointing to the correct embedded resource name
- [ ] `Template.cshtml` included as `EmbeddedResource` in the `.csproj`
- [ ] Default `.resx` file + one per supported language
- [ ] All `.resx` files configured as `EmbeddedResource` in the `.csproj`
- [ ] Validator inheriting from `SmsNotificationValidatorBase<T>` or `EmailNotificationValidatorBase<T>`
- [ ] Validators registered via `AddValidatorsFromAssemblyContaining<>`

---

## Available providers

| Provider | Channel | Registration method |
|---|---|---|
| Folio | SMS | `AddFolioSmsProvider()` |
| SendGrid | E-mail | `AddSendGridEmailProvider()` |

All providers have automatic retry (3 attempts with exponential backoff) and circuit breaker (opens after 5 failures, resets after 30 seconds) configured via Polly.

---

## Running the tests

```bash
dotnet test src/UEAT.Notification/UEAT.Notification.Library.Tests
```

The client integration tests (Folio, Twilio, SendGrid) use **WireMock.Net** to simulate external providers — no real credentials are needed to run the test suite.