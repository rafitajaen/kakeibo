using FluentValidation;

namespace Kakeibo.Api.Features.Transactions.Categories.CreateCategory;

public sealed class CreateCategoryValidator : AbstractValidator<CreateCategoryEndpoint.CreateCategoryRequest>
{
    public CreateCategoryValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(50);
    }
}
