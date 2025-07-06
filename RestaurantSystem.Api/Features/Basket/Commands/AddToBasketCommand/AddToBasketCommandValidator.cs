﻿using FluentValidation;

namespace RestaurantSystem.Api.Features.Basket.Commands.AddToBasketCommand;

public class AddToBasketCommandValidator : AbstractValidator<AddToBasketCommand>
{
    public AddToBasketCommandValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty().WithMessage("Session ID is required");

        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID is required");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than 0")
            .LessThanOrEqualTo(100).WithMessage("Quantity cannot exceed 100");

        RuleFor(x => x.SpecialInstructions)
            .MaximumLength(500).WithMessage("Special instructions cannot exceed 500 characters");

        //RuleForEach(x => x.SideItems).ChildRules(sideItem =>
        //{
        //    sideItem.RuleFor(si => si.SideItemProductId)
        //        .NotEmpty().WithMessage("Side item product ID is required");

        //    sideItem.RuleFor(si => si.Quantity)
        //        .GreaterThan(0).WithMessage("Side item quantity must be greater than 0")
        //        .LessThanOrEqualTo(10).WithMessage("Side item quantity cannot exceed 10");
        //});
    }
}
