using Microsoft.EntityFrameworkCore;

namespace Sample.Database.Model
{
    public class ShoppingListsDbContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite("Data Source=shoppinglists.db");

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => base.OnModelCreating(modelBuilder);

        public void RunMigrations()
            => this.Database.Migrate();

        public DbSet<ShoppingList> ShoppingLists { get; set; }
        public DbSet<ShoppingListItem> ShoppingListItems { get; set; }

    }
}
