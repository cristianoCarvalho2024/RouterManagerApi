using FluentValidation;
using RouterManager.Shared.Dtos.Requests;

namespace RouterManager.Api.Validators;

public class ReportStatusRequestValidator : AbstractValidator<ReportStatusRequest>
{
    public ReportStatusRequestValidator()
    {
        RuleFor(x => x.SerialNumber).NotEmpty();
        RuleFor(x => x.ModelIdentifier).NotEmpty();
        RuleFor(x => x.ProviderId).GreaterThan(0);
    }
}