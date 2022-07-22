using Troolio.Core;
using Troolio.Core.Creatable;
using Troolio.Core.Stateful.Interfaces;

namespace Sample.Shared.ActorInterfaces;

public interface IAllShoppingListsActor : IStatefulActor { }
public interface IEmailActor : IActor { }
public interface IShoppingListActor : ICreatableActor { }
public interface IUserActor : IStatefulActor { }
public interface IPingActor : IActor { }