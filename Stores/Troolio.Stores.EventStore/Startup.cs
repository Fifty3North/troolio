using KurrentDB.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;
using Troolio.Core;

namespace Troolio.Stores.EventStore;

public static class Startup
{
    public static Task RunWithDefaults(string appName, Assembly[] registerAssemblies,
        Action<IServiceCollection> additionalServices, string[]? disableActors = null) =>
        StartupHost(appName, registerAssemblies, additionalServices, disableActors).RunAsync();
    public static Task StartWithDefaults(string appName, Assembly[] registerAssemblies,
        Action<IServiceCollection> additionalServices, string[]? disableActors = null) =>
        StartupHost(appName, registerAssemblies, additionalServices, disableActors).StartAsync();
    private static IHost StartupHost(string appName, Assembly[] assemblies,
        Action<IServiceCollection> services, string[]? disableActors)
    {
        IConfiguration configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").AddEnvironmentVariables().Build();
        string connection = configuration.GetConnectionString("EventStore")
            ?? throw new ArgumentException("ConnectionStrings:EventStore must contain an esdb gRPC connection string.");
        return Host.CreateDefaultBuilder().TroolioServer<ESStore>(appName, assemblies, collection =>
        {
            collection.AddSingleton(_ => ES.CreateClient(connection));
            services(collection);
        }, disableActors: disableActors);
    }
}
