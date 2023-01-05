using Troolio.Core;

namespace ShoppingList.Shared.Events;

public record ItemAddedToList(Metadata Headers, Guid ItemId, string Description, ushort Quantity) : Event(Headers);
public record ItemCrossedOffList(Metadata Headers, Guid ItemId) : Event(Headers);
public record ItemRemovedFromList(Metadata Headers, Guid ItemId) : Event(Headers);
public record ListIdRecorded(Metadata Headers, Guid ListId) : Event(Headers);
public record ListJoined(Metadata Headers) : Event(Headers);
public record ListJoinedUsingCode(Metadata Headers, Guid ListId) : Event(Headers);
public record NewListCreated(Metadata Headers, string Title) : Event(Headers);
public record ShoppingListAdded(Metadata Headers, Guid ListId, string JoinCode) : Event(Headers);