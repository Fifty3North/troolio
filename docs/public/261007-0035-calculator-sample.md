# Run the Calculator sample

The smallest runnable Troolio example: send two commands, record events, and query an actor's sum.

## Run

Install the .NET 10 SDK, then run from the repository root:

```sh
dotnet run --project Sample/Calculator
```

The program prints `42 + 19 = 61` and exits without requiring keyboard input. Run one default local silo at a time; its Orleans ports are 9000 and 9001.

## Follow the code

```csharp
var headers = new Metadata(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
await client.Tell(actorId, new RecordInteger(headers, 42));
var sum = await client.Ask(actorId, new Sum());
```

The correlation ID groups related work, the user ID identifies the caller already established by your application, and the device ID identifies its client. This console sample creates these IDs locally; they are not an authentication system. The runtime assigns message lineage as it handles each command and event.

```csharp
public IEnumerable<Event> Handle(RecordInteger command)
    => [new IntegerRecorded(command.Headers, command.Value)];
public void On(IntegerRecorded e)
    => State = State with { Total = checked(State.Total + e.Value) };
```

`Handle` decides what happened. `On` restores only the actor's own state and can be repeated during replay without outside effects. This sample's default store is in memory: restart loses its events. The [ToDo sample](todo-sample.html) adds relational read models and durable store choices. The [Shopping List sample](shopping-sample.html) adds collaboration and orchestration.

## Get the complete source

[Open Calculator on GitHub](https://github.com/Fifty3North/troolio/tree/main/Sample/Calculator). The sample references Troolio.Core 10.0.8 and generates its message serializers with the Orleans 10 SDK.
