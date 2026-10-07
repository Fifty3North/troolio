using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace Sample.Database.Model;

public sealed class ShoppingListsDbContext(DbContextOptions<ShoppingListsDbContext> options) : DbContext(options)
{
    public DbSet<ShoppingList> ShoppingLists => Set<ShoppingList>();
    public DbSet<ShoppingListItem> ShoppingListItems => Set<ShoppingListItem>();
    public DbSet<ShoppingListMember> ShoppingListMembers => Set<ShoppingListMember>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<ShoppingList>().HasKey(x => x.Id);
        model.Entity<ShoppingListItem>().HasKey(x => x.Id);
        model.Entity<ShoppingListMember>().HasKey(x => x.Id);
        model.Entity<ShoppingListMember>().HasIndex(x => new { x.UserId, x.ShoppingListId }).IsUnique();
    }

    public static string ConnectionString(IConfiguration configuration)
    {
        var configured = configuration["Shopping:ReadModels:Path"];
        var path = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TroolioSamples", "shopping", "readmodels.db")
            : Path.GetFullPath(configured);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        return $"Data Source={path};Default Timeout=30";
    }
}

public sealed class ShoppingListMember
{
    public Guid Id { get; set; }
    public Guid ShoppingListId { get; set; }
    public Guid UserId { get; set; }

    // Domain identity, independent of delivery retries or diagnostic message headers.
    public static Guid Key(Guid listId, Guid userId)
        => new(SHA256.HashData(Encoding.UTF8.GetBytes($"{listId:N}:{userId:N}"))[..16]);
}
