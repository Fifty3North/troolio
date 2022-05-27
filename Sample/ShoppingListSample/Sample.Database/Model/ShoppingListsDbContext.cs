using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Sample.Database.Model;
public class ShoppingListsDbContext : DbContext
{
    protected readonly IConfiguration Configuration;
    private string _connectionString = "Server=localhost;Database=shopping;User=root;Password=secret;";

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

    public void RunMigrations()
        => this.Database.Migrate();

    public DbSet<ShoppingList> ShoppingLists { get; set; }
    public DbSet<ShoppingListItem> ShoppingListItems { get; set; }

}
