# Troolio
This project contains projections, stores and samples to be used with Trool.io.  Trool.io is an opinionated CQRS and Event Sourcing library running on top of Microsoft Orleans - the virtual actor framework.  Lowering the barrier to entry for Event Sourcing and CQRS, Trool.io lets you quickly and safely build robust and scalable distributed systems without extensive knowledge of the underlying technology.

## Concepts
All functionality is performed by passing immutable messages to actors. Immutable means that messages cannot be modified. There are 3 types of message:-
* Command e.g. AddToDoItem
* Event e.g. ToDoItemAdded 
* Query e.g. GetAllOutstandingToDoItems

Trool.io is fully tracible and transaction based.  Tracibility is achieved through the use of the header metadata which identifies who performed an action and from which device, they also contain a unique collation ID which can be followed through the system.  

In addition to queries to retrieve information from the system there are also read models.  Read models allow you to see a model built from one or more actors.

## Actors
Actors are made up of collections of functionality which handle messages.  Actors can implement one of 3 interfaces:-
* IActor - This type of actor doesn't have any state so is used as a worker or to project outside the system
* IStatefulActor - This type of actor maintains its own state so can include validation as to when actions can be performed
* ICreatableActor - This type of actor maintains its own state but also must have an explicit create command and will raise an exception if other actions are requested before the actor has been created.

Messages from clients can be Commands or Queries. 
### Commands
Commands are defined to allow users to perform an action on the system and optionally update state. They must contain all the information required to validate whether the action can be performed and then perform the action.

Let's take a look at how an actor handles a command:

Take the command AddItemToList. 

```
public record AddItemToList(Metadata Headers, string Name, int Quantity) : Command<ShoppingListActor>(Headers);
```

and the command handler in the ShoppingListActor:

```
public IEnumerable<Event> Handle(AddItemToList command)
{
    if (!(this.State.Author == command.Headers.UserId || this.State.Collaborators.Contains(command.Headers.UserId)))
    {
        throw new UnauthorizedAccessException();
    }

    if (this.State.Items.Any(i => i.Name == command.payload.Description))
    {
        throw new ItemAlreadyExistsException();
    }

    yield return new ItemAddedToList(Guid.NewGuid(), command.payload.Description, command.payload.Quantity, command.Headers);
}
```

It's the command handler's responsibility to check that given the current state of the actor the command is valid and can be processed. At this point one or more events is returned from the command handler or an exception can be raised to indicate an invalid command or invalid state.
### Queries
Queries allow the client to retrieve information from the system. 
### Events
Events are only ever raised as a result of a command being handled by the actor.  The command can raise multiple events for a single action to keep things atomic.  Events can be listened for and can then trigger other actions to be performed.

Let's look at an event:

```
public record ItemAddedToList(Metadata Headers, string Name, int Quantity) : Event(Headers);`
```

And the event handler:

```
public void On(ItemAddedToList ev) 
{
    State = State with { Items = State.Items.Add(new ShoppingListItemState(ev.ItemId, ev.Description, ItemState.Pending, ev.Quantity)) };
}
```

And the State object:

```
public record ShoppingListState(ImmutableList<ShoppingListItemState> Items)
```

### Read Models
As mentioned previously, read models allow the user to retrieve data from the system.  The "ReadModelOrchestrator" is a special type of orchestration (an orchestration is an event which is triggered by another event while listening to an actors event stream).  It will watch one or many actors event streams and act on events to update a model which can then be read at any point.

Lets take a look at a read model:

```
public record ShoppingList : TroolioReadModel
{
    public ShoppingList(Guid id)
        => Id = id;

    public Guid Id { get; }
    public string Title { get; private set; }
    public Guid OwnerId { get; private set; }
    public ImmutableArray<ShoppingListItem> Items { get; private set; } = ImmutableArray<ShoppingListItem>.Empty;
    public string JoinCode { get; private set; }
    
    public ShoppingList On(EventEnvelope<NewListCreated> ev) 
        => this with { Title = ev.Event.Title, OwnerId = ev.Event.Headers.UserId };
    public ShoppingList On(EventEnvelope<ItemAddedToList> ev) 
        => this with { Items = Items.Add(new ShoppingListItem(ev.Event.ItemId).On(ev)) };
    public ShoppingList On(EventEnvelope<ItemRemovedFromList> ev) 
        => this with { Items = Items.RemoveAt( Items.FindIndex((i) => i.Id == ev.Event.ItemId)) };
    public ShoppingList On(EventEnvelope<ShoppingListAdded> ev) 
        => this with { JoinCode = ev.Event.joinCode };
}
```

And the orchestrator:

```
[RegexImplicitStreamSubscription("ShoppingListActor-.*")]
[RegexImplicitStreamSubscription("AllShoppingListsActor-.*")]
public class ShoppingReadModelOrchestrator 
    : ReadModelOrchestrator<ShoppingList>, IShoppingReadModelOrchestrator, IGrainWithGuidCompoundKey
{
    public ShoppingReadModelOrchestrator(IStore store) : base(store) { }

    public string Handle(EventEnvelope<NewListCreated> ev) 
        => ev.Id.ToString();
    public string Handle(EventEnvelope<ItemAddedToList> ev) 
        => ev.Id.ToString();
    public string Handle(EventEnvelope<ItemRemovedFromList> ev) 
        => ev.Id.ToString();
    public string Handle(EventEnvelope<ShoppingListAdded> ev) 
        => ev.Event.ListId.ToString();

}
```