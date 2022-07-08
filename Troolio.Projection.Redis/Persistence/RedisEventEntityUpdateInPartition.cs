namespace Troolio.Projection.Redis.Persistence
{
    public class RedisEventEntityUpdateInPartition<TEntity> : RedisEventEntity<TEntity>
        where TEntity : class
    {
        public readonly Guid EntityId;
        public readonly Guid PartitionId;

        public RedisEventEntityUpdateInPartition(Guid entityId, Guid partitionId)
        {
            EntityId = entityId;
            PartitionId = partitionId;
        }
    }
}
