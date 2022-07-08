using System.Linq.Expressions;

namespace Troolio.Projection.Redis.Persistence
{
    public class RedisEventEntityCollection<TParentEntity, TChildEntity>
          where TParentEntity : class
          where TChildEntity : class
    {
        public readonly Guid ParentId;

        public readonly Guid ChildId;

        public readonly Expression<Func<TParentEntity, ICollection<TChildEntity>>> Collection;

        public RedisEventEntityCollection(Guid parentId, Guid childId, Expression<Func<TParentEntity, ICollection<TChildEntity>>> children)
        {
            ParentId = parentId;
            ChildId = childId;
            Collection = children;
        }
    }
}