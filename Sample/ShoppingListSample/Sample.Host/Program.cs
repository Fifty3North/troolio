﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sample.Shared.ActorInterfaces;
using Sample.Database.Model;
using Sample.Host.App.ShoppingList;
using Troolio.Stores.EventStore;
using Microsoft.Extensions.Hosting;
using Troolio.Core;

Console.WriteLine("Running sample. Booting cluster might take some time ...\n");

IConfiguration configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

// Create MySQL db and run any pending migrations
await new ShoppingListsDbContext(configuration).RunMigrations();

Action<IServiceCollection> configureServices = (s) =>
{
    s
    .AddDbContext<ShoppingListsDbContext>()
    .AddSingleton<F3N.Providers.MessageQueue.IMessageQueueProvider>(new F3N.Providers.MessageQueue.InMemoryMessageQueueProvider())
    // Orleans incoming call fiulter to log messages
    //.AddSingleton<IIncomingGrainCallFilter, LoggingCallFilter>()
    ;
};

// For non-docker debugging using in-memory event database and actor registry
if (configuration["Shopping:Clustering:Storage"] != null && configuration["Shopping:Clustering:Storage"] == "Local")
{
    var host = Host.CreateDefaultBuilder(args)
        .TroolioServer("Shopping", new[] {
            typeof(IShoppingListActor).Assembly,    // Sample.Shared
            typeof(ShoppingListActor).Assembly      // Sample.Host.App
        }, configureServices
        // Lines below will disable MySQL projections if uncommented
        //,
        //disableActors: new[] {
        //    "Sample.Database.Projection.ShoppingListEFProjection",
        //    "Sample.Database.Projection.ShoppingListItemEFProjection"
        //}
        );

    await host.RunAsync();
}
// Docker using Event Store and Azure table storage for actor registry
else
{
    await Startup.RunWithDefaults("Shopping",
    new[] {
        typeof(IShoppingListActor).Assembly,    // Sample.Shared
        typeof(ShoppingListActor).Assembly      // Sample.Host.App
    },
    configureServices
    // Lines below will disable MySQL projections if uncommented
    //,
    //disableActors: new[] {
    //    "Sample.Database.Projection.ShoppingListEFProjection",
    //    "Sample.Database.Projection.ShoppingListItemEFProjection"
    //}
    );
}

