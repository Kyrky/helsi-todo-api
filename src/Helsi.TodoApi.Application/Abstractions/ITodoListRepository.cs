using Helsi.TodoApi.Application.Common;
using Helsi.TodoApi.Domain.Entities;

namespace Helsi.TodoApi.Application.Abstractions
{
    // Writes change one field and re-check access in the same atomic operation,
    // so a user who loses access mid-request can't write, and parallel edits don't overwrite each other.
    public interface ITodoListRepository
    {
        Task<TodoList?> GetByIdAsync(Guid id, CancellationToken ct);
        Task<PagedResult<TodoList>> GetPageForUserAsync(Guid userId, int page, int pageSize, CancellationToken ct);
        Task AddAsync(TodoList todoList, CancellationToken ct);

        // Returns the updated list, or null if it's gone or the user has no access.
        Task<TodoList?> RenameAsync(Guid id, Guid userId, string title, CancellationToken ct);
        Task<WriteResult> AddShareAsync(Guid id, Guid userId, Guid targetUserId, CancellationToken ct);
        Task<WriteResult> RemoveShareAsync(Guid id, Guid userId, Guid targetUserId, CancellationToken ct);
        Task<bool> DeleteAsync(Guid id, Guid ownerId, CancellationToken ct);
    }
}
