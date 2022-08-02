namespace Troolio.Core.Projection
{
    public record EventEntityUpdate<TEntity>(
        Guid EntityId, 
        Action<TEntity>[] PropertyUpdateActions
    ) 
        where TEntity : class;
}
