using Helsi.TodoApi.Application.Common;
using Helsi.TodoApi.Application.Dtos;

namespace Helsi.TodoApi.Application.Abstractions
{
    // userId is always the current user (comes from the X-User-Id header).
    public interface ITodoListService
    {
        Task<Result<TodoListDto>> CreateAsync(Guid userId, string title, CancellationToken ct);
        Task<Result<TodoListDto>> GetByIdAsync(Guid userId, Guid id, CancellationToken ct);
        Task<Result<PagedResult<TodoListSummaryDto>>> GetListAsync(Guid userId, int page, int pageSize, CancellationToken ct);
        Task<Result<TodoListDto>> UpdateAsync(Guid userId, Guid id, string title, CancellationToken ct);
        Task<Result> DeleteAsync(Guid userId, Guid id, CancellationToken ct);

        Task<Result> ShareAsync(Guid userId, Guid id, Guid targetUserId, CancellationToken ct);
        Task<Result<IReadOnlyList<Guid>>> GetSharesAsync(Guid userId, Guid id, CancellationToken ct);
        Task<Result> UnshareAsync(Guid userId, Guid id, Guid targetUserId, CancellationToken ct);
    }
}
