using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Sample.Database.Model;
public class ShoppingListsDbContext : DbContext
{
    protected readonly IConfiguration Configuration;
    private string _connectionString = "Server=db;Database=shopping;User=root;Password=secret;";

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
