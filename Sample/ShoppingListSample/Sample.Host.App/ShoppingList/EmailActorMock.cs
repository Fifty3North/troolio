using Orleankka;
using Orleans.Concurrency;
using Sample.Shared.ShoppingList;

namespace Sample.Host.App.ShoppingList
{
    [Reentrant]
    [StatelessWorker]
    public class EmailActorMock : DispatchActorGrain, IEmailActor
    {
        public EmailActorMock(IActorRuntime? runtime = null)
            : base(runtime)
        {
        }

        #region Command Handlers

        public async Task Handle(SendEmailNotification command)
        {
            try
            {
                string email = command.email;
                //string subject = "New Shopping list user.";
                string content = $"The {command.description} shopping list had a new contributor join.";

                // Send the email to the provider (Not implemented for this sample)
                // await _emailProvider.SendEmail(email, subject, content);
                await Task.CompletedTask;
            }
            catch (Exception)
            {
                throw;
            }
        }

        #endregion Command Handlers
    }
}