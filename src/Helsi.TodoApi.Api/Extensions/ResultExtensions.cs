using Helsi.TodoApi.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace Helsi.TodoApi.Api.Extensions
{
    // The only place that knows how an application error becomes an HTTP status code.
    public static class ResultExtensions
    {
        public static IActionResult ToActionResult(this Result result)
        {
            if (result.IsFailure)
            {
                return result.Error!.ToProblemResult();
            }

            return new NoContentResult();
        }

        public static IActionResult ToActionResult<T>(this Result<T> result)
        {
            if (result.IsFailure)
            {
                return result.Error!.ToProblemResult();
            }

            return new OkObjectResult(result.Value);
        }

        public static IActionResult ToProblemResult(this Error error)
        {
            int statusCode = error.Type switch
            {
                ErrorType.Validation => StatusCodes.Status400BadRequest,
                ErrorType.NotFound => StatusCodes.Status404NotFound,
                ErrorType.Forbidden => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status500InternalServerError
            };

            ProblemDetails problem = new ProblemDetails
            {
                Status = statusCode,
                Title = error.Code,
                Detail = error.Message
            };

            return new ObjectResult(problem) { StatusCode = statusCode };
        }
    }
}
