namespace Helsi.TodoApi.Domain.Entities
{
    public sealed class TodoList
    {
        public Guid Id { get; init; }
        public string Title { get; set; } = string.Empty;
        public Guid OwnerId { get; init; }
        public DateTime CreatedAt { get; init; }
        public List<Guid> SharedWith { get; set; } = new List<Guid>();
    }
}
