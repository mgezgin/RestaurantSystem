using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using RestaurantSystem.Api.Common.Models;

namespace RestaurantSystem.Api.Common.Behaviors
{
    public class RequestValidationBehavior
    {
        private readonly IServiceProvider _serviceProvider;

        public RequestValidationBehavior(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            foreach (var (_, value) in context.ActionArguments)
            {
                if (value == null) continue;

                var validatorType = typeof(IValidator<>).MakeGenericType(value.GetType());
                if (_serviceProvider.GetService(validatorType) is not IValidator validator) continue;

                var validationContext = new ValidationContext<object>(value);
                var validationResult = validator.Validate(validationContext);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    var response = ApiResponse<object>.Failure(errors, "Validation failed");
                    context.Result = new BadRequestObjectResult(response);
                    return;
                }
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // Method intentionally left empty
        }
    }
}
