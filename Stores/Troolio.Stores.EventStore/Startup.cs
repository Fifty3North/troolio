using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Hosting;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Common.Log;
using Troolio.Core;
using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace Troolio.Stores.EventStore
{
    public static class Startup
    {
        public static Task StartWithDefaults(string appName, Assembly[] registerAssemblies, Action<IServiceCollection> additionalServices)
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            bool eventStoreCluster = bool.Parse(configuration[$"{appName}:Storage:EventStoreCluster"]);
            int eventStorePort = int.Parse(configuration[$"{appName}:Storage:EventStorePort"]);
            string[] eventStoreHosts = configuration[$"{appName}:Storage:EventStoreHosts"].Split(',');

            Action<IServiceCollection> configureDelegates = (s) =>
            {
                s.AddSingleton<ILogger, ConsoleLogger>();
                s.AddSingleton<ES>((e) => new ES(eventStoreCluster, eventStorePort, eventStoreHosts));
                additionalServices.Invoke(s);
            };

            Action<ISiloBuilder> builderDelegates = (b) =>
            {
                b.AddStartupTask<Troolio.Stores.EventStore.InitialiseEventStore>();
            };

            var host = Host.CreateDefaultBuilder()
                .TroolioServer<Troolio.Stores.ESStore>(appName, registerAssemblies, configureDelegates, builderDelegates);

            return host.RunAsync();
        }
    }
}
