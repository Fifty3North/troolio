using System;
using System.ComponentModel.DataAnnotations;

namespace Sample.Database.Model
{
    public class ShoppingList
    {
        [Key]
        public string Id { get; set; }
        public string AuthorId { get; set; }
        public string Title { get; set; }
    }
}