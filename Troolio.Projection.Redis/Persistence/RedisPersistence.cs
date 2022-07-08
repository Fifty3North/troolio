
using Microsoft.Extensions.Logging;
using Omu.ValueInjecter;
using Orleankka;
using System.Linq.Expressions;
using Troolio.Core;
using Troolio.Projection.Redis.Persistence;
using Troolio.Projection.Redis.Providers;

namespace Troolio.Projection.Persistence
{
    internal class RedisPersistence<TEntity> : Troolio.Core.DispatchActor
        where TEntity : class, new()
    {
        protected readonly IRedisWriteProvider<TEntity> _redisWriteProvider;
        protected readonly IRedisReadProvider<TEntity> _redisReadProvider;
        protected ILogger<RedisPersistence<TEntity>> _logger;

        public RedisPersistence(string id, IActorRuntime runtime,
            IRedisWriteProvider<TEntity> redisWriteProvider, IRedisReadProvider<TEntity> redisReadProvider,
            ILogger<RedisPersistence<TEntity>> logger)

            : base(id, runtime)
        {
            _redisWriteProvider = redisWriteProvider;
            _redisReadProvider = redisReadProvider;
            _logger = logger;
        }

        public virtual async Task Persist<TEvent>(EventEnvelope<TEvent> e) where TEvent : Event
        {
            List<RedisEventEntity<TEntity>> redisEventEntities = await Mapper.Map<Task<List<RedisEventEntity<TEntity>>>>(e);

            foreach (RedisEventEntity<TEntity> redisEventEntity in redisEventEntities)
            {
                if (redisEventEntity is RedisEventEntityCreate<TEntity> create)
                {
                    await Create(create.EntityId, create.Entity, create.PartitionId);
                }
                else if (redisEventEntity is RedisEventEntityAddToPartition<TEntity> add)
                {
                    await AddToPartition(add.EntityId, add.Entity, add.PartitionId);
                }
                else if (redisEventEntity is RedisEventEntityUpdate<TEntity> update)
                {
                    await Update(update);
                }
                else if (redisEventEntity is RedisEventEntityUpdateInPartition<TEntity> updatePartition)
                {
                    await UpdateInPartition(updatePartition.EntityId, updatePartition.PartitionId);
                }
                else if (redisEventEntity is RedisEventEntityDelete<TEntity> delete)
                {
                    await Delete(delete.EntityId, delete.PartitionId);
                }
                else if (redisEventEntity is RedisEventEntityRemoveFromPartition<TEntity> remove)
                {
                    await RemoveFromPartition(remove.EntityId, remove.PartitionId);
                }
            }
        }

        public virtual async Task Create<TEvent>(EventEnvelope<TEvent> e) where TEvent : Event
        {
            RedisEventEntityCreate<TEntity> create = await Mapper.Map<Task<RedisEventEntityCreate<TEntity>>>(e);

            await Create(create.EntityId, create.Entity, create.PartitionId);
        }

        private async Task Create(Guid entityId, TEntity entity, Guid partitionId)
        {
            string changeId = await _redisWriteProvider.CreateEntity(entityId, entity, partitionId);

            if (entityId == Guid.Empty)
            {
                _logger.Log(LogLevel.Error, $"Redis Create has empty create.Guid for type: {entity.GetType()}, PartitionId: {partitionId}.");
            }

        }

        public virtual async Task Update<TEvent>(EventEnvelope<TEvent> e) where TEvent : Event
        {
            RedisEventEntityUpdate<TEntity> update = await Mapper.Map<Task<RedisEventEntityUpdate<TEntity>>>(e);

            await Update(update);
        }

        private async Task Update(RedisEventEntityUpdate<TEntity> update)
        {
            // If nothing to update then nothing to do.
            if (update.Entity == null || update.UpdatedProperties == null || update.UpdatedProperties.Length == 0)
            {
                return;
            }

            // Set Id on IStatefulItem
            // This is to ensure that if a Create failed then a subsequent Update would guarantee that the Id is correctly set
            TEntity entity = update.Entity;
            //entity.Id = update.EntityId;

            // Add update for IStatefulItem.Id
            Expression<Func<TEntity, object>>[] updatedProperties = update.UpdatedProperties;
            Array.Resize(ref updatedProperties, updatedProperties.Length + 1);
            //updatedProperties[updatedProperties.Length - 1] = (o) => o.Id;

            // Update
            await Update(update.EntityId, update.Entity, update.PartitionId, updatedProperties);
        }

        private async Task Update(Guid entityId, TEntity entity, Guid partitionId, params Expression<Func<TEntity, object>>[] updatedProperties)
        {
            string changeId = await _redisWriteProvider.UpdateEntity(entityId, entity, partitionId, updatedProperties);

            TEntity updatedEntity = _redisReadProvider.GetEntity(entityId);

            if (entityId == Guid.Empty)
            {
                _logger.Log(LogLevel.Error, $"Redis Update has empty update.Guid for type: {entity.GetType()}, PartitionId: {partitionId}.");
            }
        }

        public virtual async Task Delete<TEvent>(EventEnvelope<TEvent> e) where TEvent : Event
        {
            RedisEventEntityDelete<TEntity> delete = await Mapper.Map<Task<RedisEventEntityDelete<TEntity>>>(e);

            await Delete(delete.EntityId, delete.PartitionId);
        }

        protected async Task Delete(Guid entityId, Guid partitionId)
        {
            string changeId = await _redisWriteProvider.DeleteEntity(entityId, partitionId);
        }

        protected async Task AddToPartition(Guid entityId, TEntity entity, Guid partitionId)
        {
            if (entity == null)
            {
                entity = GetEntity(entityId);
                if (entity == null)
                {
                    _logger.Log(LogLevel.Error, $"Redis AddToPartition entity not found for type: {typeof(TEntity).Name}, Entity Id: {entityId}, PartitionId: {partitionId}.");
                    return;
                }
            }

            string changeId = await _redisWriteProvider.AddEntityKeyToPartition(entityId, partitionId);

        }

        protected async Task UpdateInPartition(Guid entityId, Guid partitionId)
        {
            TEntity entity = GetEntity(entityId);
            if (entity == null)
            {
                _logger.Log(LogLevel.Error, $"Redis UpdatePartition entity not found for type: {typeof(TEntity).Name}, Entity Id: {entityId}, PartitionId: {partitionId}.");
                return;
            }

            string changeId = await _redisWriteProvider.UpdateEntityKeyInPartition(entityId, partitionId);

        }

        protected async Task RemoveFromPartition(Guid entityId, Guid partitionId)
        {
            string changeId = await _redisWriteProvider.RemoveEntityKeyFromPartition(entityId, partitionId);

        }

        public bool EntityKeyExists(Guid entityId)
        {
            return _redisReadProvider.EntityKeyExists(entityId);
        }

        public TEntity GetEntity(Guid entityId)
        {
            return _redisReadProvider.GetEntity(entityId);
        }

        public TProperty GetEntityProperty<TProperty>(Guid entityId, Expression<Func<TEntity, TProperty>> property)
        {
            return _redisReadProvider.GetEntityProperty(entityId, property);
        }
    }
}
