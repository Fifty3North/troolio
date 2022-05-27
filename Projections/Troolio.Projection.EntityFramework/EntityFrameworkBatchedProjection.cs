using Microsoft.EntityFrameworkCore;
using Orleans;

namespace Troolio.Core.Projection
{
    public abstract class EntityFrameworkBatchedProjection<TEntity, TDbContext> : EntityFrameworkBatched<TEntity, TDbContext>, IGrainWithGuidCompoundKey
            where TEntity : class
            where TDbContext : DbContext
    {
        public override Task OnActivateAsync()
        {
            SetupMappings();
            return base.OnActivateAsync();
        }
        protected abstract void SetupMappings();

        /// <summary>
        /// If the projection doesn't handle an event then ignore it.
        /// Otherwise, we will result in 'Orleankka.UnhandledMessageException'
        /// </summary>
        /// <param name="envelope"></param>
        /// <returns></returns>
        public override async Task<object> Receive(object envelope)
        {
            if (Dispatcher.CanHandle(envelope.GetType()))
            {
                return await base.Receive(envelope);
            }
            else
            {
                return Task.CompletedTask;
            }
        }

    }
}
