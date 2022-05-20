using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sample.Database.Model;
using Sample.Host.App.ShoppingList;
using Sample.Shared.ShoppingList;
using Troolio.Stores.EventStore;

Console.WriteLine("Running sample. Booting cluster might take some time ...\n");

// create db and run any pending migrations
new ShoppingListsDbContext().RunMigrations();

IConfiguration configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

Action<IServiceCollection> configureServices = (s) =>
    { s.AddTransient<ShoppingListsDbContext>(); };


await Startup.StartWithDefaults("Shopping", 
    new[] { 
        typeof(IShoppingListActor).Assembly,    // Sample.Shared
        typeof(ShoppingListActor).Assembly      // Sample.Host.App
    },
    configureServices);