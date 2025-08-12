using FluentValidation;
using PhotoBank.ViewModel.Dto;

namespace PhotoBank.Api.Validators;

public class UpdateUserDtoValidator : AbstractValidator<UpdateUserDto>
{
    public UpdateUserDtoValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .Matches(@"^[0-9+()\- ]*$").When(x => x.PhoneNumber is not null);
        RuleFor(x => x.Telegram)
            .MaximumLength(32).When(x => x.Telegram is not null);
    }
}
