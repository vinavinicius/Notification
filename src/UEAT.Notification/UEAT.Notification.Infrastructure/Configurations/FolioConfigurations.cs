using System.ComponentModel.DataAnnotations;

namespace UEAT.Notification.Infrastructure.Configurations;

public class FolioConfigurations
{
    [Required(ErrorMessage = "Folio BaseUrl is required")]
    [Url(ErrorMessage = "Folio BaseUrl must be a valid URL")]
    public required string BaseUrl { get; set; }
    
    [Required(ErrorMessage = "Folio ApiKey is required")]
    public required string ApiKey { get; set; }
}