using FluentValidation;

namespace UEAT.Notification.Library.SMS.NoDateOrder;

public class NoDateOrderSmsNotification : SmsNotificationValidatorBase<NoDateOrderNotification>
{
    public NoDateOrderSmsNotification()
    {
        RuleFor(x => x.OrderNumber)
            .NotNull()
            .NotEmpty();
            
        RuleFor(x => x.RestaurantName)
            .NotNull()
            .NotEmpty();
    }
}
