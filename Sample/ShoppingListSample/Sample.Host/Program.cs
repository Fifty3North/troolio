using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sample.Database.Model;
using Sample.Host.App;
using Sample.Host.App.ShoppingList;
using Sample.Shared.ActorInterfaces;
using Troolio.Core;
using Troolio.Core.Serialization;
using Troolio.MessageQueue;
using Troolio.Stores;

var host = Host.CreateDefaultBuilder(args).TroolioServer("Shopping",
    [typeof(IShoppingListActor).Assembly, typeof(ShoppingListActor).Assembly], services =>
    {
        services.AddDbContext<ShoppingListsDbContext>((provider, options) => options.UseSqlite(
            ShoppingListsDbContext.ConnectionString(provider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>())));
        services.AddSingleton<IMessageQueueProvider, InMemoryMessageQueueProvider>();
        // A restartable local demonstration store; use a supported service store for deployment.
        services.AddSingleton<IStore>(provider => new FileSystemStore(
            provider.GetRequiredService<IEventSerializer>(),
            provider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>()["Shopping:EventFolder"] ?? "troolio-samples-shopping"));
    }, silo => silo.ConfigureServices(services => services.AddShoppingReadModels()));
using (var scope = host.Services.CreateScope())
    await scope.ServiceProvider.GetRequiredService<ShoppingListsDbContext>().Database.EnsureCreatedAsync();
await host.RunAsync();
