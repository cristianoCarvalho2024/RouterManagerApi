using FluentValidation;
using RouterManager.Api.Models;

namespace RouterManager.Api.Validators;

public class AuthRegisterValidator : AbstractValidator<RegisterRequest>
{
    public AuthRegisterValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MinimumLength(3);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
    }
}

public class AuthLoginValidator : AbstractValidator<LoginRequest>
{
    public AuthLoginValidator()
    {
        RuleFor(x => x.Username).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
    }
}