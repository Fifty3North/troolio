namespace ShoppingList.Shared.Models
{
    public record ShoppingListItemState(Guid Id, string Name, Enums.ItemState Status, uint Quantity);
}
