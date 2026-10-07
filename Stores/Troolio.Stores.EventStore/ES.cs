using KurrentDB.Client;

namespace Troolio.Stores.EventStore;

/// <summary>Creates a gRPC client. Register the client as a singleton owned by the host.</summary>
public static class ES
{
    public static KurrentDBClient CreateClient(string connectionString) =>
        new(KurrentDBClientSettings.Create(connectionString));
}
