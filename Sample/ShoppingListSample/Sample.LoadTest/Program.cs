using System;
using System.Text;
using System.Threading.Tasks;

using NBomber;
using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Plugins.Http.CSharp;
using NBomber.Plugins.Network.Ping;
using Newtonsoft.Json;

namespace Troolio.Deployment.LoadTest
{
    public class ListTitle
    {
        public string Title { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            // NOTE: Since the IP changes with each fresh deployment, it needs adding here:
            var serverIp = "127.0.0.1";
            var serverPath = "http://" + serverIp + ":8081";

            //var userId = "500110AC-CA87-4F7C-B8D2-EBAE76CB03F2";//Guid.NewGuid().ToString();
            var deviceId = "B8118E16-3530-4B65-94B9-CF38D2E5F749";//Guid.NewGuid().ToString();


            var step = Step.Create("create_list",
                clientFactory: HttpClientFactory.Create(),
                execute: async context =>
                {
                    var listId = Guid.NewGuid().ToString();

                    var listPrefix = "/ShoppingList/" + listId;

                    var requestPath = serverPath + listPrefix + "/CreateNewList";
                    var request = Http.CreateRequest("POST", requestPath)
                        .WithHeader("Accept", "*/*")
                        .WithHeader("userId", Guid.NewGuid().ToString())
                        .WithHeader("deviceId", deviceId)
                        .WithBody(new StringContent(JsonConvert.SerializeObject(new ListTitle { Title = "testlist"}), Encoding.UTF8, "application/json"));

                    //System.Console.WriteLine(requestPath);
                    context.Logger.Debug($"Creating new list: {requestPath}");
                    var response = await Http.Send(request, context);
                    //System.Console.WriteLine(response.StatusCode);
                    return response;
                }, TimeSpan.FromSeconds(30));

            var scenario = ScenarioBuilder
                .CreateScenario("simple_http", step)
                .WithWarmUpDuration(TimeSpan.FromSeconds(5))
                .WithLoadSimulations(
                    Simulation.InjectPerSec(rate: 1, during: TimeSpan.FromSeconds(5)),
                    Simulation.InjectPerSec(rate: 5, during: TimeSpan.FromSeconds(5)),
                    Simulation.InjectPerSec(rate: 10, during: TimeSpan.FromSeconds(5)),
                    Simulation.InjectPerSec(rate: 15, during: TimeSpan.FromSeconds(10))
                );;

            NBomberRunner
                .RegisterScenarios(scenario)
                .Run();
        }
    }
}