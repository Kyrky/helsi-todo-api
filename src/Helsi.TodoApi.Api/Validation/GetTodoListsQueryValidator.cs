using FluentValidation;
using Helsi.TodoApi.Api.Contracts;
using Helsi.TodoApi.Application.Common;

namespace Helsi.TodoApi.Api.Validation
{
    public sealed class GetTodoListsQueryValidator : AbstractValidator<GetTodoListsQuery>
    {
        public const int MaxPageSize = 100;

        // Keeps (page - 1) * pageSize within int, so a huge page can't overflow the skip.
        public const int MaxPage = int.MaxValue / MaxPageSize;

        public GetTodoListsQueryValidator()
        {
            RuleFor(query => query.Page)
                .InclusiveBetween(1, MaxPage)
                    .WithErrorCode(TodoListErrors.PageInvalid.Code)
                    .WithMessage(TodoListErrors.PageInvalid.Message);

            RuleFor(query => query.PageSize)
                .InclusiveBetween(1, MaxPageSize)
                    .WithErrorCode(TodoListErrors.PageSizeInvalid.Code)
                    .WithMessage(TodoListErrors.PageSizeInvalid.Message);
        }
    }
}
