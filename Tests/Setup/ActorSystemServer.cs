using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sample.Host.App.ShoppingList;
using Sample.Shared.ActorInterfaces;
using Troolio.Core;
using Troolio.Core.Client;

namespace Troolio.Tests.Setup
{
    public static class ActorSystemServer
    {
        static IHost _host;
        private static ITroolioClient _client;
        const string AppName = "Sample.Test";
        public static async Task Start()
        {
            Action<IServiceCollection> configureServices = (s) =>
            {
                
            };            
            
            _host = Host.CreateDefaultBuilder()
               .TroolioServer(AppName, 
                   new[] { 
                       typeof(ShoppingListActor).Assembly,
                       typeof(IShoppingListActor).Assembly
                   },
                   configureServices,
                   // This disables any actors that you do not want to be registered
                   // Turn off DB projections for running tests
                   disableActors: new[] { 
                       "Sample.Database.Projection.ShoppingListEFProjection", 
                       "Sample.Database.Projection.ShoppingListItemEFProjection" 
                   }
               );

            await _host.StartAsync();
        }

        public static async Task Shutdown()
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        public static async Task<ITroolioClient> ConnectClient()
        {
            // nothing async (yet)
            await Task.CompletedTask;

            _client = new TroolioClient(new[] { typeof(IAllShoppingListsActor).Assembly }, AppName, new ConfigurationBuilder());
            return _client;
        }

        public static async Task DisconnectClient()
        {
            await Task.CompletedTask;
        }

        internal static async Task EnableTracing()
        {
            await _client.Tell(Constants.SingletonActorId, new EnableTracing());
        }
    }
}
