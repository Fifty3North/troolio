using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Omu.ValueInjecter;
using StackExchange.Redis;
using Troolio.Core;
using Troolio.Projection.Persistence;
using Troolio.Projection.Redis.Persistence;
using Troolio.Projection.Redis.Providers;

namespace Troolio.Extensions.Tests;

public sealed class CacheItem { public Guid Id { get; set; } public string? Name { get; set; } }
public interface ICacheProbe : IActor { }
public sealed class CacheProbe(IRedisWriteProvider<CacheItem> write, IRedisReadProvider<CacheItem> read)
    : RedisPersistence<CacheItem>("probe", null!, write, read, NullLogger<RedisPersistence<CacheItem>>.Instance), ICacheProbe;

[TestFixture]
[Category("Integration")]
public sealed class RedisIntegrationTests
{
    private RedisConnectionProvider _connection = null!;
    private RedisProvider<CacheItem> _provider = null!;
    [OneTimeSetUp]
    public void Setup()
    {
        string endpoint = Environment.GetEnvironmentVariable("TROOLIO_TEST_REDIS")
            ?? throw new InvalidOperationException("Set TROOLIO_TEST_REDIS to an explicitly owned fixture.");
        _connection = new RedisConnectionProvider(endpoint);
        _provider = new(_connection);
        Assert.That(_connection.Connection.IsConnected, Is.True);
    }
    [OneTimeTearDown] public void Teardown() => _connection.Dispose();

    [Test]
    public async Task Mutations_And_Selected_Properties_Are_Committed_Before_Returning()
    {
        Guid partition = Guid.NewGuid(), id = Guid.NewGuid();
        await _provider.CreateEntity(id, new CacheItem { Id = id, Name = "before" }, partition);
        Mapper.AddMap<EventEnvelope<NameChanged>, Task<RedisEventEntityUpdate<CacheItem>>>(envelope =>
            Task.FromResult(new RedisEventEntityUpdate<CacheItem>(id, new CacheItem { Id = id, Name = envelope.Event.Name }, partition, item => item.Name!)));
        CacheProbe probe = new(_provider, _provider);
        await probe.Update(new EventEnvelope<NameChanged>(id.ToString(), "cache-" + id, new NameChanged("after", new Metadata(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid())), 2));
        Assert.That((await _provider.GetEntityAsync(id))!.Name, Is.EqualTo("after"));
        Assert.That(_provider.GetEntityIndexKeys(partition), Has.Length.EqualTo(1));
        await _provider.RemoveEntityKeyFromPartition(id, partition);
        Assert.That(_provider.EntityIndexContains(id, partition), Is.False);
        await _provider.AddEntityKeyToPartition(id, partition);
        await _provider.DeleteEntity(id, partition);
        Assert.That(_provider.EntityKeyExists(id), Is.False);
    }

    [Test]
    public async Task Concurrent_Writes_Return_Their_Own_Cursors_And_Pages_Are_Bounded()
    {
        Guid partition = Guid.NewGuid();
        var writes = await Task.WhenAll(Enumerable.Range(0, 40).Select(async number =>
        {
            Guid id = Guid.NewGuid();
            string cursor = await _provider.CreateEntity(id, new CacheItem { Id = id, Name = number.ToString() }, partition);
            return (Id: id, Cursor: cursor);
        }));
        Assert.That(writes.Select(value => value.Cursor).Distinct().Count(), Is.EqualTo(40));
        var entries = (await _provider.GetChangePageAsync(partition, 0, 100)).Entries;
        foreach (var write in writes)
            Assert.That(entries.Single(entry => entry.ChangeId.ToString() == write.Cursor).EntityKey.ToString(), Is.EqualTo(_provider.GetEntityKey(write.Id)));
        var first = await _provider.GetChangePageAsync(partition, 0, 7);
        Assert.That(first.Entries, Has.Count.EqualTo(7));
        Assert.That(first.NextOffset, Is.EqualTo(7));
        Assert.That(first.HasMore, Is.True);
        Assert.That((await _provider.GetChangePageAsync(partition, 35, 7)).Entries, Has.Count.EqualTo(5));
        Assert.Throws<ArgumentOutOfRangeException>(() => _provider.GetChangePage(partition, 0, 1001));
    }

    [Test]
    public async Task Mapped_Membership_Changes_Do_Not_Silently_Acknowledge_Missing_Entities()
    {
        Guid partition = Guid.NewGuid(), id = Guid.NewGuid();
        Mapper.AddMap<EventEnvelope<NameChanged>, Task<List<RedisEventEntity<CacheItem>>>>(_ =>
            Task.FromResult(new List<RedisEventEntity<CacheItem>> { new RedisEventEntityAddToPartition<CacheItem>(id, null!, partition) }));
        CacheProbe probe = new(_provider, _provider);
        var envelope = new EventEnvelope<NameChanged>(id.ToString(), "cache-" + id,
            new NameChanged("membership", new Metadata(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid())), 1);
        Assert.ThrowsAsync<InvalidOperationException>(() => probe.Persist(envelope));
        Assert.That((await _provider.GetChangePageAsync(partition, 0)).Entries, Is.Empty);
        await _provider.CreateEntity(id, new CacheItem { Id = id, Name = "exists" }, Guid.NewGuid());
        await probe.Persist(envelope);
        Assert.That((await _provider.GetChangePageAsync(partition, 0)).Entries, Has.Count.EqualTo(1));
    }

    [Test]
    public void Provider_Instances_Keep_Their_Configuration_And_Dispose_Independently()
    {
        string endpoint = Environment.GetEnvironmentVariable("TROOLIO_TEST_REDIS")!;
        using RedisConnectionProvider second = new(endpoint);
        Assert.That(second.Connection, Is.Not.SameAs(_connection.Connection));
        using RedisConnectionProvider unavailable = new("localhost:1,connectTimeout=50,abortConnect=false");
        Assert.That(unavailable.Connection, Is.Not.SameAs(_connection.Connection));
        Assert.That(unavailable.Connection.IsConnected, Is.False);
    }
}
