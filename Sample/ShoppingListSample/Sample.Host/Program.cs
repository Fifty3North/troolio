using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sample.Shared.ActorInterfaces;
using Sample.Database.Model;
using Sample.Host.App.ShoppingList;
using Troolio.Stores.EventStore;
using Microsoft.Extensions.Hosting;
using Troolio.Core;
using Orleans;
using Sample.Host.App;
using Orleans.Configuration;

Console.WriteLine("Running sample. Booting cluster might take some time ...\n");

IConfiguration configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

// create db and run any pending migrations
new ShoppingListsDbContext(configuration)
    .RunMigrations();

Action<IServiceCollection> configureServices = (s) =>
{
    s
    .AddDbContext<ShoppingListsDbContext>()
    .AddSingleton<F3N.Providers.MessageQueue.IMessageQueueProvider>(new F3N.Providers.MessageQueue.InMemoryMessageQueueProvider())
    //.AddSingleton<IIncomingGrainCallFilter, LoggingCallFilter>()
    ;
};


if (configuration["Shopping:Clustering:Storage"] != null && configuration["Shopping:Clustering:Storage"] == "Local")
{
    var host = Host.CreateDefaultBuilder(args)
        .TroolioServer("Shopping", new[] {
            typeof(IShoppingListActor).Assembly,    // Sample.Shared
            typeof(ShoppingListActor).Assembly      // Sample.Host.App
        }, configureServices
        //,
        //disableActors: new[] {
        //    //"Sample.Database.Projection.ShoppingListEFProjection",
        //    //"Sample.Database.Projection.ShoppingListItemEFProjection"
        //}
        );

    await host.RunAsync();
}
else
{
    await Startup.RunWithDefaults("Shopping",
    new[] {
        typeof(IShoppingListActor).Assembly,    // Sample.Shared
        typeof(ShoppingListActor).Assembly      // Sample.Host.App
    },
    configureServices
    //,
    //disableActors: new[] {
    //    //"Sample.Database.Projection.ShoppingListEFProjection",
    //    //"Sample.Database.Projection.ShoppingListItemEFProjection"
    //}
    );
}

