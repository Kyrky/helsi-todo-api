using Helsi.TodoApi.Application.Abstractions;
using Helsi.TodoApi.Application.Common;
using Helsi.TodoApi.Domain.Entities;
using Helsi.TodoApi.Infrastructure.Persistence;
using MongoDB.Driver;

namespace Helsi.TodoApi.Infrastructure.Repositories
{
    internal sealed class TodoListRepository(MongoDbContext context) : ITodoListRepository
    {
        public async Task<TodoList?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            return await context.TodoLists
                .Find(ById(id))
                .FirstOrDefaultAsync(ct);
        }

        public async Task<PagedResult<TodoList>> GetPageForUserAsync(Guid userId, int page, int pageSize, CancellationToken ct)
        {
            FilterDefinition<TodoList> filter = Builders<TodoList>.Filter.Or(
                Builders<TodoList>.Filter.Eq(todoList => todoList.OwnerId, userId),
                Builders<TodoList>.Filter.AnyEq(todoList => todoList.SharedWith, userId));

            long totalCount = await context.TodoLists.CountDocumentsAsync(filter, cancellationToken: ct);

            List<TodoList> items = await context.TodoLists
                .Find(filter)
                .SortByDescending(todoList => todoList.CreatedAt)
                .ThenByDescending(todoList => todoList.Id)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync(ct);

            return new PagedResult<TodoList>(items, totalCount, page, pageSize);
        }

        public async Task AddAsync(TodoList todoList, CancellationToken ct)
        {
            await context.TodoLists.InsertOneAsync(todoList, cancellationToken: ct);
        }

        public async Task<TodoList?> RenameAsync(Guid id, Guid userId, string title, CancellationToken ct)
        {
            UpdateDefinition<TodoList> update = Builders<TodoList>.Update.Set(todoList => todoList.Title, title);
            FindOneAndUpdateOptions<TodoList> options = new FindOneAndUpdateOptions<TodoList> { ReturnDocument = ReturnDocument.After };

            return await context.TodoLists.FindOneAndUpdateAsync(AccessibleBy(id, userId), update, options, ct);
        }

        // $addToSet skips a user who is already there, so repeated or parallel shares never duplicate.
        public async Task<WriteResult> AddShareAsync(Guid id, Guid userId, Guid targetUserId, CancellationToken ct)
        {
            FilterDefinition<TodoList> filter = Builders<TodoList>.Filter.And(
                AccessibleBy(id, userId),
                Builders<TodoList>.Filter.Ne(todoList => todoList.OwnerId, targetUserId));
            UpdateDefinition<TodoList> update = Builders<TodoList>.Update.AddToSet(todoList => todoList.SharedWith, targetUserId);

            UpdateResult result = await context.TodoLists.UpdateOneAsync(filter, update, cancellationToken: ct);
            return ToWriteResult(result);
        }

        public async Task<WriteResult> RemoveShareAsync(Guid id, Guid userId, Guid targetUserId, CancellationToken ct)
        {
            UpdateDefinition<TodoList> update = Builders<TodoList>.Update.Pull(todoList => todoList.SharedWith, targetUserId);

            UpdateResult result = await context.TodoLists.UpdateOneAsync(AccessibleBy(id, userId), update, cancellationToken: ct);
            return ToWriteResult(result);
        }

        public async Task<bool> DeleteAsync(Guid id, Guid ownerId, CancellationToken ct)
        {
            FilterDefinition<TodoList> filter = Builders<TodoList>.Filter.And(
                ById(id),
                Builders<TodoList>.Filter.Eq(todoList => todoList.OwnerId, ownerId));

            DeleteResult result = await context.TodoLists.DeleteOneAsync(filter, ct);
            return result.DeletedCount > 0;
        }

        private static WriteResult ToWriteResult(UpdateResult result)
        {
            if (result.MatchedCount == 0)
            {
                return WriteResult.NotFound;
            }

            return result.ModifiedCount > 0 ? WriteResult.Changed : WriteResult.Unchanged;
        }

        // The same access rule as in the service, checked by the database at write time.
        private static FilterDefinition<TodoList> AccessibleBy(Guid id, Guid userId)
        {
            return Builders<TodoList>.Filter.And(
                ById(id),
                Builders<TodoList>.Filter.Or(
                    Builders<TodoList>.Filter.Eq(todoList => todoList.OwnerId, userId),
                    Builders<TodoList>.Filter.AnyEq(todoList => todoList.SharedWith, userId)));
        }

        private static FilterDefinition<TodoList> ById(Guid id)
        {
            return Builders<TodoList>.Filter.Eq(document => document.Id, id);
        }
    }
}
