using Helsi.TodoApi.Api.Contracts;
using Helsi.TodoApi.Api.Extensions;
using Helsi.TodoApi.Api.Filters;
using Helsi.TodoApi.Application.Abstractions;
using Helsi.TodoApi.Application.Common;
using Helsi.TodoApi.Application.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Helsi.TodoApi.Api.Controllers
{
    [ApiController]
    [Route("api/todo-lists")]
    [RequireUserId]
    public sealed class TodoListsController(ITodoListService todoListService) : ControllerBase
    {
        private const string UserIdHeader = RequireUserIdAttribute.HeaderName;

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromHeader(Name = UserIdHeader)] Guid userId,
            [FromBody] CreateTodoListRequest request,
            CancellationToken ct)
        {
            Result<TodoListDto> result = await todoListService.CreateAsync(userId, request.Title ?? string.Empty, ct);
            if (result.IsFailure)
            {
                return result.Error!.ToProblemResult();
            }

            return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
        }

        [HttpGet]
        public async Task<IActionResult> GetList(
            [FromHeader(Name = UserIdHeader)] Guid userId,
            [FromQuery] GetTodoListsQuery query,
            CancellationToken ct)
        {
            Result<PagedResult<TodoListSummaryDto>> result = await todoListService.GetListAsync(userId, query.Page, query.PageSize, ct);
            return result.ToActionResult();
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(
            [FromHeader(Name = UserIdHeader)] Guid userId,
            Guid id,
            CancellationToken ct)
        {
            Result<TodoListDto> result = await todoListService.GetByIdAsync(userId, id, ct);
            return result.ToActionResult();
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(
            [FromHeader(Name = UserIdHeader)] Guid userId,
            Guid id,
            [FromBody] UpdateTodoListRequest request,
            CancellationToken ct)
        {
            Result<TodoListDto> result = await todoListService.UpdateAsync(userId, id, request.Title ?? string.Empty, ct);
            return result.ToActionResult();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(
            [FromHeader(Name = UserIdHeader)] Guid userId,
            Guid id,
            CancellationToken ct)
        {
            Result result = await todoListService.DeleteAsync(userId, id, ct);
            return result.ToActionResult();
        }

        [HttpPost("{id:guid}/shares")]
        public async Task<IActionResult> Share(
            [FromHeader(Name = UserIdHeader)] Guid userId,
            Guid id,
            [FromBody] ShareTodoListRequest request,
            CancellationToken ct)
        {
            Result result = await todoListService.ShareAsync(userId, id, request.UserId, ct);
            return result.ToActionResult();
        }

        [HttpGet("{id:guid}/shares")]
        public async Task<IActionResult> GetShares(
            [FromHeader(Name = UserIdHeader)] Guid userId,
            Guid id,
            CancellationToken ct)
        {
            Result<IReadOnlyList<Guid>> result = await todoListService.GetSharesAsync(userId, id, ct);
            return result.ToActionResult();
        }

        [HttpDelete("{id:guid}/shares/{targetUserId:guid}")]
        public async Task<IActionResult> Unshare(
            [FromHeader(Name = UserIdHeader)] Guid userId,
            Guid id,
            Guid targetUserId,
            CancellationToken ct)
        {
            Result result = await todoListService.UnshareAsync(userId, id, targetUserId, ct);
            return result.ToActionResult();
        }
    }
}
