using Troolio.Core;

namespace Sample.Shared.Events;

public record ItemAddedToList(Guid ItemId, string Description, ushort Quantity, Metadata Headers) : TroolioEvent(Headers);
public record ItemCrossedOffList(Guid ItemId, Metadata Headers) : TroolioEvent(Headers);
public record ItemRemovedFromList(Guid ItemId, Metadata Headers) : TroolioEvent(Headers);
public record ListIdRecorded(Guid ListId, Metadata Headers) : TroolioEvent(Headers);
public record ListJoined(Metadata Headers) : TroolioEvent(Headers);
public record ListJoinedUsingCode(Guid ListId, Metadata Headers) : TroolioEvent(Headers);
public record NewListCreated(string Title, Metadata Headers) : TroolioEvent(Headers);
public record ShoppingListAdded(Guid ListId, string joinCode, Metadata Headers) : TroolioEvent(Headers);