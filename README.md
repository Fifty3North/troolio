# Troolio
This project contains projections, stores and samples to be used with Trool.io.  Trool.io is an opinionated CQRS and Event Sourcing library running on top of Microsoft Orleans - the virtual actor framework.  Lowering the barrier to entry for Event Sourcing and CQRS, Trool.io lets you quickly and safely build robust and scalable distributed systems without extensive knowledge of the underlying technology.

## Concepts
All functionality is performed by passing immutable messages to actors. Immutable means that messages cannot be modified. There are 3 types of message:-
* Command e.g. AddToDoItem
* Event e.g. ToDoItemCompleted 
* Query e.g. GetAllOutstandingToDoItems

## Actors
Actors are made up of collections of functionality which handle messages and optionally state. 

An actor can receive messages from clients as Commands or Queries. Commands are defined to allow users to modify the state of the system. They contain all the information required to modify the state of the actor and validate that the command is valid given the current actor state.

Queries allow the client to retrieve information from the system. 

Events are only ever raised as a result of a command being handled by the actor.

Stateful actors can be created by passing a create command to the actor. For example a command "CreateNewList" may be used to create a "ShoppingListActor". If you tried to rename a shopping list that hadn't first had a create command, you would receive an error message stating that this shoping list has not been created.

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

    if (this.State.Items.Any(i => i.Name.ToLower() == command.payload.Description))
    {
        throw new ItemAlreadyExistsException();
    }

    yield return new ItemAddedToList(Guid.NewGuid(), command.payload.Description, command.payload.Quantity, command.Headers);
}
```

It's the command handler's responsibility to check that given the current state of the actor the command is valid and can be processed. At this point one or more events is returned from the command handler or an exception can be raised to indicate an invalid command or invalid state.

Let's look at an event:

```
public record ItemAddedToList(Metadata Headers, string Name, int Quantity) : Event(Headers);`
```

And the State object:

```
public record ShoppingListState(ImmutableList<ShoppingListItemState> Items)
```

And the event handler:

```
public void On(ItemAddedToList ev) 
{
    State = State with { Items = State.Items.Add(new ShoppingListItemState(ev.ItemId, ev.Description, ItemState.Pending, ev.Quantity)) };
}
```

Getting started

Create 3 projects:
Shared
Client
Server

Install NuGet package Troolio.Core to the shared project.

Reference the shared project from Client and Server.

