using Helsi.TodoApi.Application.Abstractions;
using Helsi.TodoApi.Infrastructure.Persistence;
using Helsi.TodoApi.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
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

            ConfigureMongoSerialization();

            services.AddSingleton<IMongoClient>(new MongoClient(connectionString));
            services.AddSingleton(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(databaseName));
            services.AddSingleton<MongoDbContext>();
            services.AddHostedService<MongoIndexInitializer>();
            services.AddScoped<ITodoListRepository, TodoListRepository>();

            services.AddHealthChecks()
                .AddCheck<MongoHealthCheck>("mongodb", timeout: TimeSpan.FromSeconds(5));

            return services;
        }

        // Guid as UUID, camelCase field names, unknown fields ignored.
        private static void ConfigureMongoSerialization()
        {
            BsonSerializer.TryRegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

            ConventionPack conventions = new ConventionPack
            {
                new CamelCaseElementNameConvention(),
                new IgnoreExtraElementsConvention(true)
            };
            ConventionRegistry.Register("TodoApiConventions", conventions, type => true);
        }
    }
}
