﻿using Orleans.Concurrency;
using Sample.Shared.ActorInterfaces;
using Sample.Shared.InternalCommands;
using Troolio.Core;

namespace Sample.Host.App.ShoppingList
{
    [Reentrant]
    [StatelessWorker]
    public class EmailActorMock : DispatchActor, IEmailActor
    {
        #region Command Handlers

        public async Task Handle(SendEmailNotification command)
        {
            try
            {
                string email = command.Email;
                //string subject = "New Shopping list user.";
                string content = $"The {command.Description} shopping list had a new contributor join.";

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