using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orleankka;
using Orleankka.Client;
using Orleankka.Cluster;
using Orleankka.Meta;
using Orleans;
using Orleans.Hosting;
using Sample.Host.App.ShoppingList;
using Sample.Shared.ActorInterfaces;
using Troolio.Core;
using Troolio.Stores;

namespace Troolio.Tests.Setup
{
    public static class ActorSystemServer
    {
        static ISiloHost _host;
        static string diagnosticsLevel = "Verbose";
        private static IClusterClient _client;

        public static async Task Start()
        {
            ISiloHostBuilder hostBuilder = new SiloHostBuilder()
                //.ConfigureServices(x => x.AddTransient<IStore, MartenStore>())
                .ConfigureServices(x => x.AddTransient<IStore, FileSystemStore>())
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddJsonFile("appsettings.json");
                })
                .ConfigureApplicationParts(x => x
                    .AddApplicationPart(typeof(ShoppingListActor).Assembly)
                    .AddApplicationPart(typeof(IShoppingListActor).Assembly)
                    .WithTransactionSupport()
                    .WithCodeGeneration())
// TODO Exclude all actors excexpt the ones required for the running tests
// That way we can shut down the actor system and restart it on demand without
// actors being registered in the Orleankka dispatcher multiple times which causes exception

//.Configure<GrainClassOptions>(options => {
//    Takes grain implementation type not the interface
//    options.ExcludedGrainTypes.Add("Troolio.Tests.ExampleOutput.ToDo.ToDoActor");
//})
                .UseOrleankka()
                .AddIncomingGrainCallFilter(async context =>
                {
                    object[] messages = context.Arguments;

                    if (diagnosticsLevel != "" && string.Equals(context.InterfaceMethod.Name, "ReceiveTell"))
                    {
                        if (messages.Length == 1)
                        {
                            object message = messages[0];
                            if (message != null)
                            {
                                Type messageType = message.GetType();

                                if (message is Command command)
                                {
                                    System.Diagnostics.Debug.WriteLine(">>>>>>" + messageType.Name);
                                    System.Diagnostics.Debug.WriteLine(">>>>>> messageId:" + command.Headers.MessageId);
                                    System.Diagnostics.Debug.WriteLine(">>>>>> causationId:" + command.Headers.CausationId);
                                    System.Diagnostics.Debug.WriteLine(">>>>>> correlationId:" + command.Headers.CorrelationId);
                                    System.Diagnostics.Debug.WriteLine(">>>>>> deviceId:" + command.Headers.DeviceId);
                                    System.Diagnostics.Debug.WriteLine(">>>>>> transactionId:" + command.Headers.TransactionId);
                                    System.Diagnostics.Debug.WriteLine(">>>>>> userId:" + command.Headers.UserId);
                                }
                                else if (message is InternalCommand icommand)
                                {
                                    System.Diagnostics.Debug.WriteLine(">>>>>>" + messageType.Name);
                                    System.Diagnostics.Debug.WriteLine(">>>>>> messageId:" + icommand.Headers.MessageId);
                                    System.Diagnostics.Debug.WriteLine(">>>>>> causationId:" + icommand.Headers.CausationId);
                                    System.Diagnostics.Debug.WriteLine(">>>>>> correlationId:" + icommand.Headers.CorrelationId);
                                    System.Diagnostics.Debug.WriteLine(">>>>>> deviceId:" + icommand.Headers.DeviceId);
                                    System.Diagnostics.Debug.WriteLine(">>>>>> transactionId:" + icommand.Headers.TransactionId);
                                    System.Diagnostics.Debug.WriteLine(">>>>>> userId:" + icommand.Headers.UserId);
                                }
                                else if (message is Event ev)
                                {
                                    System.Diagnostics.Debug.WriteLine(">>>>>>" + messageType.Name);
                                    System.Diagnostics.Debug.WriteLine(">>>>>> messageId:" + ev.Headers.MessageId);
                                    System.Diagnostics.Debug.WriteLine(">>>>>> causationId:" + ev.Headers.CausationId);
                                    System.Diagnostics.Debug.WriteLine(">>>>>> correlationId:" + ev.Headers.CorrelationId);
                                    System.Diagnostics.Debug.WriteLine(">>>>>> deviceId:" + ev.Headers.DeviceId);
                                    System.Diagnostics.Debug.WriteLine(">>>>>> transactionId:" + ev.Headers.TransactionId);
                                    System.Diagnostics.Debug.WriteLine(">>>>>> userId:" + ev.Headers.UserId);
                                }
                                else if (message is Message && diagnosticsLevel == "Verbose")
                                {
                                    System.Diagnostics.Debug.WriteLine(">>>>>>" + messageType.Name);
                                }
                            }
                        }
                    }

                    try
                    {
                        await context.Invoke();
                    }
                    catch (Exception e)
                    {
                        if (diagnosticsLevel != "")
                        {
                            System.Diagnostics.Debug.WriteLine(">>>>>>" + e.Message);

                            if (e.InnerException != null)
                            {
                                System.Diagnostics.Debug.WriteLine(">>>>>>" + e.InnerException.Message);
                            }
                        }

                        throw;
                    }
                });


            _host = await hostBuilder.Start();
        }

        public static async Task Shutdown()
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        public static async Task<IClientActorSystem> ConnectClient()
        {
            // nothing async (yet)
            await Task.CompletedTask;

            _client = _host.Connect();
            return _client.ActorSystem();
        }

        public static async Task DisconnectClient()
        {
            await _client.Close();
        }

        internal static async Task EnableTracing()
        {
            var client = _host.Services.GetRequiredService<IClientActorSystem>();
            var runtime = client.TypedActorOf<ITroolioRuntime>(Constants.SingletonActorId);
            await runtime.Tell(new EnableTracing());
        }
    }
}
