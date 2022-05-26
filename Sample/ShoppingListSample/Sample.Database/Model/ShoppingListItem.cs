using Sample.Shared.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace Sample.Database.Model
{
    public class ShoppingListItem
    {
        [Key]
        public string Id { get; set; }
        public string ShoppingListId { get; set; }
        public string Name { get; set; }
        public ItemState Status { get; set; }
        public uint Quantity { get; set; }
    }
}
