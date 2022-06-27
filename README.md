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
Actors are made up of collections of functionality which handle messages.  Actors can implement one of 5 interfaces:-
* IActor - This type of actor doesn't have any state so is used as a worker or to project outside the system
* IStatefulActor - This type of actor maintains its own state so can include validation as to when actions can be performed
* ICreatableActor - This type of actor maintains its own state but also must have an explicit create command and will raise an exception if other actions are requested before the actor has been created.
* IOrchestrationActor - This type of actor responds to events raised by other actors.  It doesn't maintain state and an error will cause the transaction to fail
* IProjectionActor - This type of actor responds to events raised by other actors but only once a transaction has completed successfully

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

### Orchestrations
Each actor has an event stream which is populated from the events raised by the commands when called, this event stream can then be watched by an orchestrator which can raise commands or events when an event is encountered.  Orchestrations can listen to one or more actor event streams.  It is important to note that orchestrations are part of the transaction so if the orchestration fails the entire transaction will fail.

An orchestration can look like this:

```
[RegexImplicitStreamSubscription("ShoppingListActor-.*")]
[Reentrant]
[StatelessWorker]
public class AllShoppingListsOrchestrationActor : OrchestrationActor
{
    public async Task On(EventEnvelope<NewListCreated> e)
    {
        await System.ActorOf<IAllShoppingListsActor>(Constants.SingletonActorId).Tell(new AddShoppingList(e.Event.Headers, Guid.Parse(e.Id)));
    }
}
```

In the above example we can see the following: 
* It subscribes, i.e. listens to the ShoppingListActor stream
* It is Reentrant so can process more than one message at a time
* It is a StatelessWorker so doesn't change between calls and can be instantiated many times to improve throughput.

### Projections
Projections are similar to orchestrations but differ in one major way.  Projections are not contained in the transaction so a projection will only take place if a transaction has completed successfully.  Projections can be used for such things as sending an email when a user has been successfully created in the system.

A projection looks like this:

```
[RegexImplicitStreamSubscription("AllShoppingListsActor-.*")]
public class ShoppingListProjectionActor : ProjectionActor
{
    async Task On(EventEnvelope<ListJoinedUsingCode> e)
    {
        string email = "dummy@somewhere.com";

        ShoppingListQueryResult result = await System.ActorOf<IShoppingListActor>(e.Event.ListId.ToString()).Ask<ShoppingListQueryResult>(new ShoppingListDetails());

        await System.ActorOf<IEmailActor>(Constants.SingletonActorId).Tell(new SendEmailNotification(e.Event.Headers, email, result.Title));
    }
}
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

And the orchestrator. This maps from a property of the source event to the primary key of the read model:

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

## Creating a Server (Host)
In order to run a Trool.io project you first need to create a server.  An example can be found in the Sample.Host project.  The server is a command line application which configures all the actors and runs them waiting for a client.

### Local Development (without docker)

To run locally (without docker) you can configure the host as:

```
    var host = Host
        .CreateDefaultBuilder(args)
        .TroolioServer(appName, new[] {
	    typeof(IShoppingListActor).Assembly,    // Sample.Shared
	    typeof(ShoppingListActor).Assembly      // Sample.Host.App
        }, configureServices);

    await host.RunAsync();
```

appsettings.json needs the following:
```
{
  "{appName}:Clustering": {
    "Storage": "Local"
  },
  "snapshotThreshold":  "100"
}
```

This configures the application to use in-memory event database, in-memory node discovery and other in-memory infrastructure which supports the framework. This is for local development only and all state will be lost when the application exits.

### Local Development (with docker)

This spins up a local development docker cluster which mirrors a production setup more closely. 

In the sample there are 5 containers:
* api: hosts the HTTP REST API which clients can call
* host: the Troolio application (a node)
* azurite: an Azure Storage emulator to hold node information
* eventstore: the event database
* mysql: sample projections write to this external database

Right click on docker-compose project in Visual Studio and select "Set as Startup Project" and debug as usual

OR

From the command line type: `docker-compose -f "Sample/ShoppingListSample/docker-compose.yml" up -d --build`

This uses the EventStore shortcut to start the server using the "StartWithDefaults" passing in the application name, the list of assemblies and the service delegates:

```
await Startup.StartWithDefaults("Shopping",
    new[] {
        typeof(IShoppingListActor).Assembly,    // Sample.Shared
        typeof(ShoppingListActor).Assembly      // Sample.Host.App
    },
    configureServices);
```

appsettings.json requires more information:
```
{
  "{appName}:Clustering": {
    "ConnectionString": "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;TableEndpoint=http://azurite:10002/devstoreaccount1",
    "ClusterId": "shopping-dev",
    "ServiceId": "shopping-service-dev"
  }
}
```

The list of assemblies will be scanned to determine the relevant interfaces and implementations of those interfaces to be used by the application.  ConfigureServices allows mapping of services to delegates for dependancy injection.

### Configuration / Dependency Injection
The Sample.Host uses json configuration in the form of appsettings.json which will automatically be loaded in while starting the server if started with defaults.

You can configure services by passing in an Action<IServiceCollection> to the StartWithDefaults.  It allows mapping of interfaces and types so that you can inject implementations for different cases

This is how to specify a database context:

```
Action<IServiceCollection> configureServices = (s) =>
{
    s.AddDbContext<ShoppingListsDbContext>();
};
```

And then a singleton:

```
Action<IServiceCollection> configureServices = (s) =>
{
    s.AddSingleton<IAllShoppingListsActor, DummyAllShoppingListsActor>();
};
```

## Creating a Client
In order to use the server you need a client.  A client can be a command line application or an API.  When createing a client you only need to reference the assembly with the interfaces, the server determines the implementations.

### Command Line Client
To create a command line client you need to create a host builder:

```
var hostBuilder = Host.CreateDefaultBuilder(args);
hostBuilder.ConfigureServices(services => services.AddTroolioClient("Shopping", new[] { typeof(IShoppingListActor).Assembly }));
var host = hostBuilder.Build();
host.Start();
```

Once you have a host you need to attach to it to get the client:

```
var client = host.Services.GetRequiredService<ITroolioClient>();
```

### API Client
To add the client to the API you simply add a singleton implementation of ITroolioClient:

```
builder.Services.AddSingleton<ITroolioClient>(
    new TroolioClient(new[] { typeof(IAllShoppingListsActor).Assembly }, "Shopping", configurationBuilder));
```
	
appsettings.json needs to match that of the server for the `"{appName}:Clustering"` section. 

## Tracing
Tracing allows you to see all the commands and events that have been issued to the system.  It can be enabled by issueing an "EnableTracing" command to the Trool.io client:

```
await client.Tell(Constants.SingletonActorId, new EnableTracing());
```

And once enabled you can access it by issuing a Flush query:

```
var tracingLog = await _client.Ask(Constants.SingletonActorId, new Flush());
```

An example of using the trace to write out to a file is in the Sample.Api.  Should you wish to switch it off you can issue a "DisableTracing" command:

```
await client.Tell(Constants.SingletonActorId, new DisableTracing());
```

## Storage
Two types of store are built into Troolio. FileSystemStore writes streams to disk one file per stream and are persisted between app restarts. InMemoryStore uses a Dictionary to hold all stream data and all data is lost when the application exits. These are intended for development only.

A production grade store is provided using the Event Store client in the OSS Troolio.Stores.EventStore included in this repository.
	
Configuration of the EventStore could differ between implementations but below is an example of a single node EventStore configuration:


```
"Shopping:Storage": {
    "EventStoreCluster": "false",
    "EventStorePort": "1113",
    "EventStoreHosts": "eventstore"
}
```

## Clustering
You can use clustering to improve robustness by configuring the clustering setting in the appsettings.json:

For local clustering use the following:

```
"Shopping:Clustering": {
    "Storage": "Local",
}
```

You can also use Azure Table Storage to maintain the cluster when more than one host node is required:

```
"Shopping:Clustering": {
    "ConnectionString": "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;TableEndpoint=http://azurite:10002/devstoreaccount1",
    "ClusterId": "shopping-dev",
    "ServiceId": "shopping-service-dev"
  }
```

* ConnectionString - The connection string to access the cluster storage.  The example is the published Microsoft connectionstring for development and testing
* ClusterId - A unique id for the cluster
* ServiceId - a unique service id

## Snapshots
When actors become idle they get shut down to reduce memory usage, when next called they need to read events to get back to their latest state.  To reduce the number of events that need to be used to repopulate the actor Trool.io uses snapshots.  A snapshot is a point in time representation of the actors state which will be loaded and only events after the snapshot was taken will be actioned.  Snapshots will be taken every 100 events by default but this can be changed by specifying a SnapshotThreshhold in appsettings.json:

```
{
  "snapshotThreshold": 5
}
```

## Batch commands (Used for sending Emails etc)
Batch commands are out of process commands backed up by a message queue.  They will be actioned and if failed will be retried a certain number of times (default is 3 times).
To add batch jobs you first need to configure the queue.  To do this you need to dependancy inject an F3N.Providers.MessageQueue.IMessageQueueProvider, e.g.

```
Action<IServiceCollection> configureServices = (s) =>
{
    s.AddSingleton<F3N.Providers.MessageQueue.IMessageQueueProvider>(new F3N.Providers.MessageQueue.InMemoryMessageQueueProvider());
};
```
This example uses the InMemoryMessageQueueProvider but it shouldn't be used in production as messages are only contained on the machine so any reboot or interuption will cause loss of messages.
Once the message queue is setup you can then add batch jobs by:

Create a command you want to execute:
```
var command = new SendEmailNotification(e.Event.Headers, "someone@somewhere.com", "test email");
```

Retrieve the path to the actor that needs to execute the command:
```
var actorPath = System.ActorOf<IEmailActor>(Constants.SingletonActorId).Path;
```

And finally tell the batch job actor the actor path and command to execute:
```
await System.ActorOf<IBatchJobActor>(Constants.SingletonActorId)
	.Tell(new AddBatchJob(actorPath, command));
```

This will have a single batch job actor but you could use more.
