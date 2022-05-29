using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Omu.ValueInjecter;
using Orleankka;
using Orleans;
using Orleans.Streams;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;
using Troolio.Core.Projection.Exceptions;

namespace Troolio.Core.Projection
{
    public abstract class EntityFrameworkBatched<TEntity, TDbContext> : EFPersistance<TEntity, TDbContext>
            where TEntity : class
            where TDbContext : DbContext
    {
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

        private PropertyInfo? _entityPrimaryKeyPropertyInfo;

        /// <summary>
        /// Interval in which flush of queue should be instigated.
        /// </summary>
        private readonly TimeSpan flushPeriod = TimeSpan.FromMilliseconds(250);

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
            _entityPrimaryKeyPropertyInfo = GetPrimaryKeyPropertyInfo<TEntity>();

            IStreamProvider streamProvider = GetStreamProvider(Constants.ProjectionStreamPrefix);

            //var primaryKeyString = this.GetPrimaryKeyString();
            //var positionOfFirstDash = primaryKeyString.IndexOf('-');
            //var stringGuid = primaryKeyString.Substring(positionOfFirstDash + 1, primaryKeyString.Length - positionOfFirstDash - 1);
            //var guid = Guid.Parse(stringGuid);

            Guid guid = this.GetPrimaryKey(out string extension);
            IAsyncStream<IEventEnvelope> stream = streamProvider.GetStream<IEventEnvelope>(guid, extension);

            await stream.SubscribeAsync((envelope, token) => Receive(envelope));

            Timers.Register("flush", flushPeriod, flushPeriod, () => Self.Tell(new Commands.ProcessNow()));
        }

        /// <summary>
        /// Message handler to Process Job queue now.
        /// This is only ever intended to be called from tests.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task On(Commands.ProcessNow message)
        {
            await ProcessNow();
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
            if (_jobs.Count == 0)
            {
                await Task.CompletedTask;
            }
            else
            {
                if (_activeFlushCount > 0 && !forceFlush)
                {
                    // flush already in process and we don't want to force another one to wait for a go
                    return;
                }

                // lock here so we can clear the whole queue before another call to flush gets a go.
                await _queueProcessLock.WaitAsync();

                Interlocked.Increment(ref _activeFlushCount);

                EfJob? efJob;
                while (_jobs.TryPeek(out efJob))
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
                            _logger?.Log(LogLevel.Error, $"Retry count exceeded abandoning job for : {_entityName}", ex);
                        }
                    }
                }

                // release lock
                _queueProcessLock.Release();

                Interlocked.Decrement(ref _activeFlushCount);
            }
        }

        protected Task AddJobToQueue(Func<Task> job)
        {
            _jobs.Enqueue(new EfJob(job));
            return Task.CompletedTask;
        }

        public override async Task<int> Create<TEvent>(EventEnvelope<TEvent> e)
        {
            await AddJobToQueue(async () =>
            {
                string eventName = typeof(TEvent).Name;

                _logger?.Log(LogLevel.Trace, $"Starting Create for Event: {eventName} : {e.Id} from {this.GetType().Name}");

                // Map
                EventEntityCreate<TEntity> create;

                try
                {
                    create = await Mapper.Map<Task<EventEntityCreate<TEntity>>>(e);
                }
                catch (Exception ex)
                {
                    _logger?.LogError($"Error Creating Entity for Type: {_entityName} - Mapping failed. (Event: {eventName}, Source Id: {e.Id})  from {this.GetType().Name}", ex);
                    // TODO: should we throw this or just swallow it and return here?
                    throw;
                }
                
                using (DbContext dbContext = this.RequestServices!.GetRequiredService<TDbContext>())
                {
                    TEntity entity = create.Entity;
                    Guid entityId = create.EntityGuid;

                    try
                    {
                        _logger?.LogTrace($"About to InsertEntity for {eventName} : {e.Id} from {this.GetType().Name}");

                        int insertedItemCount = await InsertEntity(dbContext, entity);
                        if (insertedItemCount <= 0)
                        {
                            _logger?.LogError($"Failed to Insert Entity for Type: {_entityName} - Inserted Item Count = 0. (Event: {eventName}, Source Id: {e.Id}, Entity Id: {entityId})");
                            // TODO: We should throw exception here or just return
                        }

                        _logger?.LogTrace($"Finished InsertEntity for {eventName} : {e.Id}  from {this.GetType().Name}");

                        object? oId = _entityPrimaryKeyPropertyInfo?.GetValue(entity, null);
                        
                        if (oId is null)
                        {
                            _logger?.LogError($"Failed to Insert Entity for Type: {_entityName} - Primary Key Id = 0. (Event: {eventName}, Source Id: {e.Id}, Entity Id: {entityId})");
                            // TODO: We should throw exception here or just return
                        }
                    }
                    catch (ValidationException ex)
                    {
                        CatchValidationError(ex);
                        throw;
                    }
                    catch (DbUpdateException updex)
                    {
                        CatchUpdateError(updex);
                    }
                    catch (Exception e)
                    {
                        CatchError(e);
                    }
                }

                _logger?.LogTrace($"Finished Create for Event: {eventName} : {e.Id}  from {this.GetType().Name}");
            });

            return 1;
        }

        public override async Task Update<TEvent>(EventEnvelope<TEvent> e)
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

                using (DbContext dbContext = this.RequestServices!.GetRequiredService<TDbContext>())
                {
                    Guid entityId = update.EntityGuid;

                    try
                    {
                        _logger?.LogTrace($"Update Entity for Type: {_entityName}. (Event: {eventName}, Source Id: {e.Id}, Entity Id: {entityId})");
                        var errorMessage = $"Error Updating Entity for Type: {_entityName} - Existing entity does not exist. (Event: {eventName}, Source Id: {e.Id}, Entity Id: {entityId})";

                        TEntity existingEntity = await GetExistingEntity<TEntity>(dbContext, entityId, _entityPrimaryKeyPropertyInfo?.PropertyType ?? throw new Exception(errorMessage));

                        if (existingEntity == null)
                        {
                            _logger?.LogError(errorMessage);
                        }
                        else
                        {
                            Validate(dbContext);
                            await UpdateExistingEntity(dbContext, existingEntity, update.PropertyUpdateActions);
                        }
                    }
                    catch (ValidationException ex)
                    {
                        CatchValidationError(ex);
                        throw;
                    }
                    catch (DbUpdateException updex)
                    {
                        CatchUpdateError(updex);
                    }
                    catch (Exception e)
                    {
                        CatchError(e);
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

                using (DbContext dbContext = this.RequestServices!.GetRequiredService<TDbContext>())
                {
                    Guid entityId = delete.EntityGuid;

                    try
                    {
                        // Convert the primary key Id we looked up via the Guid to the type of the PK of the entity
                        object primaryKeyId = Convert.ChangeType(entityId, _entityPrimaryKeyPropertyInfo?.PropertyType ?? throw new Exception("Property Type is null"));

                        // Get the entity record 
                        DbSet<TEntity> entitySet = dbContext.Set<TEntity>();
                        TEntity? entity = entitySet.Find(primaryKeyId);
                        if (entity == null)
                        {
                            _logger?.LogError($"Error Deleting Entity for Type: {_entityName} - Entity does not exist. (Event: {eventName}, Source Id: {e.Id}, Entity Id: {entityId})");
                            return;
                        }


                        entitySet.Remove(entity);

                        // Save changes
                        await dbContext.SaveChangesAsync();
                    }
                    catch (ValidationException ex)
                    {
                        CatchValidationError(ex);
                        throw;
                    }
                    catch (DbUpdateException updex)
                    {
                        CatchUpdateError(updex);
                    }
                    catch (Exception e)
                    {
                        CatchError(e);
                    }
                }
            });
        }

        public override async Task AddItem<TChildEntity, TEvent>(EventEnvelope<TEvent> e)
        {
            await AddJobToQueue(async () =>
            {
                string eventName = typeof(TEvent).Name;

                // Map
                TroolioEventEntityCollection<TEntity, TChildEntity> collection;

                try
                {
                    collection = await Mapper.Map<Task<TroolioEventEntityCollection<TEntity, TChildEntity>>>(e);
                }
                catch (Exception ex)
                {
                    _logger?.LogError($"Error Adding Item for Type: {_entityName} - Mapping failed. (Event: {eventName}, Source Id: {e.Id}) from {this.GetType().Name}", ex);
                    // TODO: should we throw this or just swallow it and return here?
                    throw;
                }

                using (DbContext dbContext = this.RequestServices!.GetRequiredService<TDbContext>())
                {
                    Guid parentId = collection.ParentGuid;
                    Guid childId = collection.ChildGuid;

                    try
                    {
                        TEntity parent = await GetExistingEntity<TEntity>(dbContext, parentId, _entityPrimaryKeyPropertyInfo?.PropertyType ?? throw new Exception("Property Type is null"));
                        if (parent == null)
                        {
                            _logger?.LogError($"Error Adding Item for Type: {_entityName} - Parent entity does not exist. (Event: {eventName}, Source Id: {e.Id}, Parent Id: {parentId}, Child Id: {childId}) from {this.GetType().Name}");
                            return;
                        }

                        TChildEntity child = await GetExistingEntity<TChildEntity>(dbContext, childId);
                        if (child == null)
                        {
                            _logger?.LogError($"Error Adding Item for Type: {_entityName} - Child entity does not exist. (Event: {eventName}, Source Id: {e.Id}, Parent Id: {parentId}, Child Id: {childId}) from {this.GetType().Name}");
                            return;
                        }

                        await LinkEntity(dbContext, parent, child, collection.Collection);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError($"Error Adding Item for Type: {_entityName} - {ex.Message}. (Event: {eventName}, Source Id: {e.Id}, Parent Id: {parentId}, Child Id: {childId}) from {this.GetType().Name}", ex);
                        // TODO: should we throw this or just swallow it and return here?
                        throw;
                    }
                }
            });
        }

        public override async Task RemoveItem<TChildEntity, TEvent>(EventEnvelope<TEvent> e)
        {
            await AddJobToQueue(async () =>
            {
                string eventName = typeof(TEvent).Name;

                // Map
                TroolioEventEntityCollection<TEntity, TChildEntity> collection;

                try
                {
                    collection = await Mapper.Map<Task<TroolioEventEntityCollection<TEntity, TChildEntity>>>(e);
                }
                catch (Exception ex)
                {
                    _logger?.LogError($"Error Removing Item for Type: {_entityName} - Mapping failed. (Event: {eventName}, Source Id: {e.Id}) from {this.GetType().Name}", ex);
                    // TODO: should we throw this or just swallow it and return here?
                    throw;
                }

                using (DbContext dbContext = this.RequestServices!.GetRequiredService<TDbContext>())
                {
                    Guid parentId = collection.ParentGuid;
                    Guid childId = collection.ChildGuid;

                    try
                    {
                        TEntity parent = await GetExistingEntity<TEntity>(dbContext, parentId, _entityPrimaryKeyPropertyInfo?.PropertyType ?? throw new Exception("Property Type is null"));
                        if (parent == null)
                        {
                            _logger?.LogError($"Error Removing Item for Type: {_entityName} - Parent entity does not exist. (Event: {eventName}, Source Id: {e.Id}, Parent Id: {parentId}, Child Id: {childId}) from {this.GetType().Name}");
                            return;
                        }

                        TChildEntity child = await GetExistingEntity<TChildEntity>(dbContext, childId);
                        if (child == null)
                        {
                            _logger?.LogError($"Error Removing Item for Type: {_entityName} - Child entity does not exist. (Event: {eventName}, Source Id: {e.Id}, Parent Id: {parentId}, Child Id: {childId}) from {this.GetType().Name}");
                            return;
                        }

                        await UnlinkEntity(dbContext, parent, child, collection.Collection);
                    }
                    catch (ValidationException ex)
                    {
                        CatchValidationError(ex);
                        throw;
                    }
                    catch (DbUpdateException updex)
                    {
                        CatchUpdateError(updex);
                    }
                    catch (Exception e)
                    {
                        CatchError(e);
                    }
                }
            });
        }

        private async Task<T> GetExistingEntity<T>(DbContext dbContext, object id)
            where T : class
        {
            PropertyInfo primaryKeyPropertyInfo = GetPrimaryKeyPropertyInfo<T>();
            return await GetExistingEntity<T>(dbContext, id, primaryKeyPropertyInfo.PropertyType);
        }

        private async Task<T> GetExistingEntity<T>(DbContext dbContext, object id, Type primaryKeyType)
            where T : class
        {
            // Convert the primary key Id we looked up via the id to the type of the PK of the entity
            object primaryKeyId = Convert.ChangeType(id, primaryKeyType);
            
            // Look up the entity in the DB
            T? entity = await dbContext.Set<T>().FindAsync(primaryKeyId);
            if (entity == null)
            {
                string message = $"Error Getting Existing Entity: Entity does not exist for Type: {typeof(T).Name}, Id: {id}";
                _logger?.LogError(message);
                throw new ArgumentException(message);
            }

            return entity;
        }

        protected async Task<TEntity> GetExistingEntity(DbContext dbContext, Expression<Func<TEntity, bool>> entityPredicate)
        {
            TEntity? entity = await dbContext.Set<TEntity>().FirstOrDefaultAsync(entityPredicate);

            if (entity == null)
            {
                string message = $"Error Getting Existing Entity: Entity does not exist for Type: {typeof(TEntity).Name}";
                _logger?.LogError(message);
                throw new ArgumentException(message);
            }

            return entity;
        }

        protected async Task UpdateExistingEntity(DbContext dbContext, Expression<Func<TEntity, bool>> entityPredicate, params Action<TEntity>[] propertyUpdateActions)
        {
            TEntity existingEntity = await GetExistingEntity(dbContext, entityPredicate);
            if (existingEntity == null)
            {
                _logger?.LogError($"Error Updating Entity - Existing entity does not exist for Type: {_entityName} from {this.GetType().Name}");
            }
            else
            {
                await UpdateExistingEntity(dbContext, existingEntity, propertyUpdateActions);
            }
        }

        protected async Task UpdateExistingEntity(DbContext dbContext, TEntity existingEntity, params Action<TEntity>[] propertyUpdateActions)
        {
            if (propertyUpdateActions == null || propertyUpdateActions.Length == 0)
            {
                // Nothing to update
                return;
            }

            foreach (Action<TEntity> action in propertyUpdateActions)
            {
                action(existingEntity);
            }

            try
            {
                await dbContext.SaveChangesAsync();
            }
            catch (ValidationException ex)
            {
                CatchValidationError(ex);
                throw;
            }
            catch (DbUpdateException updex)
            {
                CatchUpdateError(updex);
            }
            catch (Exception e)
            {
                CatchError(e);
            }
        }

        protected async Task DeleteExistingEntity(DbContext dbContext, Expression<Func<TEntity, bool>> entityPredicate)
        {
            TEntity existingEntity = await GetExistingEntity(dbContext, entityPredicate);
            if (existingEntity == null)
            {
                _logger?.LogError($"Existing entity to Delete does not exist for Type: {_entityName}");
            }
            else
            {
                await DeleteExistingEntity(dbContext, existingEntity);
            }
        }

        private async Task DeleteExistingEntity(DbContext dbContext, TEntity existingEntity)
        {
            try
            {
                dbContext.Set<TEntity>().Remove(existingEntity);
                await dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error Deleting Entity for Type: {_entityName} - {ex.Message}", ex);
                throw;
            }
        }

        private PropertyInfo GetPrimaryKeyPropertyInfo<T>()
            where T : class
        {
            Type type = typeof(T);
            PropertyInfo? propertyInfo = type.GetProperties().FirstOrDefault(p => Attribute.IsDefined(p, typeof(KeyAttribute)));
            if (propertyInfo == null)
            {
                _logger?.LogError($"Primary Key not found for Type: {type}");
                throw new PrimaryKeyNotFoundException($"Primary Key not found for Type: {type}");
            }
            return propertyInfo;
        }
    }
}
