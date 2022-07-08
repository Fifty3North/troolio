using StackExchange.Redis;

namespace Troolio.Projection.Redis.Providers
{
    public class RedisConnectionProvider : IRedisConnectionProvider
    {
        private static Lazy<ConnectionMultiplexer> lazyConnection = null;

        private static readonly object locker = new object();

        public IConnectionMultiplexer Connection
        {
            get { return lazyConnection.Value; }
        }

        public RedisConnectionProvider(string configurationString) : this(ConfigurationOptions.Parse(configurationString))
        {
        }

        public RedisConnectionProvider(ConfigurationOptions configuration)
        {
            lock (locker)
            {
                if (lazyConnection == null)
                {
                    lazyConnection = new Lazy<ConnectionMultiplexer>(() => ConnectionMultiplexer.Connect(configuration));
                }
            }
        }
    }
}
