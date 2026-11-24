using FluentValidation;
using Helsi.TodoApi.Api.Contracts;
using Helsi.TodoApi.Application.Common;

namespace Helsi.TodoApi.Api.Validation
{
    public sealed class ShareTodoListRequestValidator : AbstractValidator<ShareTodoListRequest>
    {
        public ShareTodoListRequestValidator()
        {
            RuleFor(request => request.UserId)
                .NotEmpty()
                    .WithErrorCode(TodoListErrors.UserRequired.Code)
                    .WithMessage(TodoListErrors.UserRequired.Message);
        }
    }
}
