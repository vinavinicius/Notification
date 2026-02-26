using System.ComponentModel.DataAnnotations;

namespace UEAT.Notification.Infrastructure.Configurations;

public class SendGridConfigurations
{
    [Required(ErrorMessage = "SendGrid ApiKey is required")]
    public required string ApiKey { get; set; }
    
    [Required(ErrorMessage = "SendGrid FromEmail is required")]
    [EmailAddress(ErrorMessage = "SendGrid FromEmail must be a valid email address")]
    public required string FromEmail { get; set; }
    
    [Required(ErrorMessage = "SendGrid FromName is required")]
    [MaxLength(100, ErrorMessage = "SendGrid FromName cannot exceed 100 characters")]
    public required string FromName { get; set; }
}
