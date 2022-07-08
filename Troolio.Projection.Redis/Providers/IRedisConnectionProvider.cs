using StackExchange.Redis;

namespace Troolio.Projection.Redis.Providers
{
    public interface IRedisConnectionProvider
    {
        IConnectionMultiplexer Connection { get; }
    }
}
