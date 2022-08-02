namespace Troolio.Core.Projection
{
    public record EventEntityCreate<TEntity>(
        Guid EntityId, 
        TEntity Entity
    ) 
        where TEntity : class;
}
