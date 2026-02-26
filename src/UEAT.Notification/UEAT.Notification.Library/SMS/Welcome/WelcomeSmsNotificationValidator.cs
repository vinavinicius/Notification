using FluentValidation;

namespace UEAT.Notification.Library.SMS.Welcome;

public class WelcomeSmsNotificationValidator : SmsNotificationValidatorBase<WelcomeSmsNotification>
{
    public WelcomeSmsNotificationValidator()
    {
        RuleFor(x => x.Message)
            .NotNull()
            .NotEmpty();
    }
}
