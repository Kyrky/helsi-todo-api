using Helsi.TodoApi.Application.Common;
using Helsi.TodoApi.Domain.Entities;

namespace Helsi.TodoApi.Application.Abstractions
{
    public interface ITodoListRepository
    {
        Task<TodoList?> GetByIdAsync(Guid id, CancellationToken ct);
        Task<PagedResult<TodoList>> GetPageForUserAsync(Guid userId, int page, int pageSize, CancellationToken ct);
        Task AddAsync(TodoList todoList, CancellationToken ct);
        Task UpdateAsync(TodoList todoList, CancellationToken ct);
        Task DeleteAsync(Guid id, CancellationToken ct);
    }
}
