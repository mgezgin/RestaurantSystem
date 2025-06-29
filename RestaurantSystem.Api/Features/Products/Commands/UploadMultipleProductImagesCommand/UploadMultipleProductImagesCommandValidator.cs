using FluentValidation;

namespace RestaurantSystem.Api.Features.Products.Commands.UploadMultipleProductImagesCommand;

public class UploadMultipleProductImagesCommandValidator : AbstractValidator<UploadMultipleProductImagesCommand>
{
    public UploadMultipleProductImagesCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID is required");

        RuleFor(x => x.Images)
            .NotEmpty().WithMessage("At least one image file is required")
            .Must(images => images.Count <= 10).WithMessage("Cannot upload more than 10 images at once");

        RuleForEach(x => x.Images)
            .NotNull().WithMessage("Image file cannot be null")
            .Must(file => file.Length > 0).WithMessage("Image file cannot be empty");
    }
}
