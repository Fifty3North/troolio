using Orleankka;
using Orleans;
using Troolio.Core.Creatable;
using Troolio.Core.Stateful.Interfaces;

namespace Sample.Shared.ActorInterfaces;

public interface IAllShoppingListsActor : IStatefulActor, IActorGrain, IGrainWithStringKey { }
public interface IEmailActor : IActorGrain, IGrainWithStringKey { }
public interface IUserActor : IStatefulActor, IActorGrain, IGrainWithStringKey { }
public interface IShoppingListActor : ICreatableActor, IStatefulActor, IActorGrain, IGrainWithStringKey { }
public interface IShoppingListItemEFProjection : IActorGrain, IGrainWithStringKey { }
public interface IShoppingListEFProjection : IActorGrain, IGrainWithStringKey { }
public interface IShoppingReadModelOrchestrator : IActorGrain, IGrainWithStringKey { }
