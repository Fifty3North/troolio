using ShoppingList.Shared.ActorInterfaces;
using Troolio.Core;

namespace ShoppingList.Host.App
{
    internal record AuthorRequestedJoinCode(Guid ListId) : Query<IAllShoppingListsActor, string>;
}
