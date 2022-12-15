using ShoppingListSample.Shared.ActorInterfaces;
using Troolio.Core;

namespace ShoppingListSample.Host.App.ShoppingList
{
    internal record AuthorRequestedJoinCode(Guid ListId) : Query<IAllShoppingListsActor, string>;
}
