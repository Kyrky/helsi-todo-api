using Helsi.TodoApi.Application.Abstractions;
using Helsi.TodoApi.Application.Services;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;

namespace Helsi.TodoApi.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            TypeAdapterConfig mappingConfig = TypeAdapterConfig.GlobalSettings;
            mappingConfig.Scan(typeof(DependencyInjection).Assembly);

            services.AddSingleton(mappingConfig);
            services.AddScoped<IMapper, Mapper>();

            services.AddSingleton(TimeProvider.System);
            services.AddScoped<ITodoListService, TodoListService>();
     
            return services;
        }
    }
}
