using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using Orleans.Runtime;
using System.Net;

namespace Troolio.Stores.EventStore
{
    public class ES
    {
        bool _isCluster;
        int _port;
        string[] _hosts;

        public ES(bool isCluster, int port = 1113, params string[] hosts)
        {
            _isCluster = isCluster;
            _port = port;
            _hosts = hosts;
        }

        public static IEventStoreConnection? Connection
        {
            get; private set;
        }

        /// <summary>
        /// For creating a temp connection to ES
        /// </summary>
        /// <param name="isCluster">Whether this transient connection is cluster</param>
        /// <param name="port">If cluster, gossip port, if non-cluster then port to communicate with ES</param>
        /// <param name="ipAddresses">List of servers, only one is valid if non-cluster</param>
        /// <returns></returns>
        public async Task Connect()
        {
            if (_isCluster)
            {
                var clusterInfo = _hosts.Select(e =>
                {
                    return new GossipSeed(new DnsEndPoint(e, _port));
                }).ToArray();

                Connection = EventStoreConnection.Create(
                    ConnectionSettings
                        .Create()
                        .LimitReconnectionsTo(100),
                    ClusterSettings
                        .Create()
                        .DiscoverClusterViaGossipSeeds()
                        .SetGossipTimeout(TimeSpan.FromMilliseconds(500))
                        .SetGossipSeedEndPoints(clusterInfo));
            }
            else
            {
                ConnectionSettingsBuilder settings;

                settings = ConnectionSettings
                    .Create()
                    .LimitAttemptsForOperationTo(10)
                    .DisableServerCertificateValidation()
                    .DisableTls()
                    .FailOnNoServerResponse()
                    .UseDebugLogger()
                    .SetDefaultUserCredentials(new UserCredentials("admin", "changeit"));

                Connection = EventStoreConnection.Create(settings, new Uri(String.Format("tcp://{0}:{1}", _hosts[0], _port)));
            }

            await Connection.ConnectAsync();
        }
    }

    public class InitialiseEventStore : IStartupTask
    {
        ES _eventStore;

        public InitialiseEventStore(ES eventStore)
        {
            _eventStore = eventStore;
        }

        public async Task Execute(CancellationToken cancellationToken)
        {
            await _eventStore.Connect();
        }
    }
}
