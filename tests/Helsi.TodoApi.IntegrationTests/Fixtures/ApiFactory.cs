using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.MongoDb;

namespace Helsi.TodoApi.IntegrationTests.Fixtures
{
    // Real API in memory against a throwaway MongoDB container.
    public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private readonly MongoDbContainer _mongo = new MongoDbBuilder()
            .WithImage("mongo:7")
            .Build();

        public async Task InitializeAsync()
        {
            await _mongo.StartAsync();
        }

        public new async Task DisposeAsync()
        {
            await _mongo.DisposeAsync();
            await base.DisposeAsync();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("MongoDb:ConnectionString", _mongo.GetConnectionString());
            builder.UseSetting("MongoDb:DatabaseName", "todolists_tests");
        }
    }

    // Shared by all test classes: Serilog's static logger can only be set up once per process.
    [CollectionDefinition(Name)]
    public sealed class ApiCollection : ICollectionFixture<ApiFactory>
    {
        public const string Name = "api";
    }
}
