using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;

namespace Sample.Domain.Model
{
    public class ShoppingList
    {
        [Key]
        public Guid Id { get; set; }
        public Guid AuthorId { get; set; }
        public string Title { get; set; }
        //public DbSet<ShoppingListItem> Items { get; set; }

        // public DbSet<Guid> Collaborators { get; set; }

    }
}