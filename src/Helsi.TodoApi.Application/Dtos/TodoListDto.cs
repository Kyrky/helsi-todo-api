namespace Helsi.TodoApi.Application.Dtos
{
    public sealed record TodoListDto(
        Guid Id,
        string Title,
        Guid OwnerId,
        DateTime CreatedAt,
        IReadOnlyList<Guid> SharedWith);
}
