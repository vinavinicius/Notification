using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using UEAT.Notification.Core;

namespace UEAT.Notification.Library;

public class FluentValidationNotificationValidator(IServiceProvider serviceProvider)
{
    public async Task ValidateAsync(INotification notification, CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        
        var validatorType = typeof(IValidator<>)
            .MakeGenericType(notification.GetType());

        if (scope.ServiceProvider.GetService(validatorType) is not IValidator validator)
            return;

        var context = new ValidationContext<object>(notification);
        var result = await validator.ValidateAsync(context, ct);

        if (!result.IsValid)
            throw new ValidationException(result.Errors);
    }
}