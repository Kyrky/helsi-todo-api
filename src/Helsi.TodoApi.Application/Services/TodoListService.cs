using Helsi.TodoApi.Application.Abstractions;
using Helsi.TodoApi.Application.Common;
using Helsi.TodoApi.Application.Dtos;
using Helsi.TodoApi.Domain.Entities;
using MapsterMapper;

namespace Helsi.TodoApi.Application.Services
{
    internal sealed class TodoListService(ITodoListRepository repository, TimeProvider timeProvider, IMapper mapper) : ITodoListService
    {
        public async Task<Result<TodoListDto>> CreateAsync(Guid userId, string title, CancellationToken ct)
        {
            TodoList todoList = new TodoList
            {
                Id = Guid.NewGuid(),
                Title = title.Trim(),
                OwnerId = userId,
                CreatedAt = timeProvider.GetUtcNow().UtcDateTime
            };

            await repository.AddAsync(todoList, ct);

            return Result.Success(mapper.Map<TodoListDto>(todoList));
        }

        public async Task<Result<TodoListDto>> GetByIdAsync(Guid userId, Guid id, CancellationToken ct)
        {
            TodoList? todoList = await GetAccessibleTodoListAsync(userId, id, ct);
            if (todoList == null)
            {
                return Result.Failure<TodoListDto>(TodoListErrors.NotFound);
            }

            return Result.Success(mapper.Map<TodoListDto>(todoList));
        }

        public async Task<Result<PagedResult<TodoListSummaryDto>>> GetListAsync(Guid userId, int page, int pageSize, CancellationToken ct)
        {
            PagedResult<TodoList> todoLists = await repository.GetPageForUserAsync(userId, page, pageSize, ct);
            List<TodoListSummaryDto> items = mapper.Map<List<TodoListSummaryDto>>(todoLists.Items);
            PagedResult<TodoListSummaryDto> result = new PagedResult<TodoListSummaryDto>(items, todoLists.TotalCount, todoLists.Page, todoLists.PageSize);

            return Result.Success(result);
        }

        public async Task<Result<TodoListDto>> UpdateAsync(Guid userId, Guid id, string title, CancellationToken ct)
        {
            TodoList? todoList = await GetAccessibleTodoListAsync(userId, id, ct);
            if (todoList == null)
            {
                return Result.Failure<TodoListDto>(TodoListErrors.NotFound);
            }

            todoList.Title = title.Trim();
            await repository.UpdateAsync(todoList, ct);

            return Result.Success(mapper.Map<TodoListDto>(todoList));
        }

        public async Task<Result> DeleteAsync(Guid userId, Guid id, CancellationToken ct)
        {
            TodoList? todoList = await GetAccessibleTodoListAsync(userId, id, ct);
            if (todoList == null)
            {
                return Result.Failure(TodoListErrors.NotFound);
            }

            if (!IsOwner(todoList, userId))
            {
                return Result.Failure(TodoListErrors.OnlyOwnerCanDelete);
            }

            await repository.DeleteAsync(todoList.Id, ct);

            return Result.Success();
        }

        public async Task<Result> ShareAsync(Guid userId, Guid id, Guid targetUserId, CancellationToken ct)
        {
            TodoList? todoList = await GetAccessibleTodoListAsync(userId, id, ct);
            if (todoList == null)
            {
                return Result.Failure(TodoListErrors.NotFound);
            }

            if (targetUserId == Guid.Empty)
            {
                return Result.Failure(TodoListErrors.UserRequired);
            }

            if (IsOwner(todoList, targetUserId))
            {
                return Result.Failure(TodoListErrors.CannotShareWithOwner);
            }

            // Sharing twice with the same user changes nothing and is still a success.
            if (!todoList.SharedWith.Contains(targetUserId))
            {
                todoList.SharedWith.Add(targetUserId);
                await repository.UpdateAsync(todoList, ct);
            }

            return Result.Success();
        }

        public async Task<Result<IReadOnlyList<Guid>>> GetSharesAsync(Guid userId, Guid id, CancellationToken ct)
        {
            TodoList? todoList = await GetAccessibleTodoListAsync(userId, id, ct);
            if (todoList == null)
            {
                return Result.Failure<IReadOnlyList<Guid>>(TodoListErrors.NotFound);
            }

            IReadOnlyList<Guid> shares = todoList.SharedWith.ToList();

            return Result.Success(shares);
        }

        public async Task<Result> UnshareAsync(Guid userId, Guid id, Guid targetUserId, CancellationToken ct)
        {
            TodoList? todoList = await GetAccessibleTodoListAsync(userId, id, ct);
            if (todoList == null)
            {
                return Result.Failure(TodoListErrors.NotFound);
            }

            if (todoList.SharedWith.Remove(targetUserId))
            {
                await repository.UpdateAsync(todoList, ct);
            }

            return Result.Success();
        }

        // "Not found" and "no access" look the same from outside (404),
        // so a stranger cannot find out that the list exists.
        private async Task<TodoList?> GetAccessibleTodoListAsync(Guid userId, Guid id, CancellationToken ct)
        {
            TodoList? todoList = await repository.GetByIdAsync(id, ct);
            if (todoList == null || !HasAccess(todoList, userId))
            {
                return null;
            }

            return todoList;
        }

        private static bool HasAccess(TodoList todoList, Guid userId)
        {
            return IsOwner(todoList, userId) || todoList.SharedWith.Contains(userId);
        }

        private static bool IsOwner(TodoList todoList, Guid userId)
        {
            return todoList.OwnerId == userId;
        }
    }
}
