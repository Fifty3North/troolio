using System;
using System.ComponentModel.DataAnnotations;

namespace Sample.Database.Model
{
    public class ShoppingList
    {
        [Key]
        public Guid Id { get; set; }
        public Guid AuthorId { get; set; }
        public string Title { get; set; }
    }
}