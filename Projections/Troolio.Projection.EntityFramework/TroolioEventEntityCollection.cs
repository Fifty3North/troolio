using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Troolio.Core.Projection
{
    public class TroolioEventEntityCollection<TParentEntity, TChildEntity>
            where TParentEntity : class
            where TChildEntity : class
    {
        public readonly Guid ParentGuid;

        public readonly Guid ChildGuid;

        public readonly Expression<Func<TParentEntity, ICollection<TChildEntity>>> Collection;

        public TroolioEventEntityCollection(Guid parentGuid, Guid childGuid, Expression<Func<TParentEntity, ICollection<TChildEntity>>> children)
        {
            ParentGuid = parentGuid;
            ChildGuid = childGuid;
            Collection = children;
        }
    }
}
