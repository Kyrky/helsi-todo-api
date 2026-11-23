namespace Helsi.TodoApi.Application.Common
{
    public class Result
    {
        protected Result(bool isSuccess, Error? error)
        {
            if (isSuccess && error != null)
            {
                throw new ArgumentException("Success result cannot have an error.", nameof(error));
            }

            if (!isSuccess && error == null)
            {
                throw new ArgumentException("Failure result must have an error.", nameof(error));
            }

            IsSuccess = isSuccess;
            Error = error;
        }

        public bool IsSuccess { get; }

        public bool IsFailure
        {
            get { return !IsSuccess; }
        }

        public Error? Error { get; }

        public static Result Success()
        {
            return new Result(true, null);
        }

        public static Result Failure(Error error)
        {
            return new Result(false, error);
        }

        public static Result<T> Success<T>(T value)
        {
            return new Result<T>(value, true, null);
        }

        public static Result<T> Failure<T>(Error error)
        {
            return new Result<T>(default, false, error);
        }
    }
}
