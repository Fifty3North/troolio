using System;

namespace Troolio.Core.Projection
{
    public class EventEntityCreate<TEntity>
        where TEntity : class
    {
        public readonly Guid EntityGuid;

        public readonly TEntity Entity;

        public EventEntityCreate(Guid entityGuid, TEntity entity)
        {
            EntityGuid = entityGuid;
            Entity = entity;
        }
    }
}
