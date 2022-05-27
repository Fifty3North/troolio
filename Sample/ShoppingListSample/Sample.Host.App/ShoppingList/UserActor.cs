using Microsoft.Extensions.Configuration;
using Orleankka;
using Sample.Shared.ActorInterfaces;
using Sample.Shared.Events;
using Sample.Shared.InternalCommands;
using Sample.Shared.Queries;
using System.Collections.Immutable;
using Troolio.Core;
using Troolio.Core.Stateful;
using Troolio.Stores;

namespace Sample.Host.App.ShoppingList;

public class UserActor : StatefulActor<UserState>, IUserActor
{
    public UserActor(IStore store, IConfiguration configuration) : base(store, configuration) { State = new UserState(ImmutableList<Guid>.Empty); }

    #region Commands ...
    public IEnumerable<TroolioEvent> Handle(RecordListId command) => new[] { new ListIdRecorded(command.ListId, command.Headers) };
    #endregion

    #region Events ...
    public void On(ListIdRecorded ev) => State = State with { Lists = State.Lists.Add(ev.ListId) };
    #endregion

    #region Queries ...
    public async Task<ImmutableList<ShoppingListQueryResult>> Handle(MyShoppingLists query)
    {
        return (
          await Task.WhenAll(
            this.State.Lists.Select(async (l) =>
                await this.System.TypedActorOf<IShoppingListActor>(l.ToString())
                    .Ask(new ShoppingListDetails())
            )
          )
        ).ToImmutableList<ShoppingListQueryResult>();
    }
    #endregion
}
