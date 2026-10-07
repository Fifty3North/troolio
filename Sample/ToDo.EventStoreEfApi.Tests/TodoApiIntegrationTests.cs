using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Troolio.Core;

namespace ToDo.EventStoreEfApi.Tests;

[TestFixture, NonParallelizable, Category("TodoRuntime")]
public sealed class TodoApiIntegrationTests
{
    private string directory = null!;
    [SetUp] public void SetUp() => directory = Directory.CreateTempSubdirectory("troolio-todo-").FullName;
    [TearDown] public void TearDown() => Directory.Delete(directory, true);

    [Test]
    public async Task RealRuntime_CreateCompleteRetries_CommitAndProjectOnce()
    {
        await using var factory = new TodoApplicationFactory(directory);
        using var client = factory.CreateClient();
        var id = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        client.DefaultRequestHeaders.Add("X-Correlation-Id", correlationId.ToString());
        client.DefaultRequestHeaders.Add("X-User-Id", Guid.NewGuid().ToString()); // Untrusted header never grants identity.
        var request = new { id, title = "Ship the public sample" };
        Assert.That((await client.PostAsJsonAsync("/todos", request)).StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That((await client.PostAsJsonAsync("/todos", request)).StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That((await client.PostAsJsonAsync("/todos", new { id, title = "Different" })).StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        var initial = await WaitForReadModel(client, id, completed: false);
        Assert.That(initial!.IsCompleted, Is.False);
        Assert.That(initial.OwnerId, Is.EqualTo(TodoApplicationFactory.UserId));
        Assert.That((await client.PostAsync($"/todos/{id}/complete", null)).StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
        Assert.That((await client.PostAsync($"/todos/{id}/complete", null)).StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
        var completed = await WaitForReadModel(client, id, completed: true);
        Assert.That(completed!.IsCompleted, Is.True);
        Assert.That(completed.CompletedAt, Is.Not.Null);
        var events = await factory.Services.GetRequiredService<TodoRuntime>().Store.ReadStream($"TodoActor-{id}");
        Assert.That(events.Length, Is.EqualTo(2));
        foreach (var e in events.Cast<Event>())
        {
            Assert.That(e.Headers.UserId, Is.EqualTo(TodoApplicationFactory.UserId));
            Assert.That(e.Headers.CorrelationId, Is.EqualTo(correlationId));
            Assert.That(e.Headers.MessageId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(e.Headers.CausationId, Is.Not.Null);
        }
        Assert.That(events.Cast<Event>().Select(e => e.Headers.MessageId).Distinct().Count(), Is.EqualTo(2));
        Assert.That((await client.GetFromJsonAsync<List<TodoReadModel>>("/todos"))!.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task SqlCatalogUsesStableBoundedPagesAndRejectsInvalidRanges()
    {
        await using var factory = new TodoApplicationFactory(directory);
        using var client = factory.CreateClient();
        foreach (var title in new[] { "First", "Second", "Third" })
        {
            var id = Guid.NewGuid();
            Assert.That((await client.PostAsJsonAsync("/todos", new { id, title })).StatusCode, Is.EqualTo(HttpStatusCode.Created));
            await WaitForReadModel(client, id, completed: false);
        }
        var first = (await client.GetFromJsonAsync<List<TodoReadModel>>("/todos?skip=0&take=2"))!;
        var second = (await client.GetFromJsonAsync<List<TodoReadModel>>("/todos?skip=2&take=2"))!;
        Assert.That(first.Count, Is.EqualTo(2));
        Assert.That(second.Count, Is.EqualTo(1));
        Assert.That(first.Select(row => row.Id).Intersect(second.Select(row => row.Id)), Is.Empty);
        Assert.That(first[0].CreatedAt, Is.GreaterThanOrEqualTo(first[1].CreatedAt));
        Assert.That((await client.GetAsync("/todos?skip=-1")).StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That((await client.GetAsync("/todos?take=101")).StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task ActualActor_RejectsMissingInvalidAndForeignOwner()
    {
        await using var factory = new TodoApplicationFactory(directory);
        using var client = factory.CreateClient();
        Assert.That((await client.PostAsJsonAsync("/todos", new { title = " " })).StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That((await client.PostAsJsonAsync("/todos", new { title = new string('a', 201) })).StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That((await client.PostAsJsonAsync("/todos", new { id = Guid.Empty, title = "Valid" })).StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That((await client.PostAsync($"/todos/{Guid.NewGuid()}/complete", null)).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var foreignId = Guid.NewGuid();
        var foreignOwner = Guid.NewGuid();
        await factory.Services.GetRequiredService<TodoRuntime>().Tell(foreignId,
            new CreateTodoCommand(new Metadata(Guid.NewGuid(), foreignOwner, Guid.Empty), foreignId, "Private"));
        client.DefaultRequestHeaders.Add("X-User-Id", foreignOwner.ToString());
        Assert.That((await client.GetAsync($"/todos/{foreignId}")).StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That((await client.PostAsync($"/todos/{foreignId}/complete", null)).StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        Assert.That((await client.GetFromJsonAsync<List<TodoReadModel>>("/todos"))!, Is.Empty);
    }

    private static async Task<TodoReadModel> WaitForReadModel(HttpClient client, Guid id, bool completed)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (DateTime.UtcNow < deadline)
        {
            var response = await client.GetAsync($"/todos/{id}");
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var model = (await response.Content.ReadFromJsonAsync<TodoReadModel>())!;
                if (model.IsCompleted == completed) return model;
            }
            else Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            await Task.Delay(50);
        }
        Assert.Fail("The live projection did not reach the requested state within 15 seconds.");
        throw new InvalidOperationException();
    }

    [Test, Category("TodoKurrent")]
    public async Task KurrentGrpc_ActualRuntime_PersistsAcrossHostRestart()
    {
        var connection = Environment.GetEnvironmentVariable("TROOLIO_TODO_KURRENT_CONNECTION");
        if (string.IsNullOrWhiteSpace(connection)) Assert.Ignore("Set TROOLIO_TODO_KURRENT_CONNECTION to an owned disposable gRPC fixture.");
        var id = Guid.NewGuid();
        await using (var first = new TodoApplicationFactory(directory, "EventStore", connection))
        {
            using var client = first.CreateClient();
            Assert.That((await client.PostAsJsonAsync("/todos", new { id, title = "Persist through gRPC" })).StatusCode,
                Is.EqualTo(HttpStatusCode.Created));
            Assert.That((await WaitForReadModel(client, id, completed: false)).Title, Is.EqualTo("Persist through gRPC"));
        }
        await using var second = new TodoApplicationFactory(directory, "EventStore", connection);
        using var restarted = second.CreateClient();
        Assert.That((await second.Services.GetRequiredService<TodoRuntime>().ReadState(id)).Title,
            Is.EqualTo("Persist through gRPC"));
        Assert.That((await restarted.PostAsync($"/todos/{id}/complete", null)).StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
        Assert.That((await second.Services.GetRequiredService<TodoRuntime>().Store.ReadStream($"TodoActor-{id}")).Length,
            Is.EqualTo(2));
        Assert.That((await WaitForReadModel(restarted, id, completed: true)).IsCompleted, Is.True);
        // No global Clear: the fixture may contain unrelated streams owned by other tests.
    }

    [Test]
    public async Task DurableRestart_ReplaysOnlyOwnState_WithNoProjectionWritesOrGlobalScan()
    {
        var id = Guid.Parse("22222222-2222-2222-2222-222222222222");
        await using (var first = new TodoApplicationFactory(directory))
        {
            using var client = first.CreateClient();
            await client.PostAsJsonAsync("/todos", new { id, title = "Survive restart" });
            await client.PostAsync($"/todos/{id}/complete", null);
            await WaitForReadModel(client, id, completed: true);
            using var scope = first.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TodoReadModelDbContext>();
            // Fail loudly if activation replay attempts to update the durable read model.
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_replay_update BEFORE UPDATE ON Todos BEGIN SELECT RAISE(ABORT, 'Replay must not project'); END;");
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_replay_insert BEFORE INSERT ON Todos WHEN NEW.Id = '22222222-2222-2222-2222-222222222222' BEGIN SELECT RAISE(ABORT, 'Replay must not project'); END;");
        }
        var malformed = Path.Combine(directory, "events", "TodoActor-unrelated");
        await File.WriteAllLinesAsync(malformed, Enumerable.Repeat("malformed event data", 20_000));
        await using var second = new TodoApplicationFactory(directory);
        using var restarted = second.CreateClient();
        Assert.That((await restarted.GetAsync("/health")).StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var runtime = second.Services.GetRequiredService<TodoRuntime>();
        var restored = await runtime.ReadState(id); // Activates a real actor and reads only its stream.
        Assert.That(restored.Title, Is.EqualTo("Survive restart"));
        Assert.That(restored.IsCompleted, Is.True);
        Assert.That((await restarted.PostAsync($"/todos/{id}/complete", null)).StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
        Assert.That((await runtime.Store.ReadStream($"TodoActor-{id}")).Length, Is.EqualTo(2));
        Assert.That((await WaitForReadModel(restarted, id, completed: true)).IsCompleted, Is.True);
        Assert.That((await restarted.PostAsJsonAsync("/todos", new { title = "An unrelated healthy actor" })).StatusCode,
            Is.EqualTo(HttpStatusCode.Created));
        Assert.That(new FileInfo(malformed).Length, Is.GreaterThan(100_000));
    }
}

internal sealed class TodoApplicationFactory(string directory, string mode = "FileSystem", string? connection = null) : WebApplicationFactory<Program>
{
    public static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Todo:EventStore"] = mode, ["EventStore:ConnectionString"] = connection ?? "esdb://localhost:2113?tls=false", ["Todo:EventFolder"] = Path.Combine(directory, "events"),
            ["Todo:DemoUserId"] = UserId.ToString(), ["Todo:SiloPort"] = "19220", ["Todo:GatewayPort"] = "19221",
            ["ConnectionStrings:ReadModels"] = $"Data Source={Path.Combine(directory, "readmodels.db")}",
            ["Logging:LogLevel:Default"] = "Error"
        }));
    }
}
