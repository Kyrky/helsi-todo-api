using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Helsi.TodoApi.Api.Filters
{
    // The current user is a transport concern: every request must carry a non-empty GUID in X-User-Id.
    // Checked once here instead of in every endpoint.
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class RequireUserIdAttribute : ActionFilterAttribute
    {
        public const string HeaderName = "X-User-Id";

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            string? headerValue = context.HttpContext.Request.Headers[HeaderName].FirstOrDefault();

            if (!Guid.TryParse(headerValue, out Guid userId) || userId == Guid.Empty)
            {
                ProblemDetails problem = new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "UserId.Required",
                    Detail = $"Header {HeaderName} with a non-empty GUID is required."
                };

                context.Result = new BadRequestObjectResult(problem);
            }
        }
    }
}
