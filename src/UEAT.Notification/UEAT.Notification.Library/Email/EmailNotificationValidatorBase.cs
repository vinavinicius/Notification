using FluentValidation;
using UEAT.Notification.Core.Email;

namespace UEAT.Notification.Library.Email;

public abstract class EmailNotificationValidatorBase<T> : AbstractValidator<T>
    where T : IEmailNotification
{
    protected EmailNotificationValidatorBase()
    {
        RuleFor(x => x.To.Address)
            .NotEmpty()
            .EmailAddress()
            .WithMessage("'To' must be a valid email address.");

        RuleFor(x => x.Subject)
            .NotEmpty()
            .MaximumLength(200)
            .WithMessage("'Subject' is required and must not exceed 200 characters.");

        RuleFor(x => x.CultureInfo)
            .NotNull()
            .WithMessage("'Culture Info' is required for localization.");
    }
}
