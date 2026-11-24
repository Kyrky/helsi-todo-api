using FluentValidation;
using Helsi.TodoApi.Api.Contracts;

namespace Helsi.TodoApi.Api.Validation
{
    public sealed class CreateTodoListRequestValidator : AbstractValidator<CreateTodoListRequest>
    {
        public CreateTodoListRequestValidator()
        {
            RuleFor(request => request.Title).ValidTodoListTitle();
        }
    }
}
