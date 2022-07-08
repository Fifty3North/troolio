using System.Linq.Expressions;

namespace Troolio.Projection.Redis.Persistence
{
    public class RedisEventEntityUpdateFields<TEntity> : RedisEventEntity<TEntity> where TEntity : class
    {
        public TEntity Entity { get; set; }

        public Expression<Func<TEntity, bool>> PrimaryKey { get; set; }

        public Expression<Func<TEntity, object>>[] ChangedProperties { get; set; }

        public RedisEventEntityUpdateFields(TEntity entity, Expression<Func<TEntity, bool>> primaryKeyPredicate, params Expression<Func<TEntity, object>>[] changedProperties)
        {
            Entity = entity;
            PrimaryKey = primaryKeyPredicate;
            ChangedProperties = changedProperties;
        }
    }
}