using FluentValidation;
using PhotoBank.ViewModel.Dto;

namespace PhotoBank.Api.Validators;

public class PageRequestValidator : AbstractValidator<PageRequest>
{
    public PageRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, PageRequest.MaxPageSize);
    }
}
