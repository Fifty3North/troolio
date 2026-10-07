using StackExchange.Redis;
using System.Linq.Expressions;
using Troolio.Projection.Redis.Exceptions;
using Troolio.Projection.Redis.Models;

namespace Troolio.Projection.Redis.Providers
{
    public class RedisProvider : IRedisReadProvider
    {
        private readonly IRedisConnectionProvider _redis;

        protected readonly string CHANGE_INDEX = "ChangeIndex"; // SET of Change Ids
        protected readonly string CHANGE_HASH = "ChangeHash"; // HASH of Change Ids -> Indexes (Entity Keys)
        protected readonly string KEY_INDEX = "KeyIndex"; // SET of indexes for an Entity

        public RedisProvider(IRedisConnectionProvider redis)
        {
            _redis = redis;
        }

        private IDatabase GetDatabase()
        {
            return _redis.Connection.GetDatabase();
        }

        protected string GetChangeIndexKey(Guid partitionId)
        {
            return CHANGE_INDEX + ":" + partitionId.ToString();
        }

        protected string GetChangeHashKey(Guid partitionId)
        {
            return CHANGE_HASH + ":" + partitionId.ToString();
        }

        protected string GetEntityIndexKey(string entityTypeName, Guid partitionId)
        {
            return entityTypeName + ":" + KEY_INDEX + ":" + partitionId.ToString();
        }

        protected T DatabaseAction<T>(Func<IDatabase, T> dbFunc)
        {
            try
            {
                IDatabase db = GetDatabase();
                T result = dbFunc(db);
                return result;
            }
            catch (Exception ex)
            {
                throw ToRedisProviderException(ex);
            }
        }

        protected void DatabaseAction(Action<IDatabase> dbAct)
        {
            try
            {
                IDatabase db = GetDatabase();
                dbAct(db);
            }
            catch (Exception ex)
            {
                throw ToRedisProviderException(ex);
            }
        }

        protected async Task DatabaseActionAsync(Func<IDatabase, Task> action)
        {
            try { await action(GetDatabase()); }
            catch (Exception exception) { throw ToRedisProviderException(exception); }
        }

        private static RedisProviderException ToRedisProviderException(Exception exception) =>
            new(exception.Message, exception);

        /// <summary>Returns at most 1000 committed changes at an absolute list offset.</summary>
        public async Task<RedisChangePage> GetChangePageAsync(Guid partitionId, long offset, int maxCount = 100)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(offset);
            if (maxCount is < 1 or > 1000) throw new ArgumentOutOfRangeException(nameof(maxCount));
            List<ChangeHashEntry> entries = [];
            await DatabaseActionAsync(async db =>
            {
                RedisValue[] ids = await db.ListRangeAsync(GetChangeIndexKey(partitionId), offset, checked(offset + maxCount - 1)).ConfigureAwait(false);
                foreach (RedisValue id in ids)
                {
                    RedisValue value = await db.HashGetAsync(GetChangeHashKey(partitionId), id).ConfigureAwait(false);
                    if (!value.HasValue) throw new InvalidDataException("Change-feed history is incomplete; reload the read model.");
                    entries.Add(new ChangeHashEntry(id, value));
                }
            }).ConfigureAwait(false);
            return new RedisChangePage(entries, checked(offset + entries.Count), entries.Count == maxCount);
        }

        public RedisChangePage GetChangePage(Guid partitionId, long offset, int maxCount = 100)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(offset);
            if (maxCount is < 1 or > 1000) throw new ArgumentOutOfRangeException(nameof(maxCount));
            RedisValue[] ids = DatabaseAction(db => db.ListRange(GetChangeIndexKey(partitionId), offset, checked(offset + maxCount - 1)));
            List<ChangeHashEntry> entries = new(ids.Length);
            foreach (RedisValue id in ids)
            {
                RedisValue value = HashGet(GetChangeHashKey(partitionId), id);
                if (!value.HasValue) throw new InvalidDataException("Change-feed cursor history is incomplete; reload the read model.");
                entries.Add(new ChangeHashEntry(id, value));
            }
            return new RedisChangePage(entries, checked(offset + ids.Length), ids.Length == maxCount);
        }

        /// <summary>
        /// Issues Redis Command: EXISTS {key}
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        protected bool KeyExists(string key)
        {
            return DatabaseAction(db => db.KeyExists(key));
        }

        /// <summary>
        /// Issues Redis Command: LLEN {key}
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        protected long ListLength(string key)
        {
            return DatabaseAction(db => db.ListLength(key));
        }

        /// <summary>
        /// Issues Redis Command: SCARD {key}
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        protected long SetLength(string key)
        {
            return DatabaseAction(db => db.SetLength(key));
        }

        /// <summary>
        /// Issues Redis Command: SMEMBERS {key}
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        protected RedisValue[] SetMembers(string key)
        {
            return DatabaseAction(db => db.SetMembers(key));
        }

        /// <summary>
        /// Issues Redis Command: SISMEMBER {key} {field}
        /// </summary>
        /// <param name="key"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        protected bool SetContains(string key, string field)
        {
            return DatabaseAction(db => db.SetContains(key, field));
        }

        /// <summary>
        /// Issues Redis Command: HGET {key} {field}
        /// </summary>
        /// <param name="key"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        protected RedisValue HashGet(string key, string field)
        {
            return DatabaseAction(db => db.HashGet(key, field));
        }

        /// <summary>
        /// Issues Redis Command: HGETALL {key}
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        protected HashEntry[] HashGetAll(string key)
        {
            return DatabaseAction(db => db.HashGetAll(key));
        }

        public long GetChangeIndexLength(Guid partitionId)
        {
            return ListLength(GetChangeIndexKey(partitionId));
        }

        public Guid GetLastChangeId(Guid partitionId)
        {
            RedisValue changeId = DatabaseAction(db =>
            {
                return db.ListGetByIndex(GetChangeIndexKey(partitionId), -1);
            });

            if (changeId.IsNullOrEmpty)
            {
                return Guid.Empty;
            }
            else
            {
                return Guid.Parse(changeId.ToString());
            }
        }

        public IList<ChangeHashEntry> GetChangeEntries(string lastChangeId, Guid partitionId)
        {
            IList<ChangeHashEntry> changes = new List<ChangeHashEntry>();

            string changeIndexKey = GetChangeIndexKey(partitionId);

            RedisValue[] changesToGet = DatabaseAction(db =>
            {
                return DiffChangesFromList(db, changeIndexKey, lastChangeId, 1000);
            });

            string changeHashKey = GetChangeHashKey(partitionId);

            foreach (RedisValue key in changesToGet)
            {
                RedisValue value = HashGet(changeHashKey, key);
                if (value.HasValue)
                {
                    ChangeHashEntry change = new ChangeHashEntry(key, value);
                    changes.Add(change);
                }
            }

            return changes;
        }

        private RedisValue[] DiffChangesFromList(IDatabase db, string key, string lastChangeId, int batchSize)
        {
            long length = db.ListLength(key);
            RedisValue[] values = db.ListRange(key, Math.Max(0, length - batchSize), -1);
            if (string.IsNullOrEmpty(lastChangeId)) return values;
            int position = Array.FindIndex(values, value => value == lastChangeId);
            if (position < 0) throw new InvalidOperationException("Cursor is outside the bounded change window; reload the read model or use GetChangePage.");
            return values.Skip(position + 1).ToArray();
        }

        public bool EntityIndexContains(string entityKey, string entityTypeName, Guid partitionId)
        {
            string entityIndexKey = GetEntityIndexKey(entityTypeName, partitionId);
            return SetContains(entityIndexKey, entityKey);
        }

        public T GetEntity<T>(string entityKey) where T : class, new()
        {
            T entity = null;
            HashEntry[] entries = HashGetAll(entityKey);
            // If the key does not exist in Redis then an empty HashEntry array is returned
            if (entries?.Length > 0)
            {
                entity = RedisConverter<T>.FromHashEntries(entries);
            }
            return entity;
        }

        public object GetEntity(string entityKey, Type entityType)
        {
            object entity = null;
            HashEntry[] entries = HashGetAll(entityKey);
            // If the key does not exist in Redis then an empty HashEntry array is returned
            if (entries?.Length > 0)
            {
                entity = RedisConverter.FromHashEntries(entries, entityType);
            }
            return entity;
        }

        public bool TryParseEntityKey(string entityKey, out string entityTypeName, out Guid id)
        {
            entityTypeName = null;
            id = default(Guid);

            if (!string.IsNullOrEmpty(entityKey))
            {
                string[] parts = entityKey.Split(':');
                if (parts.Length == 2)
                {
                    entityTypeName = parts[0];
                    if (Guid.TryParse(parts[1], out id))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }

    public class RedisProvider<TEntity> : RedisProvider, IRedisReadProvider<TEntity>, IRedisWriteProvider<TEntity>
        where TEntity : class, new()
    {
        private readonly string ENTITY_TYPE_NAME = typeof(TEntity).Name;

        private string GetEntityIndexKey(Guid partitionId)
        {
            return GetEntityIndexKey(ENTITY_TYPE_NAME, partitionId);
        }

        public RedisProvider(IRedisConnectionProvider redis) : base(redis)
        {
        }

        public long GetEntityIndexLength(Guid partitionId)
        {
            string entityIndexKey = GetEntityIndexKey(partitionId);
            return SetLength(entityIndexKey);
        }

        public string[] GetEntityIndexKeys(Guid partitionId)
        {
            string entityIndexKey = GetEntityIndexKey(partitionId);
            return SetMembers(entityIndexKey).Select(x => x.ToString()).ToArray();
        }

        public bool EntityIndexContains(Guid id, Guid partitionId)
        {
            string key = GetEntityKey(id);
            string entityIndexKey = GetEntityIndexKey(partitionId);
            return SetContains(entityIndexKey, key);
        }

        public string GetEntityKey(Guid id)
        {
            return $"{ENTITY_TYPE_NAME}:{id}";
        }

        public bool EntityKeyExists(Guid id)
        {
            string key = GetEntityKey(id);
            return KeyExists(key);
        }

        public async Task<TEntity?> GetEntityAsync(Guid id)
        {
            TEntity? entity = null;
            await DatabaseActionAsync(async db =>
            {
                HashEntry[] entries = await db.HashGetAllAsync(GetEntityKey(id)).ConfigureAwait(false);
                if (entries.Length > 0) entity = RedisConverter<TEntity>.FromHashEntries(entries);
            }).ConfigureAwait(false);
            return entity;
        }

        public TEntity GetEntity(Guid id)
        {
            string key = GetEntityKey(id);
            return GetEntity<TEntity>(key);
        }

        public TProperty GetEntityProperty<TProperty>(Guid id, string property)
        {
            string key = GetEntityKey(id);
            RedisValue value = HashGet(key, property);
            return RedisConverter.FromRedisValue<TProperty>(value);
        }

        public TProperty GetEntityProperty<TProperty>(Guid id, Expression<Func<TEntity, TProperty>> property)
        {
            string propertyName = ReflectionUtils.GetPropertyName(property);
            return GetEntityProperty<TProperty>(id, propertyName);
        }

        public async Task<string> CreateEntity(Guid id, TEntity entity, Guid partitionId)
        {
            string key = GetEntityKey(id);
            string entityIndexKey = GetEntityIndexKey(partitionId);

            HashEntry[] entries = RedisConverter<TEntity>.ToHashEntries(entity);

            string? operationChange = null;
            await DatabaseActionAsync(async db =>
            {
                ITransaction trans = db.CreateTransaction();
                Task txTask = trans.HashSetAsync(key, entries);
                Task<bool> addTxTask = trans.SetAddAsync(entityIndexKey, key);
                var change = UpdateChangeSet(trans, key, partitionId);
                string lastChange = change.Id;
                operationChange = lastChange;
                if (!await trans.ExecuteAsync()) throw new InvalidOperationException("Redis transaction was not committed.");
                await Task.WhenAll(change.Tasks);
                await txTask;
                await addTxTask;
            });

            return operationChange!;
        }

        public async Task<string> UpdateEntity(Guid id, TEntity entity, Guid partitionId, params Expression<Func<TEntity, object>>[] updatedProperties)
        {
            string key = GetEntityKey(id);
            HashEntry[] entityHash = RedisConverter<TEntity>.ToHashEntries(entity, updatedProperties);

            string? operationChange = null;
            await DatabaseActionAsync(async db =>
            {
                ITransaction trans = db.CreateTransaction();
                Task txTask = trans.HashSetAsync(key, entityHash);
                var change = UpdateChangeSet(trans, key, partitionId);
                string lastchange = change.Id;
                operationChange = lastchange;
                if (!await trans.ExecuteAsync()) throw new InvalidOperationException("Redis transaction was not committed.");
                await Task.WhenAll(change.Tasks);
                await txTask;
            });

            return operationChange!;
        }

        public async Task<string> DeleteEntity(Guid id, Guid partitionId)
        {
            string key = GetEntityKey(id);
            string entityIndexKey = GetEntityIndexKey(partitionId);

            string? operationChange = null;
            await DatabaseActionAsync(async db =>
            {
                ITransaction trans = db.CreateTransaction();
                Task<bool> txTask = trans.KeyDeleteAsync(key);
                Task<bool> deleteTxTask = trans.SetRemoveAsync(entityIndexKey, key);
                var change = UpdateChangeSet(trans, key, partitionId);
                string lastChange = change.Id;
                operationChange = lastChange;
                if (!await trans.ExecuteAsync()) throw new InvalidOperationException("Redis transaction was not committed.");
                await Task.WhenAll(change.Tasks);
                await txTask;
                await deleteTxTask;
            });

            return operationChange!;
        }

        public async Task<string> AddEntityKeyToPartition(Guid id, Guid partitionId)
        {
            return await AddOrUpdateEntityKeyInPartition(id, partitionId);
        }

        public async Task<string> UpdateEntityKeyInPartition(Guid id, Guid partitionId)
        {
            return await AddOrUpdateEntityKeyInPartition(id, partitionId);
        }

        private async Task<string> AddOrUpdateEntityKeyInPartition(Guid id, Guid partitionId)
        {
            // Note: For an update, we should not need to add entity key to the entity index, 
            // but cannot guarantee that Add to Partition would have previously been called.
            // If the entity key already exists in the entity index then the add will be ignored.

            string key = GetEntityKey(id);
            string entityIndexKey = GetEntityIndexKey(partitionId);

            string? operationChange = null;
            await DatabaseActionAsync(async db =>
            {
                ITransaction trans = db.CreateTransaction();
                Task<bool> addTxTask = trans.SetAddAsync(entityIndexKey, key);
                var change = UpdateChangeSet(trans, key, partitionId);
                string lastChange = change.Id;
                operationChange = lastChange;
                if (!await trans.ExecuteAsync()) throw new InvalidOperationException("Redis transaction was not committed.");
                await Task.WhenAll(change.Tasks);
                await addTxTask;
            });

            return operationChange!;
        }

        public async Task<string> RemoveEntityKeyFromPartition(Guid id, Guid partitionId)
        {
            string key = GetEntityKey(id);
            string entityIndexKey = GetEntityIndexKey(partitionId);

            string? operationChange = null;
            await DatabaseActionAsync(async db =>
            {
                ITransaction trans = db.CreateTransaction();
                Task<bool> deleteTxTask = trans.SetRemoveAsync(entityIndexKey, key);
                var change = UpdateChangeSet(trans, key, partitionId);
                string lastChange = change.Id;
                operationChange = lastChange;
                if (!await trans.ExecuteAsync()) throw new InvalidOperationException("Redis transaction was not committed.");
                await Task.WhenAll(change.Tasks);
                await deleteTxTask;
            });

            return operationChange!;
        }

        private (string Id, Task[] Tasks) UpdateChangeSet(ITransaction trans, string entityKey, Guid partitionId)
        {
            string changeIndexKey = GetChangeIndexKey(partitionId);
            string changeHashKey = GetChangeHashKey(partitionId);
            string changeId = Guid.NewGuid().ToString();

            Task append = trans.ListRightPushAsync(changeIndexKey, changeId);
            Task hash = trans.HashSetAsync(changeHashKey, changeId, entityKey);
            return (changeId, new[] { append, hash });
        }
    }
}
