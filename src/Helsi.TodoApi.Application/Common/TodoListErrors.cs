namespace Helsi.TodoApi.Application.Common
{
    public static class TodoListErrors
    {
        public static readonly Error TitleEmpty =
            Error.Validation("TodoList.TitleEmpty", "Title is required.");

        public static readonly Error TitleTooLong =
            Error.Validation("TodoList.TitleTooLong", "Title must be at most 255 characters.");

        public static readonly Error OwnerRequired =
            Error.Validation("TodoList.OwnerRequired", "Owner id is required.");

        public static readonly Error UserRequired =
            Error.Validation("TodoList.UserRequired", "User id is required.");

        public static readonly Error CannotShareWithOwner =
            Error.Validation("TodoList.CannotShareWithOwner", "The list cannot be shared with its owner.");

        public static readonly Error NotFound =
            Error.NotFound("TodoList.NotFound", "Todo list was not found.");

        public static readonly Error OnlyOwnerCanDelete =
            Error.Forbidden("TodoList.OnlyOwnerCanDelete", "Only the owner can delete the list.");
    }
}
