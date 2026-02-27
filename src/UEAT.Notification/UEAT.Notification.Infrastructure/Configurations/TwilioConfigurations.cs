using System.ComponentModel.DataAnnotations;

namespace UEAT.Notification.Infrastructure.Configurations;

public class TwilioConfigurations
{
    [Required(ErrorMessage = "Twilio AccountKey is required")]
    public required string AccountKey { get; set; }

    [Required(ErrorMessage = "Twilio AccountName is required")]
    public required string AccountName { get; set; }

    [Required(ErrorMessage = "Twilio NumberFrom is required")]
    [RegularExpression(@"^\+[1-9]\d{1,14}$", ErrorMessage = "NumberFrom must be in E.164 format")]
    public required string NumberFrom { get; set; }
}
