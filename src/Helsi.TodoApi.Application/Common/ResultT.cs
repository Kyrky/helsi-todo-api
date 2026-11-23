namespace Helsi.TodoApi.Application.Common
{
    public sealed class Result<T> : Result
    {
        private readonly T? _value;

        internal Result(T? value, bool isSuccess, Error? error) : base(isSuccess, error)
        {
            _value = value;
        }

        public T Value
        {
            get
            {
                if (IsFailure)
                {
                    throw new InvalidOperationException("Cannot read the value of a failed result.");
                }

                return _value!;
            }
        }
    }
}
