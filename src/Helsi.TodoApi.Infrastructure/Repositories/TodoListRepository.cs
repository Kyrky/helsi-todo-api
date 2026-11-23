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

        public async Task UpdateAsync(TodoList todoList, CancellationToken ct)
        {
            await context.TodoLists.ReplaceOneAsync(ById(todoList.Id), todoList, cancellationToken: ct);
        }

        public async Task DeleteAsync(Guid id, CancellationToken ct)
        {
            await context.TodoLists.DeleteOneAsync(ById(id), ct);
        }

        private static FilterDefinition<TodoList> ById(Guid id)
        {
            return Builders<TodoList>.Filter.Eq(document => document.Id, id);
        }
    }
}
