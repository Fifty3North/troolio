using Microsoft.EntityFrameworkCore;

namespace Sample.Domain.Model
{
    public class ShoppingListsContext : DbContext
    {
        public DbSet<ShoppingList> ShoppingLists { get; set; }
        public DbSet<ShoppingListItem> ShoppingListItems { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite("Data Source=shoppinglists.db");

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }

        public void RunMigrations()
        {
            this.Database.Migrate();
        }
    }

}
