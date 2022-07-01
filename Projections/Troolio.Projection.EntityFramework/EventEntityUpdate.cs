namespace Troolio.Core.Projection
{

    public class EventEntityUpdate<TEntity>
        where TEntity : class
    {
        public readonly Guid EntityGuid;

        public readonly Action<TEntity>[] PropertyUpdateActions;

        public EventEntityUpdate(Guid entityGuid, params Action<TEntity>[] propertyUpdateActions)
        {
            EntityGuid = entityGuid;
            PropertyUpdateActions = propertyUpdateActions;
        }
    }
}
