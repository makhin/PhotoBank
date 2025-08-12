using FluentValidation;
using PhotoBank.ViewModel.Dto;

namespace PhotoBank.Api.Validators;

public class UpdateFaceDtoValidator : AbstractValidator<UpdateFaceDto>
{
    public UpdateFaceDtoValidator()
    {
        RuleFor(x => x.FaceId).GreaterThan(0);
        RuleFor(x => x.PersonId).GreaterThan(0);
    }
}
