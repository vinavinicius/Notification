using FluentValidation;

namespace UEAT.Notification.Library.SMS.NoDateOrder;

public class NoDateOrderSmsNotificationValidator : SmsNotificationValidatorBase<NoDateOrderSmsNotification>
{
    public NoDateOrderSmsNotificationValidator()
    {
        RuleFor(x => x.OrderNumber)
            .NotNull()
            .NotEmpty();
            
        RuleFor(x => x.RestaurantName)
            .NotNull()
            .NotEmpty();
    }
}
