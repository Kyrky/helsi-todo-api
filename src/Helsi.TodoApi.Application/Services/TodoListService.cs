using Helsi.TodoApi.Application.Abstractions;
using Helsi.TodoApi.Application.Common;
using Helsi.TodoApi.Application.Dtos;
using Helsi.TodoApi.Domain.Entities;
using MapsterMapper;
using Microsoft.Extensions.Logging;

namespace Helsi.TodoApi.Application.Services
{
    internal sealed class TodoListService(
        ITodoListRepository repository,
        TimeProvider timeProvider,
        IMapper mapper,
        ILogger<TodoListService> logger) : ITodoListService
    {
        private const int TitleMaxLength = 255;
        private const int MaxPageSize = 100;

        // Keeps (page - 1) * pageSize within int, so a huge page can't overflow the skip.
        private const int MaxPage = int.MaxValue / MaxPageSize;

        public async Task<Result<TodoListDto>> CreateAsync(Guid userId, string title, CancellationToken ct)
        {
            Error? titleError = ValidateTitle(title);
            if (titleError != null)
            {
                return Result.Failure<TodoListDto>(titleError);
            }

            TodoList todoList = new TodoList
            {
                Id = Guid.NewGuid(),
                Title = title.Trim(),
                OwnerId = userId,
                CreatedAt = TruncateToMilliseconds(timeProvider.GetUtcNow().UtcDateTime)
            };

            await repository.AddAsync(todoList, ct);
            logger.LogInformation("Todo list {TodoListId} created by {UserId}", todoList.Id, userId);

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
            if (page < 1 || page > MaxPage)
            {
                return Result.Failure<PagedResult<TodoListSummaryDto>>(TodoListErrors.PageInvalid);
            }

            if (pageSize < 1 || pageSize > MaxPageSize)
            {
                return Result.Failure<PagedResult<TodoListSummaryDto>>(TodoListErrors.PageSizeInvalid);
            }

            PagedResult<TodoList> todoLists = await repository.GetPageForUserAsync(userId, page, pageSize, ct);
            List<TodoListSummaryDto> items = mapper.Map<List<TodoListSummaryDto>>(todoLists.Items);
            PagedResult<TodoListSummaryDto> result = new PagedResult<TodoListSummaryDto>(items, todoLists.TotalCount, todoLists.Page, todoLists.PageSize);

            return Result.Success(result);
        }

        public async Task<Result<TodoListDto>> UpdateAsync(Guid userId, Guid id, string title, CancellationToken ct)
        {
            Error? titleError = ValidateTitle(title);
            if (titleError != null)
            {
                return Result.Failure<TodoListDto>(titleError);
            }

            TodoList? todoList = await repository.RenameAsync(id, userId, title.Trim(), ct);
            if (todoList == null)
            {
                return Result.Failure<TodoListDto>(TodoListErrors.NotFound);
            }

            logger.LogInformation("Todo list {TodoListId} renamed by {UserId}", todoList.Id, userId);

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
                logger.LogWarning("User {UserId} tried to delete todo list {TodoListId} they do not own", userId, todoList.Id);
                return Result.Failure(TodoListErrors.OnlyOwnerCanDelete);
            }

            if (!await repository.DeleteAsync(todoList.Id, userId, ct))
            {
                return Result.Failure(TodoListErrors.NotFound);
            }

            logger.LogInformation("Todo list {TodoListId} deleted by {UserId}", todoList.Id, userId);

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
            WriteResult write = await repository.AddShareAsync(todoList.Id, userId, targetUserId, ct);
            if (write == WriteResult.NotFound)
            {
                return Result.Failure(TodoListErrors.NotFound);
            }

            if (write == WriteResult.Changed)
            {
                logger.LogInformation("Todo list {TodoListId} shared with {TargetUserId} by {UserId}", todoList.Id, targetUserId, userId);
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
            WriteResult write = await repository.RemoveShareAsync(id, userId, targetUserId, ct);
            if (write == WriteResult.NotFound)
            {
                return Result.Failure(TodoListErrors.NotFound);
            }

            if (write == WriteResult.Changed)
            {
                logger.LogInformation("Todo list {TodoListId} unshared from {TargetUserId} by {UserId}", id, targetUserId, userId);
            }

            return Result.Success();
        }

        // "Not found" and "no access" look the same from outside (404),
        // so a stranger cannot find out that the list exists.
        private async Task<TodoList?> GetAccessibleTodoListAsync(Guid userId, Guid id, CancellationToken ct)
        {
            TodoList? todoList = await repository.GetByIdAsync(id, ct);
            if (todoList == null)
            {
                logger.LogDebug("Todo list {TodoListId} not found", id);
                return null;
            }

            if (!HasAccess(todoList, userId))
            {
                logger.LogDebug("User {UserId} has no access to todo list {TodoListId}", userId, id);
                return null;
            }

            return todoList;
        }

        private static Error? ValidateTitle(string? title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return TodoListErrors.TitleEmpty;
            }

            if (title.Trim().Length > TitleMaxLength)
            {
                return TodoListErrors.TitleTooLong;
            }

            return null;
        }

        // MongoDB stores dates in milliseconds; trimming here makes the 201 response match later reads.
        private static DateTime TruncateToMilliseconds(DateTime value)
        {
            return value.AddTicks(-(value.Ticks % TimeSpan.TicksPerMillisecond));
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
