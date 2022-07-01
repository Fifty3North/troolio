namespace Troolio.Core.Projection
{
    public class EventEntityDelete<TEntity>
        where TEntity : class
    {
        public readonly Guid EntityGuid;

        public EventEntityDelete(Guid entityGuid)
        {
            EntityGuid = entityGuid;
        }
    }
}
