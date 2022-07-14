using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace Sample.Database.Model;
public class ShoppingListsDbContext : DbContext
{
    protected readonly IConfiguration Configuration;
    private string _connectionString = "Server=localhost;Database=shopping;User=root;Password=secret;Connection Timeout=30";

    // empty constructor for use with command line migrations
    public ShoppingListsDbContext() { }

    // configuration constructor for use with runtime builder
    public ShoppingListsDbContext(IConfiguration configuration)
    {
        Configuration = configuration;
        _connectionString = Configuration.GetConnectionString("MySQLConnection");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    => options.UseMySql(_connectionString, ServerVersion.AutoDetect(_connectionString));

    private Task Migrate()
    {
        return this.Database.MigrateAsync();
    }
    
    public async Task RunMigrations()
    {
        await ExecuteWithRetries(Migrate, async ex =>
        {  // replace Console with actual logging
            Console.WriteLine(ex);
            Console.WriteLine("Retrying MySQL...");
            await Task.Delay(2000);
            return true;
        });

        Console.WriteLine("MySQL Ready!!!");
    }

    async Task ExecuteWithRetries(Func<Task> task, Func<Exception, Task<bool>> shouldRetry)
    {
        while (true)
        {
            try
            {
                await task();
                return;
            }
            catch (Exception exception) when (shouldRetry != null)
            {
                var retry = await shouldRetry(exception);
                if (!retry) throw;
            }
        }
    }

    public DbSet<ShoppingList> ShoppingLists { get; set; }
    public DbSet<ShoppingListItem> ShoppingListItems { get; set; }

}
