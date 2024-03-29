﻿using System.Linq.Expressions;

namespace Troolio.Core.Projection
{
    public record EventEntityCollection<TEntity, TChildEntity>(
        Guid EntityId,
        Guid ChildEntityId,
        Expression<Func<TEntity, ICollection<TChildEntity>>> Collection
    )
        where TEntity : class
        where TChildEntity : class;
}
