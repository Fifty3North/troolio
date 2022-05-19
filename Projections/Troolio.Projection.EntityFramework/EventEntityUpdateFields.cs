using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Troolio.Projection.EntityFramework
{
    public class EventEntityUpdateFields<TEntity>
        where TEntity : class
    {
        public TEntity Entity { get; private set; }
        public Expression<Func<TEntity, bool>> PrimaryKey { get; private set; }
        public Expression<Func<TEntity, object>>[] ChangedProperties { get; private set; }

        public EventEntityUpdateFields(TEntity entity, Expression<Func<TEntity, bool>> primaryKeyPredicate, params Expression<Func<TEntity, object>>[] changedProperties)
        {
            Entity = entity;
            PrimaryKey = primaryKeyPredicate;
            ChangedProperties = changedProperties;
        }
    }
}
