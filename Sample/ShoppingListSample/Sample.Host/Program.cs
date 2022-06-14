﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sample.Shared.ActorInterfaces;
using Sample.Database.Model;
using Sample.Host.App.ShoppingList;
using Troolio.Stores.EventStore;

Console.WriteLine("Running sample. Booting cluster might take some time ...\n");

IConfiguration configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

// create db and run any pending migrations
new ShoppingListsDbContext(configuration)
    .RunMigrations();

Action<IServiceCollection> configureServices = (s) =>
{
    s.AddDbContext<ShoppingListsDbContext>()
    .AddSingleton<F3N.Core.Logging.ILogger>(new F3N.Core.Logging.LoggerMock())
    .AddSingleton<F3N.Providers.MessageQueue.IMessageQueueProvider>(new F3N.Providers.MessageQueue.InMemoryMessageQueueProvider());
};


await Startup.StartWithDefaults("Shopping",
    new[] {
        typeof(IShoppingListActor).Assembly,    // Sample.Shared
        typeof(ShoppingListActor).Assembly      // Sample.Host.App
    },
    configureServices);