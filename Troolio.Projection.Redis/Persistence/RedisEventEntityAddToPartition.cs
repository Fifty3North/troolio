namespace Troolio.Projection.Redis.Persistence
{
    public class RedisEventEntityAddToPartition<TEntity> : RedisEventEntity<TEntity>
         where TEntity : class
    {
        public readonly Guid EntityId;
        public readonly TEntity Entity;
        public readonly Guid PartitionId;

        public RedisEventEntityAddToPartition(Guid entityId, TEntity entity, Guid partitionId)
        {
            EntityId = entityId;
            Entity = entity;
            PartitionId = partitionId;
        }
    }
}
