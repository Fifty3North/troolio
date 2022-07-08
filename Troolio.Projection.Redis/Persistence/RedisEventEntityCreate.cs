namespace Troolio.Projection.Redis.Persistence
{
    public class RedisEventEntityCreate<TEntity> : RedisEventEntity<TEntity>
        where TEntity : class
    {
        public readonly Guid EntityId;
        public readonly TEntity Entity;
        public readonly Guid PartitionId;

        public RedisEventEntityCreate(Guid entityId, TEntity entity, Guid partitionId)
        {
            EntityId = entityId;
            Entity = entity;
            PartitionId = partitionId;
        }
    }
}