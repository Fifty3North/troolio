using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ShoppingList.Host.App;
using ShoppingList.Shared.ActorInterfaces;
using Troolio.Core;
using Troolio.Core.Client;
using Troolio.MessageQueue;

namespace Troolio.Tests.Setup
{
    public static class ActorSystemServer
    {
        private static IHost _host;
        private static ITroolioClient _client;

        private const string AppName = "Sample.Test";

        public static async Task Start()
        {
            Action<IServiceCollection> configureServices = (s) =>
            {
                s.AddTransient<IMessageQueueProvider, InMemoryMessageQueueProvider>();
            };            
            
            _host = Host.CreateDefaultBuilder()
               .TroolioServer<Stores.InMemoryStore, Core.Serialization.JsonEventSerializer>(AppName, 
                   registerAssemblies: new[] { 
                       typeof(ShoppingListActor).Assembly,
                       typeof(IShoppingListActor).Assembly
                   },
                   configureServices,
                   builderDelegates: null,
                   // This disables any actors that you do not want to be registered
                   // Turn off DB projections for running tests
                   disableActors: new[] { 
                       "Sample.Database.Projection.ShoppingListEFProjection", 
                       "Sample.Database.Projection.ShoppingListItemEFProjection" 
                   }
               );

            await _host.StartAsync();

            ClearHostStore();
        }

        public static async Task Shutdown()
        {
            ClearHostStore();

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

        private static void ClearHostStore()
        {
            if (_host != null)
            {
                Stores.IStore store = _host.Services.GetRequiredService<Stores.IStore>();

                if (store != null)
                {
                    store.Clear();
                };
            }
        }
    }
}
