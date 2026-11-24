using Helsi.TodoApi.Application.Common;
using Helsi.TodoApi.Application.Dtos;
using Helsi.TodoApi.Application.Mappings;
using Helsi.TodoApi.Application.Services;
using Helsi.TodoApi.Domain.Entities;
using Helsi.TodoApi.UnitTests.Fakes;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace Helsi.TodoApi.UnitTests.Services
{
    public sealed class TodoListServiceTests
    {
        private static readonly Guid Owner = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid Friend = Guid.Parse("22222222-2222-2222-2222-222222222222");
        private static readonly Guid Stranger = Guid.Parse("33333333-3333-3333-3333-333333333333");
        private static readonly Guid Another = Guid.Parse("44444444-4444-4444-4444-444444444444");

        private readonly InMemoryTodoListRepository _repository = new InMemoryTodoListRepository();
        private readonly FakeTimeProvider _time = new FakeTimeProvider(new DateTimeOffset(2026, 11, 24, 10, 0, 0, TimeSpan.Zero));
        private readonly TodoListService _service;

        public TodoListServiceTests()
        {
            TypeAdapterConfig config = new TypeAdapterConfig();
            config.Scan(typeof(TodoListMappingConfig).Assembly);

            _service = new TodoListService(_repository, _time, new Mapper(config), NullLogger<TodoListService>.Instance);
        }

        [Fact]
        public async Task Create_ValidTitle_SavesListOwnedByCaller()
        {
            Result<TodoListDto> result = await _service.CreateAsync(Owner, "  Groceries  ", CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal("Groceries", result.Value.Title);
            Assert.Equal(Owner, result.Value.OwnerId);
            Assert.Equal(_time.GetUtcNow().UtcDateTime, result.Value.CreatedAt);
            Assert.Empty(result.Value.SharedWith);
            Assert.NotNull(_repository.Stored(result.Value.Id));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Create_EmptyTitle_ReturnsTitleEmpty(string title)
        {
            Result<TodoListDto> result = await _service.CreateAsync(Owner, title, CancellationToken.None);

            Assert.Equal(TodoListErrors.TitleEmpty, result.Error);
        }

        [Fact]
        public async Task Create_TitleLongerThan255_ReturnsTitleTooLong()
        {
            Result<TodoListDto> result = await _service.CreateAsync(Owner, new string('a', 256), CancellationToken.None);

            Assert.Equal(TodoListErrors.TitleTooLong, result.Error);
        }

        [Fact]
        public async Task Create_TitleOf255Characters_Succeeds()
        {
            Result<TodoListDto> result = await _service.CreateAsync(Owner, new string('a', 255), CancellationToken.None);

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task GetById_OwnerAndSharedUser_CanRead()
        {
            TodoList list = SeedList(sharedWith: Friend);

            Assert.True((await _service.GetByIdAsync(Owner, list.Id, CancellationToken.None)).IsSuccess);
            Assert.True((await _service.GetByIdAsync(Friend, list.Id, CancellationToken.None)).IsSuccess);
        }

        [Fact]
        public async Task GetById_Stranger_GetsNotFound()
        {
            TodoList list = SeedList();

            Result<TodoListDto> result = await _service.GetByIdAsync(Stranger, list.Id, CancellationToken.None);

            Assert.Equal(TodoListErrors.NotFound, result.Error);
        }

        [Fact]
        public async Task GetById_MissingList_GetsNotFound()
        {
            Result<TodoListDto> result = await _service.GetByIdAsync(Owner, Guid.NewGuid(), CancellationToken.None);

            Assert.Equal(TodoListErrors.NotFound, result.Error);
        }

        [Fact]
        public async Task GetList_ReturnsOwnAndSharedListsNewestFirst()
        {
            TodoList older = SeedList(title: "Own, older", createdAt: _time.GetUtcNow().AddHours(-2));
            TodoList shared = SeedList(title: "Shared, newer", owner: Another, sharedWith: Owner, createdAt: _time.GetUtcNow().AddHours(-1));
            SeedList(title: "Not mine", owner: Stranger);

            Result<PagedResult<TodoListSummaryDto>> result = await _service.GetListAsync(Owner, 1, 20, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value.TotalCount);
            Assert.Equal(new[] { shared.Id, older.Id }, result.Value.Items.Select(item => item.Id));
        }

        [Theory]
        [InlineData(0, 20)]
        [InlineData(-1, 20)]
        public async Task GetList_PageBelowOne_ReturnsPageInvalid(int page, int pageSize)
        {
            Result<PagedResult<TodoListSummaryDto>> result = await _service.GetListAsync(Owner, page, pageSize, CancellationToken.None);

            Assert.Equal(TodoListErrors.PageInvalid, result.Error);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(101)]
        public async Task GetList_PageSizeOutOfRange_ReturnsPageSizeInvalid(int pageSize)
        {
            Result<PagedResult<TodoListSummaryDto>> result = await _service.GetListAsync(Owner, 1, pageSize, CancellationToken.None);

            Assert.Equal(TodoListErrors.PageSizeInvalid, result.Error);
        }

        [Fact]
        public async Task Update_SharedUser_CanRename()
        {
            TodoList list = SeedList(sharedWith: Friend);

            Result<TodoListDto> result = await _service.UpdateAsync(Friend, list.Id, " Food ", CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal("Food", _repository.Stored(list.Id)!.Title);
            Assert.Equal(Owner, _repository.Stored(list.Id)!.OwnerId);
        }

        [Fact]
        public async Task Update_Stranger_GetsNotFoundAndNothingChanges()
        {
            TodoList list = SeedList();

            Result<TodoListDto> result = await _service.UpdateAsync(Stranger, list.Id, "Hacked", CancellationToken.None);

            Assert.Equal(TodoListErrors.NotFound, result.Error);
            Assert.Equal(list.Title, _repository.Stored(list.Id)!.Title);
        }

        [Fact]
        public async Task Update_EmptyTitle_ReturnsTitleEmpty()
        {
            TodoList list = SeedList();

            Result<TodoListDto> result = await _service.UpdateAsync(Owner, list.Id, " ", CancellationToken.None);

            Assert.Equal(TodoListErrors.TitleEmpty, result.Error);
        }

        [Fact]
        public async Task Delete_Owner_RemovesList()
        {
            TodoList list = SeedList();

            Result result = await _service.DeleteAsync(Owner, list.Id, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Null(_repository.Stored(list.Id));
        }

        [Fact]
        public async Task Delete_SharedUser_GetsForbiddenAndListStays()
        {
            TodoList list = SeedList(sharedWith: Friend);

            Result result = await _service.DeleteAsync(Friend, list.Id, CancellationToken.None);

            Assert.Equal(TodoListErrors.OnlyOwnerCanDelete, result.Error);
            Assert.NotNull(_repository.Stored(list.Id));
        }

        [Fact]
        public async Task Delete_Stranger_GetsNotFound()
        {
            TodoList list = SeedList();

            Result result = await _service.DeleteAsync(Stranger, list.Id, CancellationToken.None);

            Assert.Equal(TodoListErrors.NotFound, result.Error);
        }

        [Fact]
        public async Task Share_Owner_AddsUser()
        {
            TodoList list = SeedList();

            Result result = await _service.ShareAsync(Owner, list.Id, Friend, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(new[] { Friend }, _repository.Stored(list.Id)!.SharedWith);
        }

        [Fact]
        public async Task Share_SharedUser_CanShareFurther()
        {
            TodoList list = SeedList(sharedWith: Friend);

            Result result = await _service.ShareAsync(Friend, list.Id, Another, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(new[] { Friend, Another }, _repository.Stored(list.Id)!.SharedWith);
        }

        [Fact]
        public async Task Share_Twice_KeepsOneEntryAndChangesOnce()
        {
            TodoList list = SeedList();

            await _service.ShareAsync(Owner, list.Id, Friend, CancellationToken.None);
            Result second = await _service.ShareAsync(Owner, list.Id, Friend, CancellationToken.None);

            Assert.True(second.IsSuccess);
            Assert.Single(_repository.Stored(list.Id)!.SharedWith);
            Assert.Equal(1, _repository.ChangingWrites);
        }

        [Fact]
        public async Task Share_WithOwner_ReturnsCannotShareWithOwner()
        {
            TodoList list = SeedList(sharedWith: Friend);

            Result result = await _service.ShareAsync(Friend, list.Id, Owner, CancellationToken.None);

            Assert.Equal(TodoListErrors.CannotShareWithOwner, result.Error);
        }

        [Fact]
        public async Task Share_EmptyUserId_ReturnsUserRequired()
        {
            TodoList list = SeedList();

            Result result = await _service.ShareAsync(Owner, list.Id, Guid.Empty, CancellationToken.None);

            Assert.Equal(TodoListErrors.UserRequired, result.Error);
        }

        [Fact]
        public async Task Share_Stranger_GetsNotFound()
        {
            TodoList list = SeedList();

            Result result = await _service.ShareAsync(Stranger, list.Id, Another, CancellationToken.None);

            Assert.Equal(TodoListErrors.NotFound, result.Error);
            Assert.Empty(_repository.Stored(list.Id)!.SharedWith);
        }

        [Fact]
        public async Task GetShares_SharedUser_SeesEveryone()
        {
            TodoList list = SeedList(sharedWith: new[] { Friend, Another });

            Result<IReadOnlyList<Guid>> result = await _service.GetSharesAsync(Friend, list.Id, CancellationToken.None);

            Assert.Equal(new[] { Friend, Another }, result.Value);
        }

        [Fact]
        public async Task GetShares_Stranger_GetsNotFound()
        {
            TodoList list = SeedList(sharedWith: Friend);

            Result<IReadOnlyList<Guid>> result = await _service.GetSharesAsync(Stranger, list.Id, CancellationToken.None);

            Assert.Equal(TodoListErrors.NotFound, result.Error);
        }

        [Fact]
        public async Task Unshare_SharedUser_CanRemoveAnotherUser()
        {
            TodoList list = SeedList(sharedWith: new[] { Friend, Another });

            Result result = await _service.UnshareAsync(Friend, list.Id, Another, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(new[] { Friend }, _repository.Stored(list.Id)!.SharedWith);
        }

        [Fact]
        public async Task Unshare_SharedUser_CanRemoveThemselves()
        {
            TodoList list = SeedList(sharedWith: Friend);

            await _service.UnshareAsync(Friend, list.Id, Friend, CancellationToken.None);

            Assert.Equal(TodoListErrors.NotFound, (await _service.GetByIdAsync(Friend, list.Id, CancellationToken.None)).Error);
        }

        [Fact]
        public async Task Unshare_MissingLink_SucceedsWithoutChanges()
        {
            TodoList list = SeedList(sharedWith: Friend);

            Result result = await _service.UnshareAsync(Owner, list.Id, Another, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal(0, _repository.ChangingWrites);
        }

        [Fact]
        public async Task Unshare_Stranger_GetsNotFound()
        {
            TodoList list = SeedList(sharedWith: Friend);

            Result result = await _service.UnshareAsync(Stranger, list.Id, Friend, CancellationToken.None);

            Assert.Equal(TodoListErrors.NotFound, result.Error);
            Assert.Equal(new[] { Friend }, _repository.Stored(list.Id)!.SharedWith);
        }

        [Fact]
        public async Task Update_ListDeletedAfterAccessCheck_ReturnsNotFound()
        {
            TodoList list = SeedList(sharedWith: Friend);
            _repository.BeforeNextWrite = () => _repository.DeleteAsync(list.Id, Owner, CancellationToken.None);

            Result<TodoListDto> result = await _service.UpdateAsync(Friend, list.Id, "Too late", CancellationToken.None);

            Assert.Equal(TodoListErrors.NotFound, result.Error);
        }

        [Fact]
        public async Task Share_ListDeletedAfterAccessCheck_ReturnsNotFound()
        {
            TodoList list = SeedList();
            _repository.BeforeNextWrite = () => _repository.DeleteAsync(list.Id, Owner, CancellationToken.None);

            Result result = await _service.ShareAsync(Owner, list.Id, Friend, CancellationToken.None);

            Assert.Equal(TodoListErrors.NotFound, result.Error);
        }

        [Fact]
        public async Task Unshare_ListDeletedAfterAccessCheck_ReturnsNotFound()
        {
            TodoList list = SeedList(sharedWith: Friend);
            _repository.BeforeNextWrite = () => _repository.DeleteAsync(list.Id, Owner, CancellationToken.None);

            Result result = await _service.UnshareAsync(Owner, list.Id, Friend, CancellationToken.None);

            Assert.Equal(TodoListErrors.NotFound, result.Error);
        }

        [Fact]
        public async Task Update_AccessRevokedAfterRequestStarted_ReturnsNotFoundAndKeepsTitle()
        {
            TodoList list = SeedList(sharedWith: Friend);
            _repository.BeforeNextWrite = () => _repository.RemoveShareAsync(list.Id, Owner, Friend, CancellationToken.None);

            Result<TodoListDto> result = await _service.UpdateAsync(Friend, list.Id, "Sneaky", CancellationToken.None);

            Assert.Equal(TodoListErrors.NotFound, result.Error);
            Assert.Equal("Groceries", _repository.Stored(list.Id)!.Title);
        }

        [Fact]
        public async Task Share_AccessRevokedAfterCheck_ReturnsNotFoundAndAddsNobody()
        {
            TodoList list = SeedList(sharedWith: Friend);
            _repository.BeforeNextWrite = () => _repository.RemoveShareAsync(list.Id, Owner, Friend, CancellationToken.None);

            Result result = await _service.ShareAsync(Friend, list.Id, Another, CancellationToken.None);

            Assert.Equal(TodoListErrors.NotFound, result.Error);
            Assert.Empty(_repository.Stored(list.Id)!.SharedWith);
        }

        [Fact]
        public async Task Update_ReturnsSharesAddedByOthers()
        {
            TodoList list = SeedList(sharedWith: Friend);
            _repository.BeforeNextWrite = () => _repository.AddShareAsync(list.Id, Owner, Another, CancellationToken.None);

            Result<TodoListDto> result = await _service.UpdateAsync(Owner, list.Id, "Food", CancellationToken.None);

            Assert.Equal(new[] { Friend, Another }, result.Value.SharedWith);
        }

        private TodoList SeedList(string title = "Groceries", Guid? owner = null, DateTimeOffset? createdAt = null, params Guid[] sharedWith)
        {
            TodoList list = new TodoList
            {
                Id = Guid.NewGuid(),
                Title = title,
                OwnerId = owner ?? Owner,
                CreatedAt = (createdAt ?? _time.GetUtcNow()).UtcDateTime,
                SharedWith = sharedWith.ToList()
            };
            _repository.Seed(list);
            return list;
        }
    }
}
