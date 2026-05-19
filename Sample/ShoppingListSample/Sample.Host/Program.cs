using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sample.Shared.ActorInterfaces;
using Sample.Database.Model;
using Sample.Host.App.ShoppingList;
using Microsoft.Extensions.Hosting;
using Troolio.Core;
using Troolio.MessageQueue;

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
    .AddSingleton<IMessageQueueProvider>(new InMemoryMessageQueueProvider())
    // Orleans incoming call fiulter to log messages
    //.AddSingleton<IIncomingGrainCallFilter, LoggingCallFilter>()
    ;
};

// Run in local mode for source-based execution while sample dependencies are modernized.
var host = Host.CreateDefaultBuilder(args)
    .TroolioServer("Shopping", new[] {
        typeof(IShoppingListActor).Assembly,    // Sample.Shared
        typeof(ShoppingListActor).Assembly      // Sample.Host.App
    }, configureServices,
    disableActors: new[] {
        "Sample.Database.Projection.ShoppingListEFProjection",
        "Sample.Database.Projection.ShoppingListItemEFProjection"
    });

await host.RunAsync();

