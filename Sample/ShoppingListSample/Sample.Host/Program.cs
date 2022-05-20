using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sample.Database.Model;
using Sample.Database.Projection;
using Sample.Host.App.ShoppingList;
using Sample.Shared.ShoppingList;
using Troolio.Stores.EventStore;

Console.WriteLine("Running sample. Booting cluster might take some time ...\n");

// create db and run any pending migrations
new ShoppingListsContext().RunMigrations();

IConfiguration configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

Action<IServiceCollection> configureServices = (s) =>
{
    s.AddTransient<ShoppingListsContext>();
};


await Startup.StartWithDefaults(
    "Shopping", 
    new[] { 
        typeof(IShoppingListActor).Assembly, 
        typeof(ShoppingListActor).Assembly,
        typeof(IShoppingListEFProjection).Assembly
    },
    configureServices);