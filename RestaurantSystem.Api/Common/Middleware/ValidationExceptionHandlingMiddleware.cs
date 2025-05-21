using FluentValidation;
using RestaurantSystem.Api.Common.Models;
using System.Net;
using System.Text.Json;

namespace RestaurantSystem.Api.Common.Middleware
{
    public class ValidationExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ValidationExceptionHandlingMiddleware> _logger;

        public ValidationExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ValidationExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error occurred");
                await HandleValidationExceptionAsync(context, ex);
            }
        }

        private static async Task HandleValidationExceptionAsync(HttpContext context, ValidationException exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

            var errors = exception.Errors
                .Select(e => e.ErrorMessage)
                .ToList();

            var response = ApiResponse<object>.Failure(errors, "Validation failed");

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var jsonResponse = JsonSerializer.Serialize(response, jsonOptions);
            await context.Response.WriteAsync(jsonResponse);
        }
    }

    // Extension method to add the middleware to the HTTP request pipeline
    public static class ValidationExceptionHandlingMiddlewareExtensions
    {
        public static IApplicationBuilder UseValidationExceptionHandling(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ValidationExceptionHandlingMiddleware>();
        }
    }
}
