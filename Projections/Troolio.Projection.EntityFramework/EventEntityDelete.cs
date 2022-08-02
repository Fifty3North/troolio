namespace Troolio.Core.Projection
{
    public record EventEntityDelete<TEntity>(Guid EntityId)
        where TEntity : class;
}
