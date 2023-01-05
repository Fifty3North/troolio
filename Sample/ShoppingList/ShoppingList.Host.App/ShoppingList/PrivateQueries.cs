using ShoppingList.Shared.ActorInterfaces;
using Troolio.Core;

namespace ShoppingList.Host.App.ShoppingList
{
    internal record AuthorRequestedJoinCode(Guid ListId) : Query<IAllShoppingListsActor, string>;
}
