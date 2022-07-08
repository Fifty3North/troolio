using System.Linq.Expressions;
using Troolio.Projection.Redis.Models;

namespace Troolio.Projection.Redis.Providers
{
    public interface IRedisReadProvider
    {
        long GetChangeIndexLength(Guid partitionId);

        IList<ChangeHashEntry> GetChangeEntries(string lastChangeId, Guid partitionId);

        Guid GetLastChangeId(Guid partitionId);

        bool EntityIndexContains(string entityKey, string entityTypeName, Guid partitionId);

        T GetEntity<T>(string entityKey) where T : class, new();

        object GetEntity(string entityKey, Type entityType);

        bool TryParseEntityKey(string entityKey, out string entityTypeName, out Guid id);
    }

    public interface IRedisReadProvider<TEntity> : IRedisReadProvider
       where TEntity : class, new()
    {
        long GetEntityIndexLength(Guid partitionId);

        string[] GetEntityIndexKeys(Guid partitionId);

        bool EntityIndexContains(Guid id, Guid partitionId);

        string GetEntityKey(Guid id);

        bool EntityKeyExists(Guid id);

        TEntity GetEntity(Guid id);

        TProperty GetEntityProperty<TProperty>(Guid id, string property);

        TProperty GetEntityProperty<TProperty>(Guid id, Expression<Func<TEntity, TProperty>> property);
    }
}
