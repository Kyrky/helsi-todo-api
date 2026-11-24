using Microsoft.AspNetCore.Mvc;

namespace Helsi.TodoApi.Api.Contracts
{
    public sealed class GetTodoListsQuery
    {
        [FromQuery(Name = "page")]
        public int Page { get; set; } = 1;

        [FromQuery(Name = "pageSize")]
        public int PageSize { get; set; } = 20;
    }
}
