using Helsi.TodoApi.Domain.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Helsi.TodoApi.Infrastructure.Persistence
{
    // Creates indexes once at startup. Creating an index that already exists is a no-op in MongoDB,
    // so this is safe on every start. If MongoDB is not reachable yet, the API still starts
    // and /health reports the problem instead of the whole process crashing.
    internal sealed class MongoIndexInitializer(MongoDbContext context, ILogger<MongoIndexInitializer> logger) : IHostedService
    {
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            IndexKeysDefinitionBuilder<TodoList> keys = Builders<TodoList>.IndexKeys;

            // Both indexes serve the feed query: "owner OR in sharedWith", newest first.
            List<CreateIndexModel<TodoList>> indexes = new List<CreateIndexModel<TodoList>>
            {
                new CreateIndexModel<TodoList>(
                    keys.Ascending(todoList => todoList.OwnerId).Descending(todoList => todoList.CreatedAt),
                    new CreateIndexOptions { Name = "ownerId_createdAt" }),
                new CreateIndexModel<TodoList>(
                    keys.Ascending(todoList => todoList.SharedWith).Descending(todoList => todoList.CreatedAt),
                    new CreateIndexOptions { Name = "sharedWith_createdAt" })
            };

            try
            {
                await context.TodoLists.Indexes.CreateManyAsync(indexes, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not create MongoDB indexes at startup.");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
