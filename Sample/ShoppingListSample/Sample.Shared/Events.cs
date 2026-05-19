using Orleans;
using Troolio.Core;

namespace Sample.Shared.Events;

[GenerateSerializer]
public record ItemAddedToList(Guid ItemId, string Description, ushort Quantity, Metadata Headers) : Event(Headers);

[GenerateSerializer]
public record ItemCrossedOffList(Guid ItemId, Metadata Headers) : Event(Headers);

[GenerateSerializer]
public record ItemRemovedFromList(Guid ItemId, Metadata Headers) : Event(Headers);

[GenerateSerializer]
public record ListIdRecorded(Guid ListId, Metadata Headers) : Event(Headers);

[GenerateSerializer]
public record ListJoined(Metadata Headers) : Event(Headers);

[GenerateSerializer]
public record ListJoinedUsingCode(Guid ListId, Metadata Headers) : Event(Headers);

[GenerateSerializer]
public record NewListCreated(string Title, Metadata Headers) : Event(Headers);

[GenerateSerializer]
public record ShoppingListAdded(Guid ListId, string JoinCode, Metadata Headers) : Event(Headers);
