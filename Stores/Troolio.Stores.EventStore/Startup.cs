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
        /// <summary>
        /// This will run the TroolioServer using defaults.  Running will block until the server is shutdown.
        /// </summary>
        /// <param name="appName"></param>
        /// <param name="registerAssemblies"></param>
        /// <param name="additionalServices"></param>
        /// <returns></returns>
        public static Task RunWithDefaults(string appName, Assembly[] registerAssemblies, Action<IServiceCollection> additionalServices, string[]? disableActors = null)
        {
            return StartupHost(appName, registerAssemblies, additionalServices, disableActors)
                .RunAsync();
        }

        /// <summary>
        /// This will start the TroolioServer using defaults.  Starting will not block while the server is running.
        /// </summary>
        /// <param name="appName"></param>
        /// <param name="registerAssemblies"></param>
        /// <param name="additionalServices"></param>
        /// <returns></returns>
        public static Task StartWithDefaults(string appName, Assembly[] registerAssemblies, Action<IServiceCollection> additionalServices, string[]? disableActors = null)
        {
            return StartupHost(appName, registerAssemblies, additionalServices, disableActors)
                .StartAsync();
        }

        private static IHost StartupHost(string appName, Assembly[] registerAssemblies, Action<IServiceCollection> additionalServices, string[]? disableActors = null)
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

            return Host.CreateDefaultBuilder()
                .TroolioServer<Troolio.Stores.ESStore>(appName, registerAssemblies, configureDelegates, builderDelegates, disableActors: disableActors);
        }
    }
}
