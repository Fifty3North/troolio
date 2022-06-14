using Troolio.Core;
using Troolio.Core.Creatable;
using Troolio.Core.Stateful.Interfaces;

namespace Sample.Shared.ActorInterfaces;

public interface IAllShoppingListsActor : IStatefulActor { }
public interface IEmailActor : IActor { }
public interface IShoppingListActor : ICreatableActor { }
public interface IShoppingListItemEFProjection : IProjectionActor { }
public interface IShoppingListEFProjection : IProjectionActor { }
public interface IShoppingListOrchestrationActor : IOrchestrationActor { }
public interface IShoppingListProjectionActor : IProjectionActor { }
public interface IShoppingReadModelOrchestrator : IOrchestrationActor { }
public interface IUserActor : IStatefulActor { }
public interface IUserProjectionActor : IProjectionActor { }
public interface IUserOrchestrationActor : IOrchestrationActor { }