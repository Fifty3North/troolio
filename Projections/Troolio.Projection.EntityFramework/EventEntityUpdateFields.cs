using System.Linq.Expressions;

namespace Troolio.Projection.EntityFramework
{
    public record EventEntityUpdateFields<TEntity>(
        TEntity Entity, 
        Expression<Func<TEntity, bool>> PrimaryKey, 
        Expression<Func<TEntity, object>>[] ChangedProperties
    ) where TEntity : class;
}
