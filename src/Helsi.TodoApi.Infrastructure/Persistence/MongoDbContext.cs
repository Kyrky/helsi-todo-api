using Helsi.TodoApi.Domain.Entities;
using MongoDB.Driver;

namespace Helsi.TodoApi.Infrastructure.Persistence
{
    internal sealed class MongoDbContext(IMongoDatabase database)
    {
        public IMongoCollection<TodoList> TodoLists { get; } = database.GetCollection<TodoList>("todoLists");
    }
}
