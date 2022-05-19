using System;
using System.ComponentModel.DataAnnotations;

namespace Sample.Domain.Model
{
    public enum ItemState
    {
        Pending = 0,
        CrossedOff = 1,
        Deleted = 2
    }

    public class ShoppingListItem
    {
        public Guid ShoppingListId { get; set; }
        [Key]
        public Guid Id { get; set; }
        public string Name { get; set; }
        public ItemState Status { get; set; }
        public uint Quantity { get; set; }
    }
}
