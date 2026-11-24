using System.Net;
using System.Net.Http.Json;
using Helsi.TodoApi.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Mvc;

namespace Helsi.TodoApi.IntegrationTests
{
    [Collection(ApiCollection.Name)]
    public sealed class TodoListsApiTests(ApiFactory factory)
    {
        private const string Lists = "/api/todo-lists";

        private readonly HttpClient _client = factory.CreateClient();

        // Fresh ids per test, so tests don't see each other's lists in the shared database.
        private readonly Guid _owner = Guid.NewGuid();
        private readonly Guid _friend = Guid.NewGuid();
        private readonly Guid _stranger = Guid.NewGuid();
        private readonly Guid _another = Guid.NewGuid();

        [Fact]
        public async Task Health_MongoIsUp_ReturnsHealthy()
        {
            HttpResponseMessage response = await _client.GetAsync("/health");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task AnyEndpoint_WithoutUserHeader_Returns400()
        {
            HttpResponseMessage response = await _client.GetAsync(Lists);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("UserId.Required", (await ReadProblem(response)).Title);
        }

        [Fact]
        public async Task Create_ReturnsCreatedWithLocationAndTrimmedTitle()
        {
            HttpResponseMessage response = await Send(HttpMethod.Post, Lists, _owner, new { title = "  Groceries  " });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.NotNull(response.Headers.Location);
            TodoListBody body = (await response.Content.ReadFromJsonAsync<TodoListBody>())!;
            Assert.Equal("Groceries", body.Title);
            Assert.Equal(_owner, body.OwnerId);
        }

        [Fact]
        public async Task Create_TooLongTitle_Returns400WithErrorCode()
        {
            HttpResponseMessage response = await Send(HttpMethod.Post, Lists, _owner, new { title = new string('a', 256) });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("TodoList.TitleTooLong", (await ReadProblem(response)).Title);
        }

        [Fact]
        public async Task SharingFlow_FollowsAccessRules()
        {
            Guid id = await CreateList(_owner, "Shared list");

            // Stranger cannot see it.
            Assert.Equal(HttpStatusCode.NotFound, (await Send(HttpMethod.Get, $"{Lists}/{id}", _stranger)).StatusCode);

            // Owner shares with friend, friend shares further with another user.
            Assert.Equal(HttpStatusCode.NoContent, (await Send(HttpMethod.Post, $"{Lists}/{id}/shares", _owner, new { userId = _friend })).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await Send(HttpMethod.Post, $"{Lists}/{id}/shares", _friend, new { userId = _another })).StatusCode);

            // Both now see it, including in the feed.
            Assert.Equal(HttpStatusCode.OK, (await Send(HttpMethod.Get, $"{Lists}/{id}", _another)).StatusCode);
            PageBody feed = (await (await Send(HttpMethod.Get, $"{Lists}?page=1&pageSize=20", _another)).Content.ReadFromJsonAsync<PageBody>())!;
            Assert.Contains(feed.Items, item => item.Id == id);

            // Friend can rename, cannot delete.
            Assert.Equal(HttpStatusCode.OK, (await Send(HttpMethod.Put, $"{Lists}/{id}", _friend, new { title = "Renamed" })).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await Send(HttpMethod.Delete, $"{Lists}/{id}", _friend)).StatusCode);

            // Friend removes the other user's link, that user loses access.
            Assert.Equal(HttpStatusCode.NoContent, (await Send(HttpMethod.Delete, $"{Lists}/{id}/shares/{_another}", _friend)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await Send(HttpMethod.Get, $"{Lists}/{id}", _another)).StatusCode);

            // Shares list reflects that.
            List<Guid> shares = (await (await Send(HttpMethod.Get, $"{Lists}/{id}/shares", _owner)).Content.ReadFromJsonAsync<List<Guid>>())!;
            Assert.Equal(new[] { _friend }, shares);

            // Owner deletes, list is gone.
            Assert.Equal(HttpStatusCode.NoContent, (await Send(HttpMethod.Delete, $"{Lists}/{id}", _owner)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await Send(HttpMethod.Get, $"{Lists}/{id}", _owner)).StatusCode);
        }

        [Fact]
        public async Task ConcurrentSharesAndRename_NoChangeIsLost()
        {
            Guid id = await CreateList(_owner, "Busy list");
            List<Guid> users = Enumerable.Range(0, 20).Select(_ => Guid.NewGuid()).ToList();

            List<Task<HttpResponseMessage>> requests = users
                .Select(user => Send(HttpMethod.Post, $"{Lists}/{id}/shares", _owner, new { userId = user }))
                .ToList();
            requests.Add(Send(HttpMethod.Put, $"{Lists}/{id}", _owner, new { title = "Renamed while sharing" }));

            HttpResponseMessage[] responses = await Task.WhenAll(requests);

            Assert.All(responses, response => Assert.True(response.IsSuccessStatusCode));
            TodoListBody list = (await (await Send(HttpMethod.Get, $"{Lists}/{id}", _owner)).Content.ReadFromJsonAsync<TodoListBody>())!;
            Assert.Equal("Renamed while sharing", list.Title);
            Assert.Equal(users.OrderBy(user => user), list.SharedWith.OrderBy(user => user));
        }

        [Fact]
        public async Task Feed_IsNewestFirstAndPaged()
        {
            Guid first = await CreateList(_owner, "First");
            Guid second = await CreateList(_owner, "Second");
            Guid third = await CreateList(_owner, "Third");

            PageBody page1 = (await (await Send(HttpMethod.Get, $"{Lists}?page=1&pageSize=2", _owner)).Content.ReadFromJsonAsync<PageBody>())!;
            PageBody page2 = (await (await Send(HttpMethod.Get, $"{Lists}?page=2&pageSize=2", _owner)).Content.ReadFromJsonAsync<PageBody>())!;

            Assert.Equal(3, page1.TotalCount);
            Assert.Equal(new[] { third, second }, page1.Items.Select(item => item.Id));
            Assert.Equal(new[] { first }, page2.Items.Select(item => item.Id));
        }

        private async Task<Guid> CreateList(Guid userId, string title)
        {
            HttpResponseMessage response = await Send(HttpMethod.Post, Lists, userId, new { title });
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<TodoListBody>())!.Id;
        }

        private async Task<HttpResponseMessage> Send(HttpMethod method, string url, Guid userId, object? body = null)
        {
            HttpRequestMessage request = new HttpRequestMessage(method, url);
            request.Headers.Add("X-User-Id", userId.ToString());
            if (body != null)
            {
                request.Content = JsonContent.Create(body);
            }

            return await _client.SendAsync(request);
        }

        private static async Task<ProblemDetails> ReadProblem(HttpResponseMessage response)
        {
            return (await response.Content.ReadFromJsonAsync<ProblemDetails>())!;
        }

        private sealed record TodoListBody(Guid Id, string Title, Guid OwnerId, DateTime CreatedAt, List<Guid> SharedWith);

        private sealed record SummaryBody(Guid Id, string Title);

        private sealed record PageBody(List<SummaryBody> Items, long TotalCount, int Page, int PageSize);
    }
}
