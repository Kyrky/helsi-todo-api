namespace Helsi.TodoApi.Application.Common
{
    // NotFound: the list is gone or the user lost access before the write landed.
    public enum WriteResult
    {
        NotFound,
        Unchanged,
        Changed
    }
}
