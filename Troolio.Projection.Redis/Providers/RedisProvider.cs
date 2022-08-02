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

        private RedisProviderException ToRedisProviderException(Exception ex)
        {
            RedisProviderException rpex;
            if (ex.GetType().IsSerializable)
            {
                rpex = new RedisProviderException(ex.Message, ex);
            }
            else
            {
                rpex = new RedisProviderException(ex.Message)
                {
                    Source = ex.Source
                };
            }
            return rpex;
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
                return Guid.Parse(changeId);
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
            // get the number of changes
            long listLength = db.ListLength(key);

            // early exit if nothing in list (very unlikely)
            if (listLength <= 0)
            {
                return new RedisValue[0];
            }

            // collection to hold changes
            // we will read list from end to start (until lastChange found) but we want the changes returned earliset first
            // hence use a FIFO collection
            Stack<RedisValue> changes = new Stack<RedisValue>();

            // determine here if lastChange provided so we do not need to compare each RedisValue if not
            bool checkLastChange = !string.IsNullOrEmpty(lastChangeId);

            // initialize check to determine if lastChange found
            bool lastChangeFound = false;

            // determine initial start and end index for batch to retrieve
            if (batchSize <= 0)
            {
                batchSize = 1000;
            }

            int startIndexOffset = batchSize - 1;
            long endIndex = listLength - 1;
            long startIndex = endIndex - startIndexOffset;
            if (startIndex < 0)
            {
                startIndex = 0;
            }

            while (!lastChangeFound && endIndex >= 0)
            {
                // get values in list from long start idx to long end idx
                RedisValue[] rangeValues = db.ListRange(key, startIndex, endIndex);

                // read batch from latest first
                for (int i = rangeValues.Length - 1; i >= 0; i--)
                {
                    // read value
                    RedisValue change = rangeValues[i];

                    // check that not reached lastChange
                    if (checkLastChange && change == lastChangeId)
                    {
                        lastChangeFound = true;
                        break;
                    }

                    // add value to stack
                    changes.Push(change);
                }

                // determine new bounds (unless we have already found last change)
                if (!lastChangeFound)
                {
                    endIndex = endIndex - batchSize;
                    startIndex = endIndex - startIndexOffset;
                    if (startIndex < 0)
                    {
                        startIndex = 0;
                    }
                }
            }

            RedisValue[] values = changes.ToArray();
            return values;
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

        // TODO: lastChange should be specific to a partition
        private string _lastChange;

        private void SetLastChange(string changeId)
        {
            _lastChange = changeId;
        }

        public async Task<string> CreateEntity(Guid id, TEntity entity, Guid partitionId)
        {
            string key = GetEntityKey(id);
            string entityIndexKey = GetEntityIndexKey(partitionId);

            HashEntry[] entries = RedisConverter<TEntity>.ToHashEntries(entity);

            await DatabaseAction(async db =>
            {
                ITransaction trans = db.CreateTransaction();
                Task txTask = trans.HashSetAsync(key, entries);
                Task<bool> addTxTask = trans.SetAddAsync(entityIndexKey, key);
                string lastChange = UpdateChangeSet(trans, key, partitionId);
                SetLastChange(lastChange);
                await trans.ExecuteAsync();
                await txTask;
                await addTxTask;
            });

            return _lastChange;
        }

        public async Task<string> UpdateEntity(Guid id, TEntity entity, Guid partitionId, params Expression<Func<TEntity, object>>[] updatedProperties)
        {
            string key = GetEntityKey(id);
            HashEntry[] entityHash = RedisConverter<TEntity>.ToHashEntries(entity, updatedProperties);

            await DatabaseAction(async db =>
            {
                ITransaction trans = db.CreateTransaction();
                Task txTask = trans.HashSetAsync(key, entityHash);
                string lastchange = UpdateChangeSet(trans, key, partitionId);
                SetLastChange(lastchange);
                await trans.ExecuteAsync();
                await txTask;
            });

            return _lastChange;
        }

        public async Task<string> DeleteEntity(Guid id, Guid partitionId)
        {
            string key = GetEntityKey(id);
            string entityIndexKey = GetEntityIndexKey(partitionId);

            await DatabaseAction(async db =>
            {
                ITransaction trans = db.CreateTransaction();
                Task<bool> txTask = trans.KeyDeleteAsync(key);
                Task<bool> deleteTxTask = trans.SetRemoveAsync(entityIndexKey, key);
                string lastChange = UpdateChangeSet(trans, key, partitionId);
                SetLastChange(lastChange);
                await trans.ExecuteAsync();
                await txTask;
                await deleteTxTask;
            });

            return _lastChange;
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

            await DatabaseAction(async db =>
            {
                ITransaction trans = db.CreateTransaction();
                Task<bool> addTxTask = trans.SetAddAsync(entityIndexKey, key);
                string lastChange = UpdateChangeSet(trans, key, partitionId);
                SetLastChange(lastChange);
                await trans.ExecuteAsync();
                await addTxTask;
            });

            return _lastChange;
        }

        public async Task<string> RemoveEntityKeyFromPartition(Guid id, Guid partitionId)
        {
            string key = GetEntityKey(id);
            string entityIndexKey = GetEntityIndexKey(partitionId);

            await DatabaseAction(async db =>
            {
                ITransaction trans = db.CreateTransaction();
                Task<bool> deleteTxTask = trans.SetRemoveAsync(entityIndexKey, key);
                string lastChange = UpdateChangeSet(trans, key, partitionId);
                SetLastChange(lastChange);
                await trans.ExecuteAsync();
                await deleteTxTask;
            });

            return _lastChange;
        }

        private string UpdateChangeSet(ITransaction trans, string entityKey, Guid partitionId)
        {
            string changeIndexKey = GetChangeIndexKey(partitionId);
            string changeHashKey = GetChangeHashKey(partitionId);
            string changeId = Guid.NewGuid().ToString();

            trans.ListRightPushAsync(changeIndexKey, changeId);
            trans.HashSetAsync(changeHashKey, changeId, entityKey);

            return changeId;
        }
    }
}
