using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sample.Shared.ShoppingList;
using System.Collections.Immutable;
using Troolio.Core;
using Troolio.Core.Client;

// This application allows a user to create a shopping list and share it with
// family members, who can also add items to the list, or let other family members 
// know that they have purchased an item from the list by crossing an item off
// Owner can remove items from list (different to crossing items off)
// A list is owned on a single device so if the creator wants to use the list 
// on another device it must be shared so the second device becomes a collaborator,
// no transfer of ownership is supported

// Personas
// --------
// Owner (list author), Collaborator (invited to edit list)

// Requirements
// ------------
// Specific registration is not required. 
// Create a list (Requires: DeviceId, ListId and Title)
// List can be shared using a retrieved code (Authorisation: Owner, Requires: DeviceId, ListId)
// A user can join list as a collaborator (Requires: DeviceId, Code)
// Add item to list (Authorisation: owner, collaborator)
// Remove item from list (Authorisation: owner)
// Cross item off list (Authorisation: owner, collaborator)
// Notify that an item has been added
// Notify that an item has been crossed off
// Notify that an item has been removed

// Commands
// --------
// Create New List
// Add item to list
// Cross item off list
// Remove item from list
// Join List

// Events
// -------
// NewListCreated
// ItemAddedToList
// ItemCrossedOffLList
// ItemRemovedFromList
// ListJoined

// Errors
// ------
// CollaboratorCannotRemoveItemFromList
// InvalidCode
// ListAlreadyExists
// ItemAlreadyCrossedOff
// ItemNotFound (when removing item or crossing item off)

//          Screen 1: My Shopping Lists => UserListSummaries : List<UserListSummary> { }
//              UserListSummary { ListId, Title, OwnerOrCollaborator }

//          Screen 2: Shopping List : 
//	            Title,
//          	List<ShoppingListItem>,
//              SharingCode
//                  ShoppingListItem { ItemId, Description, Quantity, CrossedOff }

// User: Frank
// User: Talula
// User: Sheila

// Events
// ------
// 1.NewListCreated(Frank, ListId: 1)
// 2.NewListCreated(Talula, ListId: 4)
// 3.ItemAddedToList(ListId: 4, A)
// 4.NewListCreated(Sheila, ListId: 7)
// 5.ItemAddedToList(ListId: 1, C)
// 6.ItemRemovedFromList(ListId: 1, C)
// 7.ItemAddedToList(ListId: 1, D)
// 8.ItemAddedToList(ListId: 4, F)
// 9.ListJoined(Frank, ListId: 4)
// 10.ListJoined(Frank, ListId: 7)
// 11.ItemCrossedOffList(ListId: 4, A)

// Event Sourced Actors
// --------------------
// ListActor (Id: ListId; Events: NewListCreated | ItemAddedToList | ItemRemovedFromList | ItemCrossedOffList)
// ListId 1:  1, 5, 6, 7
// ListId 4:  2, 3, 8, 9, 11
// ListId 7:  4, 10
// => ListReadModel (Title, List<ShoppingListItem>, SharingCode)
//      ShoppingListItem { ItemId, Description, Quantity, CrossedOff }

// UserListsActor (Id: UserId; Events: NewListCreated | ListJoined)
// UserListsActor - Frank : 1, 9, 10
// UserListsActor - Talula: 2
// UserListsActor - Sheila: 4
// => UserListsActorReadModel (List<UserListSummary>)
//      UserListSummary { ListId, Title, OwnerOrCollaborator }










// generate the builder
var hostBuilder = Host.CreateDefaultBuilder(args);
hostBuilder.ConfigureServices(services => services.AddTroolioClient("Shopping", new[] { typeof(IShoppingListActor).Assembly }));
var host = hostBuilder.Build();
host.Start();

// get the client
var client = host.Services.GetRequiredService<ITroolioClient>();





// Example 1. Create a new list
// Actors:      ShoppingListActor
// Commands:    CreateNewList
// Events:      NewListCreated
// Orchestrates to:
//      Actor:      AllShoppingListsActor
//      Command:    AddShoppingList
//      Actor:      UserActor
//      Command:    RecordListId
Guid example1ListId = Guid.NewGuid();
Troolio.Core.Metadata example1User = new Troolio.Core.Metadata(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
await client.Tell(example1ListId.ToString(), new CreateNewList(example1User with { CorrelationId = Guid.NewGuid() }, "Example 1 List"));



// Example 2. Add item to list
// Actors:      ShoppingListActor
// Commands:    AddItemToList
// Events:      ItemAddedToList
// Exceptions:  ItemAlreadyExistsException, UnauthorizedAccessException
Guid example2ListId = Guid.NewGuid();
Troolio.Core.Metadata example2User = new Troolio.Core.Metadata(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
await client.Tell(example2ListId.ToString(), new CreateNewList(example2User with { CorrelationId = Guid.NewGuid() }, "Example 2 List"));
await client.Tell(example2ListId.ToString(), new AddItemToList(example2User with { CorrelationId = Guid.NewGuid() }, "Example 2 Item 1", 1));



// Example 3. Remove an item from a list.
// Actors:      ShoppingListActor
// Commands:    RemoveItemFromList
// Events:      ItemRemovedFromList
// Exceptions:  CollaboratorCannotRemoveItemFromListException, ItemDoesNotExistException, UnauthorizedAccessException
Guid example3ListId = Guid.NewGuid();
Troolio.Core.Metadata example3User = new Troolio.Core.Metadata(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
await client.Tell(example3ListId.ToString(), new CreateNewList(example3User with { CorrelationId = Guid.NewGuid() }, "Example 3 List"));
await client.Tell(example3ListId.ToString(), new AddItemToList(example3User with { CorrelationId = Guid.NewGuid() }, "Example 3 Item 1", 1));
ShoppingList example3ShoppingList = await client.Get<ShoppingList>(example3ListId.ToString());
await client.Tell(example3ListId.ToString(), new RemoveItemFromList(example3User with { CorrelationId = Guid.NewGuid() }, example3ShoppingList.Items.First().Id));



// Example 4. Join list.
// Actors:      AllShoppingListsActor
// Commands:    JoinListUsingCode
// Events:      ListJoinedUsingCode
// Exceptions:  InvalidJoinCodeException
// Orchestrates to:
//      Actor:      ShoppingListActor
//      Command:    JoinList
Guid example4ListId = Guid.NewGuid();
Troolio.Core.Metadata example4User = new Troolio.Core.Metadata(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
Troolio.Core.Metadata example4User2 = new Troolio.Core.Metadata(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
await client.Tell(example4ListId.ToString(), new CreateNewList(example4User with { CorrelationId = Guid.NewGuid() }, "Example 4 List"));
ShoppingList example4ShoppingList = await client.Get<ShoppingList>(example4ListId.ToString());
await client.Tell(Constants.SingletonActorId, new JoinListUsingCode(example4User2 with { CorrelationId = Guid.NewGuid() }, example4ShoppingList.JoinCode));



// Example 5. Cross item off list
// Actors:      ShoppingListActor
// Commands:    RemoveItemFromList
// Events:      ItemRemovedFromList
// Exceptions:  CollaboratorCannotRemoveItemFromListException, ItemDoesNotExistException, UnauthorizedAccessException
Guid example5ListId = Guid.NewGuid();
Troolio.Core.Metadata example5User = new Troolio.Core.Metadata(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
await client.Tell(example5ListId.ToString(), new CreateNewList(example5User with { CorrelationId = Guid.NewGuid() }, "Example 5 List"));
await client.Tell(example5ListId.ToString(), new AddItemToList(example5User with { CorrelationId = Guid.NewGuid() }, "Example 5 Item 1", 1));
ShoppingList example5ShoppingList = await client.Get<ShoppingList>(example5ListId.ToString());
await client.Tell(example5ListId.ToString(), new CrossItemOffList(example5User with { CorrelationId = Guid.NewGuid() }, example5ShoppingList.Items.First().Id));



// Example 6. Get User lists using query
// Actors:      UserActor
// Commands:    MyShoppintLists
Guid example6ListId = Guid.NewGuid();
Troolio.Core.Metadata example6User = new Troolio.Core.Metadata(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
await client.Tell(example6ListId.ToString(), new CreateNewList(example6User with { CorrelationId = Guid.NewGuid() }, "Example 6 List"));
await client.Tell(example6ListId.ToString(), new AddItemToList(example6User with { CorrelationId = Guid.NewGuid() }, "Example 6 Item 1", 1));
await client.Tell(example6ListId.ToString(), new AddItemToList(example6User with { CorrelationId = Guid.NewGuid() }, "Example 6 Item 2", 1));
ShoppingList example6ShoppingList = await client.Get<ShoppingList>(example6ListId.ToString());
await client.Tell(example6ListId.ToString(), new CrossItemOffList(example6User with { CorrelationId = Guid.NewGuid() }, example6ShoppingList.Items.First().Id));
ImmutableList<ShoppingListQueryResult> example6UserShoppingLists = await client.Ask<IUserActor, ImmutableList<ShoppingListQueryResult>>(example6User.UserId.ToString(), new MyShoppingLists());



// Example 7. Get User list using read model
// Actors:      ShoppingListActor
// Commands:    RemoveItemFromList
// Events:      ItemRemovedFromList
// Exceptions:  CollaboratorCannotRemoveItemFromListException, ItemDoesNotExistException, UnauthorizedAccessException
Guid example7ListId = Guid.NewGuid();
Troolio.Core.Metadata example7User = new Troolio.Core.Metadata(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
await client.Tell(example7ListId.ToString(), new CreateNewList(example7User with { CorrelationId = Guid.NewGuid() }, "Example 7 List"));
await client.Tell(example7ListId.ToString(), new AddItemToList(example7User with { CorrelationId = Guid.NewGuid() }, "Example 7 Item 1", 1));
await client.Tell(example7ListId.ToString(), new AddItemToList(example7User with { CorrelationId = Guid.NewGuid() }, "Example 7 Item 2", 1));
ShoppingList example7ShoppingList = await client.Get<ShoppingList>(example7ListId.ToString());
await client.Tell(example7ListId.ToString(), new CrossItemOffList(example7User with { CorrelationId = Guid.NewGuid() }, example7ShoppingList.Items.First().Id));
example7ShoppingList = await client.Get<ShoppingList>(example7ListId.ToString());



Console.WriteLine("Shopping List Sample test run complete.");
