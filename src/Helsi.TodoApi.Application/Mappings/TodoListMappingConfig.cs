using Helsi.TodoApi.Application.Dtos;
using Helsi.TodoApi.Domain.Entities;
using Mapster;

namespace Helsi.TodoApi.Application.Mappings
{
    internal sealed class TodoListMappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<TodoList, TodoListDto>();
            config.NewConfig<TodoList, TodoListSummaryDto>();
        }
    }
}
