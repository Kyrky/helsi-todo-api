using FluentValidation;
using Helsi.TodoApi.Application.Common;

namespace Helsi.TodoApi.Api.Validation
{
    internal static class TodoListTitleRules
    {
        public const int MaxLength = 255;

        // Shared by create and update, so both reject the same titles with the same error codes.
        public static IRuleBuilderOptions<T, string?> ValidTodoListTitle<T>(this IRuleBuilderInitial<T, string?> rule)
        {
            return rule
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                    .WithErrorCode(TodoListErrors.TitleEmpty.Code)
                    .WithMessage(TodoListErrors.TitleEmpty.Message)
                .Must(title => title!.Trim().Length <= MaxLength)
                    .WithErrorCode(TodoListErrors.TitleTooLong.Code)
                    .WithMessage(TodoListErrors.TitleTooLong.Message);
        }
    }
}
