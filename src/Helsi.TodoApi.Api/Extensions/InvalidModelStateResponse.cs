using Helsi.TodoApi.Api.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Helsi.TodoApi.Api.Extensions
{
    // Binding errors (malformed GUID, wrong JSON type) get the same ProblemDetails shape as our own errors.
    public static class InvalidModelStateResponse
    {
        public const string InvalidRequestCode = "Request.Invalid";

        public static IActionResult Create(ActionContext context)
        {
            bool badUserId = context.ModelState.TryGetValue(RequireUserIdAttribute.HeaderName, out ModelStateEntry? entry)
                && entry.Errors.Count > 0;

            // JSON errors ("$.title") explain the cause; the "request is required" error is only a consequence.
            string? firstMessage = context.ModelState
                .OrderBy(pair => pair.Key.StartsWith('$') ? 0 : 1)
                .SelectMany(pair => pair.Value?.Errors ?? Enumerable.Empty<ModelError>())
                .Select(error => error.ErrorMessage)
                .FirstOrDefault(message => !string.IsNullOrEmpty(message));

            ProblemDetails problem = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = badUserId ? "UserId.Required" : InvalidRequestCode,
                Detail = badUserId
                    ? $"Header {RequireUserIdAttribute.HeaderName} with a non-empty GUID is required."
                    : firstMessage ?? "The request is malformed."
            };

            return new BadRequestObjectResult(problem);
        }
    }
}
