using System.Linq.Expressions;

namespace Troolio.Core.Projection
{
    public record EventEntityCollectionWithPredicates<TEntity, TChildEntity>(
        Expression<Func<TEntity, bool>> Entity,
        Expression<Func<TChildEntity, bool>> ChildEntity,
        Expression<Func<TEntity, ICollection<TChildEntity>>> Collection
    ) 
        where TEntity : class
        where TChildEntity : class;
}
