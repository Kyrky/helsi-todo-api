using Helsi.TodoApi.Application.Abstractions;
using Helsi.TodoApi.Infrastructure.Persistence;
using Helsi.TodoApi.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace Helsi.TodoApi.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            string connectionString = configuration["MongoDb:ConnectionString"]!;
            string databaseName = configuration["MongoDb:DatabaseName"]!;

            BsonSerializer.TryRegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

            services.AddSingleton<IMongoClient>(new MongoClient(connectionString));
            services.AddSingleton(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(databaseName));
            services.AddSingleton<MongoDbContext>();
            services.AddScoped<ITodoListRepository, TodoListRepository>();

            services.AddHealthChecks()
                .AddCheck<MongoHealthCheck>("mongodb", timeout: TimeSpan.FromSeconds(5));

            return services;
        }
    }
}
