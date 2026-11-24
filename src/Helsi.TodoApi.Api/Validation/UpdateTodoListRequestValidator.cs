using FluentValidation;
using Helsi.TodoApi.Api.Contracts;

namespace Helsi.TodoApi.Api.Validation
{
    public sealed class UpdateTodoListRequestValidator : AbstractValidator<UpdateTodoListRequest>
    {
        public UpdateTodoListRequestValidator()
        {
            RuleFor(request => request.Title).ValidTodoListTitle();
        }
    }
}
