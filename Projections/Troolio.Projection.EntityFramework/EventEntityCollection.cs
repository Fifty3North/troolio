using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Troolio.Core.Projection
{
    public class EventEntityCollection<TParentEntity, TChildEntity>
            where TParentEntity : class
            where TChildEntity : class
    {
        public Expression<Func<TParentEntity, bool>> Parent { get; private set; }

        public Expression<Func<TChildEntity, bool>> Child { get; private set; }

        public Expression<Func<TParentEntity, ICollection<TChildEntity>>> Collection { get; private set; }

        public EventEntityCollection(Expression<Func<TParentEntity, bool>> parent, Expression<Func<TChildEntity, bool>> child, Expression<Func<TParentEntity, ICollection<TChildEntity>>> children)
        {
            Parent = parent;
            Child = child;
            Collection = children;
        }
    }
}
