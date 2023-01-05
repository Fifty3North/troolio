using Sample.Shared.ActorInterfaces;
using Troolio.Core;

namespace Sample.Host.App.ShoppingList
{
    internal record AuthorRequestedJoinCode(Guid ListId) : Query<IAllShoppingListsActor, string>;
}
