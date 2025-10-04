using FluentValidation;

namespace RouterManager.Api.Validators;

public class AuthRegisterValidator : AbstractValidator<RouterManager.Api.Controllers.AuthController.AuthRequest>
{
    public AuthRegisterValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MinimumLength(3);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
    }
}