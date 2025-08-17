using FluentValidation;

namespace RestaurantSystem.Api.Features.User.Commands.UpdateUserAdminCommand;

public class UpdateUserAdminCommandValidator : AbstractValidator<UpdateUserAdminCommand>
{
    public UpdateUserAdminCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.OrderLimitAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Order limit amount must be greater than or equal to 0")
            .LessThanOrEqualTo(10000).WithMessage("Order limit amount must not exceed 10,000");

        RuleFor(x => x.DiscountPercentage)
            .InclusiveBetween(0, 100).WithMessage("Discount percentage must be between 0 and 100");
    }
}
