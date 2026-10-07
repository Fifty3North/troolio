using StackExchange.Redis;

namespace Troolio.Projection.Redis.Providers;

/// <summary>A reusable connection owned by this provider's DI lifetime.</summary>
public sealed class RedisConnectionProvider : IRedisConnectionProvider, IDisposable
{
    private readonly Lazy<ConnectionMultiplexer> _connection;
    public IConnectionMultiplexer Connection => _connection.Value;
    public RedisConnectionProvider(string configuration) : this(ConfigurationOptions.Parse(configuration)) { }
    public RedisConnectionProvider(ConfigurationOptions configuration) =>
        _connection = new(() => ConnectionMultiplexer.Connect(configuration));
    public void Dispose() { if (_connection.IsValueCreated) _connection.Value.Dispose(); }
}
