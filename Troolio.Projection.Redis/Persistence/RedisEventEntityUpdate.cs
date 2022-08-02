using System.Linq.Expressions;

namespace Troolio.Projection.Redis.Persistence
{
    public class RedisEventEntityUpdate<TEntity> : RedisEventEntity<TEntity>
         where TEntity : class
    {
        public readonly Guid EntityId;
        public readonly TEntity Entity;
        public readonly Guid PartitionId;
        public readonly Expression<Func<TEntity, object>>[] UpdatedProperties;

        public RedisEventEntityUpdate(Guid entityId, TEntity entity, Guid partitionId, params Expression<Func<TEntity, object>>[] updatedProperties)
        {
            EntityId = entityId;
            Entity = entity;
            PartitionId = partitionId;
            UpdatedProperties = updatedProperties;
        }
    }
}