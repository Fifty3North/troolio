using Orleankka.Meta;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Shared.ShoppingList
{
    public record MyShoppingLists : Query<IUserActor, ImmutableList<ShoppingListQueryResult>>;
    public record ShoppingListDetails : Query<IShoppingListActor, ShoppingListQueryResult>;

    public record ShoppingListQueryResult(Guid Id, string Title, IEnumerable<ShoppingItemQueryItem> Items, ImmutableList<Guid> Collaborators);

    public record ShoppingItemQueryItem(Guid Id, string Name, ShoppingItemQueryItemState Status, uint Quantity);

    public enum ShoppingItemQueryItemState
    {
        Pending = 0,
        CrossedOff = 1,
        Deleted = 2
    }
}
