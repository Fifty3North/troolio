using Troolio.Core;

namespace Sample.Shared.Events;

public record ItemAddedToList(Guid ItemId, string Description, ushort Quantity, Metadata Headers) : Event(Headers);
public record ItemCrossedOffList(Guid ItemId, Metadata Headers) : Event(Headers);
public record ItemRemovedFromList(Guid ItemId, Metadata Headers) : Event(Headers);
public record ListIdRecorded(Guid ListId, Metadata Headers) : Event(Headers);
public record ListJoined(Metadata Headers) : Event(Headers);
public record ListJoinedUsingCode(Guid ListId, Metadata Headers) : Event(Headers);
public record NewListCreated(string Title, Metadata Headers) : Event(Headers);
public record ShoppingListAdded(Guid ListId, string JoinCode, Metadata Headers) : Event(Headers);