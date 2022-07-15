using System.Text;
using NBomber.CSharp;
using NBomber.Plugins.Http.CSharp;
using Newtonsoft.Json;

namespace Troolio.Deployment.LoadTest
{
    public class ListTitle
    {
        public string? Title { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var serverIp = "127.0.0.1";
            var serverPath = "http://" + serverIp + ":8081";

            var deviceId = "B8118E16-3530-4B65-94B9-CF38D2E5F749";

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
                        .WithBody(new StringContent(JsonConvert.SerializeObject(new ListTitle { Title = $"testlist-{listId}" }), Encoding.UTF8, "application/json"));

                    context.Logger.Debug($"Creating new list: {requestPath}");
                    var response = await Http.Send(request, context);
                    return response;
                }, TimeSpan.FromSeconds(30));

            var scenario = ScenarioBuilder
                .CreateScenario("simple_http", step)
                .WithWarmUpDuration(TimeSpan.FromSeconds(5))
                .WithLoadSimulations(
                    Simulation.InjectPerSec(rate: 1, during: TimeSpan.FromSeconds(5)),
                    Simulation.InjectPerSec(rate: 5, during: TimeSpan.FromSeconds(5)),
                    Simulation.InjectPerSec(rate: 10, during: TimeSpan.FromSeconds(5)),
                    Simulation.InjectPerSec(rate: 15, during: TimeSpan.FromSeconds(10)),
                    Simulation.InjectPerSec(rate: 30, during: TimeSpan.FromSeconds(10)),
                    Simulation.InjectPerSec(rate: 60, during: TimeSpan.FromSeconds(10)),
                    Simulation.InjectPerSec(rate: 120, during: TimeSpan.FromSeconds(10)),
                    Simulation.InjectPerSec(rate: 240, during: TimeSpan.FromSeconds(10))
                );;

            NBomberRunner
                .RegisterScenarios(scenario)
                .Run();
        }
    }
}