using FluentValidation;
using UEAT.Notification.Core.SMS;

namespace UEAT.Notification.Library.SMS;

public abstract class SmsNotificationValidatorBase<T> : AbstractValidator<T>
    where T : ISmsNotification
{
    protected SmsNotificationValidatorBase()
    {
        RuleFor(x => x.MobilePhone)
            .NotNull()
            .NotEmpty()
            .Matches(@"^\+?[1-9]\d{1,14}$")
            .WithMessage("'Mobile Phone' must be a valid phone number in E.164 format.");

        RuleFor(x => x.CultureInfo)
            .NotNull()
            .WithMessage("'Culture Info' is required for localization.");
    }
}
