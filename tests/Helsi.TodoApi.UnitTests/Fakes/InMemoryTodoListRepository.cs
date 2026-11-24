using Helsi.TodoApi.Application.Abstractions;
using Helsi.TodoApi.Application.Common;
using Helsi.TodoApi.Domain.Entities;

namespace Helsi.TodoApi.UnitTests.Fakes
{
    // Behaves like a real store: every read returns a copy, so only repository writes change data.
    internal sealed class InMemoryTodoListRepository : ITodoListRepository
    {
        private readonly Dictionary<Guid, TodoList> _items = new Dictionary<Guid, TodoList>();

        // Writes that actually changed something, like ModifiedCount in MongoDB.
        public int ChangingWrites { get; private set; }

        // Runs right before a write, to simulate another request changing data after the read.
        public Action? BeforeNextWrite { get; set; }

        public void Seed(TodoList todoList)
        {
            _items[todoList.Id] = Copy(todoList);
        }

        public TodoList? Stored(Guid id)
        {
            return _items.TryGetValue(id, out TodoList? todoList) ? Copy(todoList) : null;
        }

        public Task<TodoList?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            return Task.FromResult(Stored(id));
        }

        public Task<PagedResult<TodoList>> GetPageForUserAsync(Guid userId, int page, int pageSize, CancellationToken ct)
        {
            List<TodoList> visible = _items.Values
                .Where(todoList => todoList.OwnerId == userId || todoList.SharedWith.Contains(userId))
                .OrderByDescending(todoList => todoList.CreatedAt)
                .ThenByDescending(todoList => todoList.Id)
                .ToList();

            List<TodoList> items = visible.Skip((page - 1) * pageSize).Take(pageSize).Select(Copy).ToList();

            return Task.FromResult(new PagedResult<TodoList>(items, visible.Count, page, pageSize));
        }

        public Task AddAsync(TodoList todoList, CancellationToken ct)
        {
            _items[todoList.Id] = Copy(todoList);
            return Task.CompletedTask;
        }

        public async Task<TodoList?> RenameAsync(Guid id, Guid userId, string title, CancellationToken ct)
        {
            WriteResult result = await Write(id, userId, todoList =>
            {
                bool changed = todoList.Title != title;
                todoList.Title = title;
                return changed;
            });

            return result == WriteResult.NotFound ? null : Stored(id);
        }

        public Task<WriteResult> AddShareAsync(Guid id, Guid userId, Guid targetUserId, CancellationToken ct)
        {
            return Write(id, userId, todoList =>
            {
                if (todoList.OwnerId == targetUserId || todoList.SharedWith.Contains(targetUserId))
                {
                    return false;
                }

                todoList.SharedWith.Add(targetUserId);
                return true;
            });
        }

        public Task<WriteResult> RemoveShareAsync(Guid id, Guid userId, Guid targetUserId, CancellationToken ct)
        {
            return Write(id, userId, todoList => todoList.SharedWith.Remove(targetUserId));
        }

        public Task<bool> DeleteAsync(Guid id, Guid ownerId, CancellationToken ct)
        {
            RunBeforeWrite();

            bool deleted = _items.TryGetValue(id, out TodoList? todoList) && todoList.OwnerId == ownerId && _items.Remove(id);
            return Task.FromResult(deleted);
        }

        // Mirrors the database filter: the list must exist and the user must still have access at write time.
        private Task<WriteResult> Write(Guid id, Guid userId, Func<TodoList, bool> change)
        {
            RunBeforeWrite();

            if (!_items.TryGetValue(id, out TodoList? todoList)
                || (todoList.OwnerId != userId && !todoList.SharedWith.Contains(userId)))
            {
                return Task.FromResult(WriteResult.NotFound);
            }

            if (!change(todoList))
            {
                return Task.FromResult(WriteResult.Unchanged);
            }

            ChangingWrites++;
            return Task.FromResult(WriteResult.Changed);
        }

        private void RunBeforeWrite()
        {
            Action? beforeWrite = BeforeNextWrite;
            BeforeNextWrite = null;
            beforeWrite?.Invoke();
        }

        private static TodoList Copy(TodoList source)
        {
            return new TodoList
            {
                Id = source.Id,
                Title = source.Title,
                OwnerId = source.OwnerId,
                CreatedAt = source.CreatedAt,
                SharedWith = source.SharedWith.ToList()
            };
        }
    }
}
