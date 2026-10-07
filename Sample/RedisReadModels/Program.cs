using Microsoft.Extensions.Configuration;
using Troolio.Projection.Redis.Models;
using Troolio.Projection.Redis.Providers;

var configuration = new ConfigurationBuilder().AddEnvironmentVariables().AddCommandLine(args).Build();
var endpoint = configuration.GetConnectionString("Redis") ?? "localhost:6379";
using var connection = new RedisConnectionProvider(endpoint);
var cards = new RedisProvider<Card>(connection);
var partitionId = Guid.NewGuid();
var cardId = Guid.NewGuid();
var card = new Card { Id = cardId, Title = "Learn Redis read models", IsComplete = false };
Console.WriteLine($"Partition {partitionId}; card {cardId}");
var createdChange = await cards.CreateEntity(cardId, card, partitionId);
var stored = await cards.GetEntityAsync(cardId);
Require(stored?.Title == card.Title && !stored.IsComplete, "Created card should be readable.");
Console.WriteLine($"Created: {stored!.Title}");
card.IsComplete = true;
var updatedChange = await cards.UpdateEntity(cardId, card, partitionId, value => value.IsComplete);
stored = await cards.GetEntityAsync(cardId);
Require(stored?.IsComplete == true, "Updated property should be committed.");
Console.WriteLine($"Updated: completed={stored!.IsComplete}");
var deletedChange = await cards.DeleteEntity(cardId, partitionId);
Require(await cards.GetEntityAsync(cardId) is null, "Deleted card should be absent.");
Console.WriteLine("Deleted: absent");

long offset = 0;
var changes = new List<ChangeHashEntry>();
// Three committed changes in this new partition; each request reads at most two entries.
for (var request = 0; request < 3; request++)
{
    var page = await cards.GetChangePageAsync(partitionId, offset, maxCount: 2);
    Require(page.Entries.Count <= 2 && page.NextOffset == offset + page.Entries.Count, "Page bounds and offset must hold.");
    changes.AddRange(page.Entries);
    Console.WriteLine($"Changes at offset {offset}: {page.Entries.Count}; next={page.NextOffset}; mayHaveMore={page.HasMore}");
    offset = page.NextOffset;
    if (!page.HasMore) break;
}
Require(changes.Select(change => change.ChangeId).SequenceEqual([createdChange, updatedChange, deletedChange]),
    "Bounded pages should contain create, update and delete in committed order.");
Require(changes.All(change => change.EntityKey == cards.GetEntityKey(cardId)), "All changes must refer to this card.");
Console.WriteLine("PASS: asynchronous create/update/read/delete and bounded change paging.");
Console.WriteLine("The partition's change history remains available for consumers; no shared Redis data was flushed.");

static void Require(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
public sealed class Card
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public bool IsComplete { get; set; }
}
