using FluentValidation;
using PhotoBank.ViewModel.Dto;

namespace PhotoBank.Api.Validators;

public class UpdateUserDtoValidator : AbstractValidator<UpdateUserDto>
{
    public UpdateUserDtoValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .Matches(@"^[0-9+()\- ]*$").When(x => x.PhoneNumber is not null);
        RuleFor(x => x.TelegramUserId)
            .GreaterThan(0).When(x => x.TelegramUserId.HasValue);
    }
}
