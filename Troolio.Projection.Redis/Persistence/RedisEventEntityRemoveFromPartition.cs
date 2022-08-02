namespace Troolio.Projection.Redis.Persistence
{
    public class RedisEventEntityRemoveFromPartition<TEntity> : RedisEventEntity<TEntity>
       where TEntity : class
    {
        public readonly Guid EntityId;
        public readonly Guid PartitionId;

        public RedisEventEntityRemoveFromPartition(Guid entityId, Guid partitionId)
        {
            EntityId = entityId;
            PartitionId = partitionId;
        }
    }
}
