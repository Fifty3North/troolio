using Microsoft.Extensions.Configuration;
using Orleans;
using Troolio.Core;
using Troolio.Core.State;
using Troolio.Stores;

public interface ITodoActor : IActor { }
[GenerateSerializer]
public sealed record CreateTodoCommand(Metadata Headers, [property: Id(0)] Guid TodoId,
    [property: Id(1)] string Title) : Command<ITodoActor>(Headers);
[GenerateSerializer]
public sealed record CompleteTodoCommand(Metadata Headers) : Command<ITodoActor>(Headers);
[GenerateSerializer]
public sealed record GetTodoState : Query<ITodoActor, TodoState>;
[GenerateSerializer]
public sealed record TodoCreated(Metadata Headers, [property: Id(0)] Guid TodoId,
    [property: Id(1)] Guid OwnerId, [property: Id(2)] string Title,
    [property: Id(3)] DateTimeOffset CreatedAt) : Event(Headers);
[GenerateSerializer]
public sealed record TodoCompleted(Metadata Headers, [property: Id(0)] Guid TodoId,
    [property: Id(1)] DateTimeOffset CompletedAt) : Event(Headers);
[GenerateSerializer]
public sealed record TodoState([property: Id(0)] Guid TodoId, [property: Id(1)] Guid OwnerId,
    [property: Id(2)] string Title, [property: Id(3)] bool IsCompleted,
    [property: Id(4)] DateTimeOffset CreatedAt, [property: Id(5)] DateTimeOffset? CompletedAt) : IActorState
{
    public static TodoState Empty => new(Guid.Empty, Guid.Empty, "", false, default, null);
    public TodoState Apply(TodoCreated e) => new(e.TodoId, e.OwnerId, e.Title, false, e.CreatedAt, null);
    public TodoState Apply(TodoCompleted e) => this with { IsCompleted = true, CompletedAt = e.CompletedAt };
}
public sealed class TodoActor : EventSourcedActor<TodoState>, ITodoActor
{
    public TodoActor(IStore store, IConfiguration configuration) : base(store, configuration) => State = TodoState.Empty;
    public IEnumerable<Event> Handle(CreateTodoCommand command)
    {
        if (command.TodoId == Guid.Empty || Id != command.TodoId.ToString() || command.Headers.UserId == Guid.Empty
            || string.IsNullOrWhiteSpace(command.Title) || command.Title.Length > 200)
            throw new ArgumentException("Todo ID, owner and title must be valid.");
        if (State.TodoId != Guid.Empty)
        {
            if (State.OwnerId != command.Headers.UserId) throw new UnauthorizedAccessException();
            if (State.Title != command.Title) throw new InvalidOperationException("This ID already has a different title.");
            return [];
        }
        return [new TodoCreated(command.Headers, command.TodoId, command.Headers.UserId, command.Title, DateTimeOffset.UtcNow)];
    }
    public IEnumerable<Event> Handle(CompleteTodoCommand command)
    {
        if (State.TodoId == Guid.Empty) throw new KeyNotFoundException("Todo does not exist.");
        if (State.OwnerId != command.Headers.UserId) throw new UnauthorizedAccessException();
        return State.IsCompleted ? [] : [new TodoCompleted(command.Headers, State.TodoId, DateTimeOffset.UtcNow)];
    }
    public void On(TodoCreated e) => State = State.Apply(e);
    public void On(TodoCompleted e) => State = State.Apply(e);
    public TodoState Handle(GetTodoState _) => State;
}
