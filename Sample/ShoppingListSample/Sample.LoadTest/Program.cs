using System.Net.Http.Headers;
using System.Net.Http.Json;

// Bounded sequential smoke load. Run only against a disposable local sample database.
var count = args.Length > 0 ? int.Parse(args[0]) : 10;
if (count is < 1 or > 1000) throw new ArgumentOutOfRangeException(nameof(count), "Use between 1 and 1000 lists.");
using var client = new HttpClient { BaseAddress = new Uri(args.Length > 1 ? args[1] : "http://localhost:8081"), Timeout = TimeSpan.FromSeconds(30) };
client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "demo-alice");
client.DefaultRequestHeaders.Add("userId", "11111111-1111-1111-1111-111111111111");
client.DefaultRequestHeaders.Add("deviceId", "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
for (var index = 0; index < count; index++)
{
    var listId = Guid.NewGuid();
    using var created = await client.PostAsJsonAsync($"/ShoppingList/{listId}/CreateNewList", new { title = $"Load sample {index}" });
    created.EnsureSuccessStatusCode();
    using var added = await client.PostAsJsonAsync($"/ShoppingList/{listId}/AddItemToList", new { itemId = Guid.NewGuid(), description = "Milk", quantity = 1 });
    added.EnsureSuccessStatusCode();
}
Console.WriteLine($"Created {count} lists and items. Read models may finish asynchronously.");
