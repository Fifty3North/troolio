namespace ShoppingList.Shared.WriteModels;

public record ItemToAdd(Guid ItemId, string Description, ushort Quantity);