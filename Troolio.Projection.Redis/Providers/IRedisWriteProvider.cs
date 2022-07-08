using System.Linq.Expressions;

namespace Troolio.Projection.Redis.Providers
{
    public interface IRedisWriteProvider<TEntity>
        where TEntity : class, new()
    {
        /// <summary>
        /// Adds the Entity to Redis.
        /// Adds Entity Key to the Partition Entity Key Index.
        /// Updates the Change Set for the Partition.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="entity"></param>
        /// <param name="partitionId"></param>
        /// <returns></returns>
        Task<string> CreateEntity(Guid id, TEntity entity, Guid partitionId);

        /// <summary>
        /// Updates the Entity in Redis.
        /// Updates the Change Set for the Partition.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="entity"></param>
        /// <param name="partitionId"></param>
        /// <param name="updatedProperties"></param>
        /// <returns></returns>
        Task<string> UpdateEntity(Guid id, TEntity entity, Guid partitionId, params Expression<Func<TEntity, object>>[] updatedProperties);

        /// <summary>
        /// Removes the Entity from Redis.
        /// Removes Entity Key from the Partition Entity Key Index.
        /// Updates the Change Set for the Partition.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="partitionId"></param>
        /// <returns></returns>
        Task<string> DeleteEntity(Guid id, Guid partitionId);

        /// <summary>
        /// Adds Entity Key to the Partition Entity Key Index.
        /// Updates the Change Set for the Partition.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="partitionId"></param>
        /// <returns></returns>
        Task<string> AddEntityKeyToPartition(Guid id, Guid partitionId);

        /// <summary>
        /// Adds Entity Key to the Partition Entity Key Index if not already Added.
        /// Updates the Change Set for the Partition.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="partitionId"></param>
        /// <returns></returns>
        Task<string> UpdateEntityKeyInPartition(Guid id, Guid partitionId);

        /// <summary>
        /// Removes Entity Key from the Partition Entity Key Index.
        /// Updates the Change Set for the Partition.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="partitionId"></param>
        /// <returns></returns>
        Task<string> RemoveEntityKeyFromPartition(Guid id, Guid partitionId);
    }
}
