namespace Troolio.Projection.Redis.Persistence
{
    public class RedisEventEntityDelete<TEntity> : RedisEventEntity<TEntity>
        where TEntity : class
    {
        public readonly Guid EntityId;
        public readonly Guid PartitionId;

        public RedisEventEntityDelete(Guid entityId, Guid partitionId)
        {
            EntityId = entityId;
            PartitionId = partitionId;
        }
    }
}