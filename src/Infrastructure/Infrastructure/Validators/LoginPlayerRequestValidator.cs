using FluentValidation;
using LSG.Core.Messages.Player;

namespace LSG.Infrastructure.Validators
{
    public sealed class LoginPlayerRequestValidator : AbstractValidator<LoginPlayerRequest>
    {
        public LoginPlayerRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotNull()
                .NotEmpty()
                .Length(2, 128);
            RuleFor(x => x.ExternalId)
                .NotNull()
                .NotEmpty()
                .Length(2, 128);

            RuleFor(x => x.CultureCode)
                .NotNull()
                .NotEmpty()
                .Length(2, 64);

            RuleFor(x => x.Type)
                .NotNull()
                .NotEmpty()
                .IsInEnum();

            RuleFor(x => x.CurrencyCode)
                .NotNull()
                .NotEmpty()
                .Length(2, 64);

            RuleFor(x => x.BetLimitGroupId)
                .NotNull()
                .NotEmpty();
        }
    }
}