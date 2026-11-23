using Helsi.TodoApi.Domain.Entities;
using MongoDB.Driver;

namespace Helsi.TodoApi.Infrastructure.Persistence
{
    internal sealed class MongoDbContext
    {
        public MongoDbContext(IMongoDatabase database)
        {
            TodoLists = database.GetCollection<TodoList>("todolists");
        }
        public IMongoCollection<TodoList> TodoLists { get; }
    }
}
