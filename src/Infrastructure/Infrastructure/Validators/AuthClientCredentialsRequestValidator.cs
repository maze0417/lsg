using FluentValidation;
using LSG.Core.Messages.Auth;

namespace LSG.Infrastructure.Validators
{
    public sealed class AuthClientCredentialsRequestValidator : AbstractValidator<AuthClientCredentialsRequest>
    {
        public AuthClientCredentialsRequestValidator()
        {
            RuleFor(x => x.ClientId)
                .NotNull()
                .NotEmpty()
                .Length(2, 128);
            RuleFor(x => x.ClientSecret)
                .NotNull()
                .NotEmpty()
                .Length(2, 128);
        }
    }
}