using FluentValidation;
using PhotoBank.Services.Identity;
using PhotoBank.ViewModel.Dto;
using System;

namespace PhotoBank.Api.Validators;

public class UpdateUserDtoValidator : AbstractValidator<UpdateUserDto>
{
    public UpdateUserDtoValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .Matches(@"^[0-9+()\- ]*$").When(x => x.PhoneNumber is not null);
        RuleFor(x => x.TelegramUserId)
            .Custom((value, context) =>
            {
                if (TelegramUserIdParser.TryParse(value, out _, out var error))
                {
                    return;
                }

                if (!string.IsNullOrEmpty(error))
                {
                    context.AddFailure(error);
                }
            });
        RuleFor(x => x.TelegramSendTimeUtc)
            .GreaterThanOrEqualTo(TimeSpan.Zero)
            .LessThan(TimeSpan.FromDays(1))
            .When(x => x.TelegramSendTimeUtc.HasValue);
    }
}
