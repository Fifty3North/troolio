using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Omu.ValueInjecter;
using Orleankka;
using Orleans;
using Orleans.Streams;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Troolio.Core.Projection.Exceptions;

namespace Troolio.Core.Projection
{
    public abstract class EntityFrameworkBatched<TEntity, TDbContext> : DispatchActor
        where TEntity : class
        where TDbContext : DbContext
    {
        private ILogger<EntityFrameworkBatched<TEntity, TDbContext>>? _logger;

        private static readonly string _entityName = typeof(TEntity).Name;

        private static readonly TimeSpan[] _defaultRetryDurations = new[]
        {
            TimeSpan.FromMilliseconds(10),
            TimeSpan.FromMilliseconds(50),
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(500),
            TimeSpan.FromMilliseconds(1000),
            TimeSpan.FromMilliseconds(2000),
            TimeSpan.FromMilliseconds(5000),
            TimeSpan.FromMilliseconds(10000)
        };

        /// <summary>
        /// The Id for the timer to instigate a flush
        /// </summary>
        private readonly string _flushTimerId = "flush";

        /// <summary>
        /// Interval in which flush of queue should be instigated.
        /// </summary>
        private readonly TimeSpan _flushPeriod = TimeSpan.FromMilliseconds(250);

        /// <summary>
        /// Queue of Jobs to process.
        /// </summary>
        private readonly ConcurrentQueue<EfJob> _jobs = new ConcurrentQueue<EfJob>();

        /// <summary>
        /// Lock to ensure that processing to flush batch job queue is only being done by one process at once.
        /// </summary>
        private readonly SemaphoreSlim _queueProcessLock = new SemaphoreSlim(1, 1);

        /// <summary>
        /// The number of active processes flushing the batch job queue (should only ever be zero or one)
        /// </summary>
        private int _activeFlushCount = 0;

        public virtual async Task On(Activate _)
        {
            _logger = this.ServiceProvider.GetRequiredService<ILogger<EntityFrameworkBatched<TEntity, TDbContext>>>();

            IStreamProvider streamProvider = GetStreamProvider(Constants.ProjectionStreamPrefix);

            //var primaryKeyString = this.GetPrimaryKeyString();
            //var positionOfFirstDash = primaryKeyString.IndexOf('-');
            //var stringGuid = primaryKeyString.Substring(positionOfFirstDash + 1, primaryKeyString.Length - positionOfFirstDash - 1);
            //var guid = Guid.Parse(stringGuid);

            Guid guid = this.GetPrimaryKey(out string extension);
            IAsyncStream<IEventEnvelope> stream = streamProvider.GetStream<IEventEnvelope>(guid, extension);

            await stream.SubscribeAsync((envelope, token) => Receive(envelope));

            //RegisterFlushTimer();

            Timers.Register(_flushTimerId, _flushPeriod, _flushPeriod, () => Flush(false));
        }

        //private void RegisterFlushTimer()
        //{
        //    if (!Timers.IsRegistered(_flushTimerId))
        //    {
        //        Timers.Register(_flushTimerId, _flushPeriod, _flushPeriod, () => Self.Tell(new Commands.Flush(false)));
        //    }
        //}

        //private void UnregisterFlushTimer()
        //{
        //    if (Timers.IsRegistered(_flushTimerId))
        //    {
        //        Timers.Unregister(_flushTimerId);
        //    }
        //}

        /// <summary>
        /// Message handler to Process Job queue now.
        /// This is only ever intended to be called from tests.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task On(Commands.ProcessNow _)
        {
            await ProcessNow();
        }

        public async Task On(Commands.Flush command)
        {
            await Flush(command.Force);      
        }

        /// <summary>
        /// Process the Job queue now.
        /// This is only ever intended to be called from tests.
        /// </summary>
        /// <returns></returns>
        public async Task ProcessNow()
        {
            await Flush(true);
        }

        private async Task Flush(bool forceFlush = false)
        {
            if (!_jobs.IsEmpty)
            {
                if (_activeFlushCount > 0 && !forceFlush)
                {
                    // flush already in process and we don't want to force another one to wait for a go
                    return;
                }

                // lock here so we can clear the whole queue before another call to flush gets a go.
                await _queueProcessLock.WaitAsync();

                Interlocked.Increment(ref _activeFlushCount);

                while (_jobs.TryPeek(out EfJob? efJob))
                {
                    try
                    {
                        await efJob.Action.Invoke();
                        _jobs.TryDequeue(out _);
                    }
                    catch (Exception ex)
                    {
                        if (efJob.TryCount < _defaultRetryDurations.Length)
                        {
                            await Task.Delay(_defaultRetryDurations[efJob.TryCount]);
                            efJob.IncrementTryCount();
                        }
                        else
                        {
                            _jobs.TryDequeue(out _);
                            _logger?.LogError($"Retry count exceeded abandoning job for : {_entityName}", ex);
                        }
                    }
                }

                //if (_jobs.IsEmpty)
                //{
                //    UnregisterFlushTimer();
                //}

                // release lock
                _queueProcessLock.Release();

                Interlocked.Decrement(ref _activeFlushCount);
            }
        }

        private Task AddJobToQueue(Func<Task> job)
        {
            _jobs.Enqueue(new EfJob(job));

            //RegisterFlushTimer();
            _ = Flush(false);

            return Task.CompletedTask;
        }

        public async Task Create<TEvent>(EventEnvelope<TEvent> e)
           where TEvent : Event
        {
            await AddJobToQueue(async () =>
            {
                string eventName = typeof(TEvent).Name;

                _logger?.LogTrace($"Starting Create for Event: {eventName} : {e.Id} from {this.GetType().Name}");

                // Map
                EventEntityCreate<TEntity> create;

                try
                {
                    create = await Mapper.Map<Task<EventEntityCreate<TEntity>>>(e);
                }
                catch (Exception ex)
                {
                    _logger?.LogError($"Error Creating Entity for Type: {_entityName} - Mapping failed. (Event: {eventName}, Source Id: {e.Id})  from {this.GetType().Name}", ex);
                    throw;
                }
                
                using (IServiceScope scope = this.ServiceProvider.CreateScope())
                using (DbContext dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>())
                {
                    TEntity entity = create.Entity;
                    Guid entityId = create.EntityId;

                    try
                    {
                        _logger?.LogTrace($"About to InsertEntity for {eventName} : {e.Id} from {this.GetType().Name}");

                        DbSet<TEntity> entitySet = dbContext.Set<TEntity>();

                        try
                        {
                            entitySet.Add(entity);
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError($"Error adding Entity to set ({_entityName}) ({ex.GetType().Name}) (Non DbEntityValidationException): {ex.Message}");
                            throw;
                        }

                        Validate(dbContext);

                        int insertedItemCount = await dbContext.SaveChangesAsync();

                        if (insertedItemCount <= 0)
                        {
                            _logger?.LogError($"Failed to Insert Entity for Type: {_entityName} - Inserted Item Count = 0. (Event: {eventName}, Source Id: {e.Id}, Entity Id: {entityId})");
                            // TODO: We should throw exception here or just return
                        }

                        _logger?.LogTrace($"Finished InsertEntity for {eventName} : {e.Id}  from {this.GetType().Name}");
                    }
                    catch (ValidationException ex)
                    {
                        LogValidationException(ex);
                        throw;
                    }
                    catch (DbUpdateException updex)
                    {
                        LogDbUpdateException(updex);
                        throw;
                    }
                    catch (Exception e)
                    {
                        LogException(e);
                        throw;
                    }
                }

                _logger?.LogTrace($"Finished Create for Event: {eventName} : {e.Id}  from {this.GetType().Name}");
            });
        }

        public async Task Update<TEvent>(EventEnvelope<TEvent> e)
            where TEvent : Event
        {
            await AddJobToQueue(async () =>
            {
                string eventName = typeof(TEvent).Name;

                // Map
                EventEntityUpdate<TEntity> update;

                try
                {
                    update = await Mapper.Map<Task<EventEntityUpdate<TEntity>>>(e);
                }
                catch (Exception ex)
                {
                    _logger?.LogError($"Error Updating Entity for Type: {_entityName} - Mapping failed. (Event: {eventName}, Source Id: {e.Id})", ex);
                    // TODO: should we throw this or just swallow it and return here?
                    throw;
                }

                if (update.PropertyUpdateActions == null || update.PropertyUpdateActions.Length == 0)
                {
                    // Nothing to update
                    return;
                }

                using (IServiceScope scope = this.ServiceProvider.CreateScope())
                using (DbContext dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>())
                {
                    Guid entityId = update.EntityId;

                    try
                    {
                        _logger?.LogTrace($"Update Entity for Type: {_entityName}. (Event: {eventName}, Source Id: {e.Id}, Entity Id: {entityId})");

                        string errorMessage = $"Error Updating Entity for Type: {_entityName} - Existing entity does not exist. (Event: {eventName}, Source Id: {e.Id}, Entity Id: {entityId})";

                        // This will throw exception if entity not found
                        TEntity entity = await GetExistingEntity<TEntity>(dbContext, entityId);

                        Validate(dbContext);

                        foreach (Action<TEntity> action in update.PropertyUpdateActions)
                        {
                            action(entity);
                        }

                        await dbContext.SaveChangesAsync();
                    }
                    catch (ValidationException ex)
                    {
                        LogValidationException(ex);
                        throw;
                    }
                    catch (DbUpdateException updex)
                    {
                        LogDbUpdateException(updex);
                        throw;
                    }
                    catch (Exception e)
                    {
                        LogException(e);
                        throw;
                    }
                }
            });
        }

        public async Task Delete<TEvent>(EventEnvelope<TEvent> e)
            where TEvent : Event
        {
            await AddJobToQueue(async () =>
            {
                string eventName = typeof(TEvent).Name;

                // Map
                EventEntityDelete<TEntity> delete;

                try
                {
                    delete = await Mapper.Map<Task<EventEntityDelete<TEntity>>>(e);
                }
                catch (Exception ex)
                {
                    _logger?.LogError($"Error Deleting Entity for Type: {_entityName} - Mapping failed. (Event: {eventName}, Source Id: {e.Id})", ex);
                    // TODO: should we throw this or just swallow it and return here?
                    throw;
                }

                using (IServiceScope scope = this.ServiceProvider.CreateScope())
                using (DbContext dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>())
                {
                    Guid entityId = delete.EntityId;

                    try
                    {
                        // This will throw exception if entity not found
                        TEntity entity = await GetExistingEntity<TEntity>(dbContext, entityId);

                        DbSet<TEntity> entitySet = dbContext.Set<TEntity>();

                        entitySet.Remove(entity);

                        // Save changes
                        await dbContext.SaveChangesAsync();
                    }
                    catch (ValidationException ex)
                    {
                        LogValidationException(ex);
                        throw;
                    }
                    catch (DbUpdateException updex)
                    {
                        LogDbUpdateException(updex);
                        throw;
                    }
                    catch (Exception e)
                    {
                        LogException(e);
                        throw;
                    }
                }
            });
        }

        public async Task AddItem<TChildEntity, TEvent>(EventEnvelope<TEvent> e)
            where TChildEntity : class
            where TEvent : Event
        {
            await AddJobToQueue(async () =>
            {
                string eventName = typeof(TEvent).Name;

                // Map
                EventEntityCollection<TEntity, TChildEntity> collection;

                try
                {
                    collection = await Mapper.Map<Task<EventEntityCollection<TEntity, TChildEntity>>>(e);
                }
                catch (Exception ex)
                {
                    _logger?.LogError($"Error Adding Item for Type: {_entityName} - Mapping failed. (Event: {eventName}, Source Id: {e.Id}) from {this.GetType().Name}", ex);
                    // TODO: should we throw this or just swallow it and return here?
                    throw;
                }

                using (IServiceScope scope = this.ServiceProvider.CreateScope())
                using (DbContext dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>())
                {
                    Guid entityId = collection.EntityId;
                    Guid childId = collection.ChildEntityId;

                    try
                    {
                        // This will throw exception if Entity not found
                        TEntity entity = await GetExistingEntity<TEntity>(dbContext, entityId);

                        // This will throw exception if Child Entity not found
                        TChildEntity childEntity = await GetExistingEntity<TChildEntity>(dbContext, childId);

                        ICollection<TChildEntity> childCollection = collection.Collection.Compile().Invoke(entity) ?? new List<TChildEntity>();

                        childCollection.Add(childEntity);

                        Validate(dbContext);

                        await dbContext.SaveChangesAsync();
                    }
                    catch (ValidationException ex)
                    {
                        LogValidationException(ex);
                        throw;
                    }
                    catch (DbUpdateException updex)
                    {
                        LogDbUpdateException(updex);
                        throw;
                    }
                    catch (Exception e)
                    {
                        LogException(e);
                        throw;
                    }
                }
            });
        }

        public async Task RemoveItem<TChildEntity, TEvent>(EventEnvelope<TEvent> e)
            where TChildEntity : class
            where TEvent : Event
        {
            await AddJobToQueue(async () =>
            {
                string eventName = typeof(TEvent).Name;

                // Map
                EventEntityCollection<TEntity, TChildEntity> collection;

                try
                {
                    collection = await Mapper.Map<Task<EventEntityCollection<TEntity, TChildEntity>>>(e);
                }
                catch (Exception ex)
                {
                    _logger?.LogError($"Error Removing Item for Type: {_entityName} - Mapping failed. (Event: {eventName}, Source Id: {e.Id}) from {this.GetType().Name}", ex);
                    // TODO: should we throw this or just swallow it and return here?
                    throw;
                }

                using (IServiceScope scope = this.ServiceProvider.CreateScope())
                using (DbContext dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>())
                {
                    Guid entityId = collection.EntityId;
                    Guid childId = collection.ChildEntityId;

                    try
                    {
                        // This will throw exception if Entity not found
                        TEntity entity = await GetExistingEntity<TEntity>(dbContext, entityId);

                        // This will throw exception if Child Entity not found
                        TChildEntity childEntity = await GetExistingEntity<TChildEntity>(dbContext, childId);

                        ICollection<TChildEntity> childCollection = collection.Collection.Compile().Invoke(entity) ?? new List<TChildEntity>();

                        childCollection.Add(childEntity);

                        dbContext.Set<TEntity>().Attach(entity);

                        childCollection.Remove(childEntity);

                        Validate(dbContext);

                        await dbContext.SaveChangesAsync();
                    }
                    catch (ValidationException ex)
                    {
                        LogValidationException(ex);
                        throw;
                    }
                    catch (DbUpdateException updex)
                    {
                        LogDbUpdateException(updex);
                        throw;
                    }
                    catch (Exception e)
                    {
                        LogException(e);
                        throw;
                    }
                }
            });
        }

        private async Task<T> GetExistingEntity<T>(DbContext dbContext, object id)
            where T : class
        {
            Type primaryKeyType = GetPrimaryKeyPropertyType<T>();

            // Convert the primary key Id we looked up via the id to the type of the PK of the entity
            object primaryKeyId = Convert.ChangeType(id, primaryKeyType);

            // Look up the entity in the DB
            T? entity = await dbContext.Set<T>().FindAsync(primaryKeyId);
            if (entity == null)
            {
                string message = $"Error Getting Existing Entity: Entity does not exist for Type: {typeof(T).Name}, Id: {id}";
                _logger?.LogError(message);
                throw new EntityDoesNotExistException(message);
            }

            return entity;
        }

        private readonly ConcurrentDictionary<Type, Type> _entityTypePrimaryKeyTypeCache = new ConcurrentDictionary<Type, Type>();

        private Type GetPrimaryKeyPropertyType<T>()
            where T : class
        {
            Type type = typeof(T);

            if (_entityTypePrimaryKeyTypeCache.TryGetValue(type, out Type? primaryKeyType))
            {
                return primaryKeyType;
            }

            PropertyInfo? propertyInfo = type.GetProperties().FirstOrDefault(p => Attribute.IsDefined(p, typeof(KeyAttribute)));

            if (propertyInfo?.PropertyType == null)
            {
                string error = $"Primary Key not found for Type: {type.Name}";
                _logger?.LogError(error);
                throw new PrimaryKeyNotFoundException(error);
            }

            primaryKeyType = propertyInfo.PropertyType;

            _entityTypePrimaryKeyTypeCache.TryAdd(type, primaryKeyType);

            return primaryKeyType;
        }

        private static void Validate(DbContext dbContext)
        {
            IEnumerable<object> entities = from e in dbContext.ChangeTracker.Entries()
                           where e.State == EntityState.Added
                               || e.State == EntityState.Modified
                           select e.Entity;

            foreach (object dbEntity in entities)
            {
                ValidationContext validationContext = new ValidationContext(dbEntity);
                Validator.ValidateObject(dbEntity, validationContext);
            }
        }

        private void LogException(Exception ex)
        {
            _logger?.LogError($"Exception for Entity of type: {_entityName} ({ex.Message}");
        }

        private void LogDbUpdateException(DbUpdateException ex)
        {
            _logger?.LogError($"DbUpdateException for Entity of type: {_entityName} - {ex.Message}");

            if (ex.InnerException != null)
            {
                _logger?.LogError($"- InnerException ({ex.InnerException.GetType().Name}) {ex.InnerException.Message}");

                if (ex.InnerException.InnerException != null)
                {
                    _logger?.LogError($"- InnerInnerException ({ex.InnerException.InnerException.GetType().Name}) {ex.InnerException.InnerException.Message}");
                }
            }
        }

        private void LogValidationException(ValidationException ex)
        {
            _logger?.LogError($"ValidationException for Entity of type: {_entityName} - ({ex.Message}");
            _logger?.LogError($"Property Name: {ex.ValidationAttribute?.ErrorMessageResourceName} Message: {ex.ValidationAttribute?.ErrorMessage}");
        }
    }
}
