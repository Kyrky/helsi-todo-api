using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Helsi.TodoApi.Api.Filters
{
    // Runs the FluentValidation validator of every action argument that has one.
    // Replaces FluentValidation.AspNetCore auto-validation, which is deprecated.
    public sealed class ValidationFilter(IServiceProvider services) : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            foreach (object? argument in context.ActionArguments.Values)
            {
                if (argument == null)
                {
                    continue;
                }

                Type validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
                if (services.GetService(validatorType) is not IValidator validator)
                {
                    continue;
                }

                ValidationResult result = await validator.ValidateAsync(
                    new ValidationContext<object>(argument), context.HttpContext.RequestAborted);
                if (!result.IsValid)
                {
                    context.Result = ToProblem(result);
                    return;
                }
            }

            await next();
        }

        // Same shape as the service errors: the first error code in title, every failure under "errors".
        private static IActionResult ToProblem(ValidationResult result)
        {
            ValidationFailure first = result.Errors[0];
            ProblemDetails problem = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = first.ErrorCode,
                Detail = first.ErrorMessage
            };
            problem.Extensions["errors"] = result.Errors
                .GroupBy(failure => failure.PropertyName)
                .ToDictionary(group => group.Key, group => group.Select(failure => failure.ErrorCode).ToArray());

            return new BadRequestObjectResult(problem);
        }
    }
}
