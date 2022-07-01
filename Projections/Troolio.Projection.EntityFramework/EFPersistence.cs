using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Omu.ValueInjecter;
using Orleans;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;

using Troolio.Projection.EntityFramework;

namespace Troolio.Core.Projection
{
    public abstract class EFPersistance<TEntity, TDbContext> : DispatchActor, IIncomingGrainCallFilter
        where TEntity : class
        where TDbContext : DbContext
    {
        protected Microsoft.Extensions.Logging.ILogger<EFPersistance<TEntity, TDbContext>>? _logger;
        protected static readonly string _entityName = typeof(TEntity).Name;

        public override Task OnActivateAsync()
        {
            _logger = RequestServices?.GetRequiredService<ILogger<EFPersistance<TEntity, TDbContext>>>();
            return base.OnActivateAsync();
        }

        public IServiceProvider? RequestServices { get; set; }

        public async Task Invoke(IIncomingGrainCallContext context)
        {
            var scope = this.ServiceProvider.CreateScope();
            try
            {
                this.RequestServices = scope.ServiceProvider;
                await context.Invoke();
            }
            finally
            {
                scope.Dispose();
                this.RequestServices = null;
            }
        }

        public virtual async Task<int> Create<TEvent>(EventEnvelope<TEvent> e)
            where TEvent : Event
        {
            using (DbContext dbContext = this.RequestServices!.GetRequiredService<TDbContext>())
            {
                TEntity entity = await Mapper.Map<Task<TEntity>>(e);
                return await InsertEntity(dbContext, entity);
            }
        }

        public virtual async Task Update<TEvent>(EventEnvelope<TEvent> e)
            where TEvent : Event
        {
            using (DbContext dbContext = this.RequestServices!.GetRequiredService<TDbContext>())
            {
                EventEntityUpdateFields<TEntity> entityUpdate = await Mapper.Map<Task<EventEntityUpdateFields<TEntity>>>(e);
                await UpdateEntityProperties(dbContext, entityUpdate.Entity, entityUpdate.PrimaryKey, entityUpdate.ChangedProperties);
            }
        }

        public virtual async Task AddItem<TChildEntity, TEvent>(EventEnvelope<TEvent> e)
            where TChildEntity : class
            where TEvent : Event
        {
            using (DbContext dbContext = this.RequestServices!.GetRequiredService<TDbContext>())
            {
                EventEntityCollection<TEntity, TChildEntity> entityCollection = await Mapper.Map<Task<EventEntityCollection<TEntity, TChildEntity>>>(e);
                await LinkEntity(dbContext, entityCollection.Parent, entityCollection.Child, entityCollection.Collection);
            }
        }

        public virtual async Task RemoveItem<TChildEntity, TEvent>(EventEnvelope<TEvent> e)
            where TChildEntity : class
            where TEvent : Event
        {
            using (DbContext dbContext = this.RequestServices!.GetRequiredService<TDbContext>())
            {
                EventEntityCollection<TEntity, TChildEntity> entityCollection = await Mapper.Map<Task<EventEntityCollection<TEntity, TChildEntity>>>(e);
                await UnlinkEntity(dbContext, entityCollection.Parent, entityCollection.Child, entityCollection.Collection);
            }
        }

        protected virtual async Task<int> InsertEntity(DbContext dbContext, TEntity entity)
        {
            int insertedItemCount = -1;

            DbSet<TEntity> entitySet = dbContext.Set<TEntity>();

            try
            {
                var changedEntity = entitySet.Add(entity);
            }
            catch (Exception ex)
            {
                _logger?.Log(LogLevel.Error, $"Error adding Entity to set ({_entityName}) ({ex.GetType().Name}) (Non DbEntityValidationException): {ex.Message}");
            }

            try
            {
                Validate(dbContext);
                insertedItemCount = await dbContext.SaveChangesAsync();
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

            return insertedItemCount;
        }

        protected async Task UpdateEntityProperties(DbContext dbContext, TEntity entity, Expression<Func<TEntity, bool>> primaryKey, params Expression<Func<TEntity, object>>[] propertyPointers)
        {
            TEntity existingEntity = dbContext.Set<TEntity>().Where(primaryKey).First();
            await UpdateEntityProperties(dbContext, entity, existingEntity, propertyPointers);
        }

        protected async Task UpdateEntityProperties(DbContext dbContext, TEntity updatedEntity, TEntity existingEntity, params Expression<Func<TEntity, object>>[] propertyPointers)
        {
            EntityEntry<TEntity> updatedItem = dbContext.Entry(updatedEntity);
            EntityEntry<TEntity> existingItem = dbContext.Entry(existingEntity);

            foreach (var prop in propertyPointers)
            {
                PropertyEntry<TEntity, object> existingProperty = existingItem.Property(prop);
                existingProperty.CurrentValue = updatedItem.Property(prop).CurrentValue;
                existingProperty.IsModified = true;
            }

            try
            {
                Validate(dbContext);

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
            catch(Exception e)
            {
                CatchError(e);
            }
        }

        protected void CatchError(Exception e)
        {
            _logger?.Log(LogLevel.Error, $"Error Linking Entity: {_entityName} ({e.Message}");
        }

        protected void CatchUpdateError(DbUpdateException ex)
        {
            _logger?.Log(LogLevel.Error, $"DbUpdateException Inserting Entity of type: {_entityName} - {ex.Message}");

            if (ex.InnerException != null)
            {
                _logger?.Log(LogLevel.Error, $"- InnerException ({ex.InnerException.GetType().Name}) {ex.InnerException.Message}");

                if (ex.InnerException.InnerException != null)
                {
                    _logger?.Log(LogLevel.Error, $"- InnerInnerException ({ex.InnerException.InnerException.GetType().Name}) {ex.InnerException.InnerException.Message}");
                }
            }
        }

        protected void CatchValidationError(ValidationException ex)
        {
            _logger?.Log(LogLevel.Error, $"Error Linking Entity: {_entityName} ({ex.Message}");
            _logger?.Log(LogLevel.Error, $"Property Name: {ex.ValidationAttribute?.ErrorMessageResourceName} Message: {ex.ValidationAttribute?.ErrorMessage}");
        }

        protected async Task LinkEntity<TChildEntity>(DbContext dbContext, Expression<Func<TEntity, bool>> entityQuery, Expression<Func<TChildEntity, bool>> childEntityQuery, Expression<Func<TEntity, ICollection<TChildEntity>>> collectionFunc)
            where TChildEntity : class
        {
            TEntity? entity = dbContext.Set<TEntity>().FirstOrDefault(entityQuery);
            TChildEntity? childEntity = dbContext.Set<TChildEntity>().FirstOrDefault(childEntityQuery);
            
            if (entity != null && childEntity != null)
            {
                await LinkEntity(dbContext, entity, childEntity, collectionFunc);
            }
        }

        protected async Task LinkEntity<TChildEntity>(DbContext dbContext, TEntity entity, TChildEntity childEntity, Expression<Func<TEntity, ICollection<TChildEntity>>> collectionFunc)
            where TChildEntity : class
        {
            ICollection<TChildEntity> collection = collectionFunc.Compile().Invoke(entity);

            if (collection == null)
            {
                collection = new List<TChildEntity>();
            }

            collection.Add(childEntity);

            try
            {
                Validate(dbContext);

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

        protected static void Validate(DbContext dbContext)
        {
            var entities = from e in dbContext.ChangeTracker.Entries()
                           where e.State == EntityState.Added
                               || e.State == EntityState.Modified
                           select e.Entity;

            foreach (var dbEntity in entities)
            {
                var validationContext = new ValidationContext(dbEntity);
                Validator.ValidateObject(dbEntity, validationContext);
            }
        }

        protected async Task UnlinkEntity<TChildEntity>(DbContext dbContext, Expression<Func<TEntity, bool>> entityQuery, Expression<Func<TChildEntity, bool>> childEntityQuery, Expression<Func<TEntity, ICollection<TChildEntity>>> collectionFunc)
            where TChildEntity : class
        {
            TEntity? entity = dbContext.Set<TEntity>().FirstOrDefault(entityQuery);
            TChildEntity? childEntity = dbContext.Set<TChildEntity>().FirstOrDefault(childEntityQuery);
            
            if (entity != null && childEntity != null)
            {
                await UnlinkEntity<TChildEntity>(dbContext, entity, childEntity, collectionFunc);
            }
        }

        protected async Task UnlinkEntity<TChildEntity>(DbContext dbContext, TEntity entity, TChildEntity childEntity, Expression<Func<TEntity, ICollection<TChildEntity>>> collectionFunc)
            where TChildEntity : class
        {
            ICollection<TChildEntity> collection = collectionFunc.Compile().Invoke(entity);
            collection.Add(childEntity);
            dbContext.Set<TEntity>().Attach(entity);
            collection.Remove(childEntity);

            try
            {
                Validate(dbContext);

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
    }
}
