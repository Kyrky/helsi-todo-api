namespace Helsi.TodoApi.Application.Common
{
    public sealed record Error(string Code, string Message, ErrorType Type)
    {
        public static Error Validation(string code, string message)
        {
            return new Error(code, message, ErrorType.Validation);
        }

        public static Error NotFound(string code, string message)
        {
            return new Error(code, message, ErrorType.NotFound);
        }

        public static Error Forbidden(string code, string message)
        {
            return new Error(code, message, ErrorType.Forbidden);
        }
    }
}
