using FluentValidation;
using PhotoBank.ViewModel.Dto;

namespace PhotoBank.Api.Validators;

public class UpdateFaceIdentityDtoValidator : AbstractValidator<UpdateFaceIdentityDto>
{
    public UpdateFaceIdentityDtoValidator()
    {
        RuleFor(x => x.FaceId).GreaterThan(0);
        RuleFor(x => x.IdentityStatus).IsInEnum();
        When(x => x.PersonId.HasValue, () =>
        {
            RuleFor(x => x.PersonId!.Value).GreaterThan(0);
        });
    }
}
