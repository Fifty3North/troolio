namespace Troolio.Projection.Redis.Models;

public sealed record RedisChangePage(IReadOnlyList<ChangeHashEntry> Entries, long NextOffset, bool HasMore);
