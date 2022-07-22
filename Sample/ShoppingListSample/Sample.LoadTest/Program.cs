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

    public class ListItem
    {
        public string? Description { get; set; }
        public int Quantity { get; set; } = 1;
    }

    class Program
    {
        static void Main(string[] args)
        {
            var serverIp = "127.0.0.1";
            var serverPath = "http://" + serverIp + ":8081";

            var deviceId = "B8118E16-3530-4B65-94B9-CF38D2E5F749";

            var http = HttpClientFactory.Create();

            var step = Step.Create("create_list",
                clientFactory: http,
                execute: async context =>
                {
                    var listId = Guid.NewGuid().ToString();
                    var userId = Guid.NewGuid().ToString();
                    var listPrefix = "/ShoppingList/" + listId;

                    var requestPath = serverPath + listPrefix + "/CreateNewList";
                    var request = Http.CreateRequest("POST", requestPath)
                        .WithHeader("Accept", "*/*")
                        .WithHeader("userId", userId)
                        .WithHeader("deviceId", deviceId)
                        .WithBody(new StringContent(JsonConvert.SerializeObject(new ListTitle { Title = $"testlist-{listId}" }), Encoding.UTF8, "application/json"));

                    context.Data.Add("listId", listId);
                    context.Data.Add("userId", userId);

                    context.Logger.Debug($"Creating new list: {requestPath}");
                    var response = await Http.Send(request, context);
                    return response;
                }, TimeSpan.FromSeconds(30));

            var step2 = Step.Create("add_first_item_to_list",
                clientFactory: http,
                execute: async context =>
                {
                    var listId = Guid.Parse((string)context.Data["listId"]);
                    var userId = Guid.Parse((string)context.Data["userId"]);
                    var listPrefix = $"/ShoppingList/{listId}";

                    var requestPath = serverPath + listPrefix + "/AddItemToList";
                    var request = Http.CreateRequest("POST", requestPath)
                        .WithHeader("Accept", "*/*")
                        .WithHeader("userId", userId.ToString())
                        .WithHeader("deviceId", deviceId)
                        .WithBody(new StringContent(JsonConvert.SerializeObject(new ListItem { Description = $"Item 1", Quantity = 1 }), Encoding.UTF8, "application/json"));


                    context.Logger.Debug($"Creating item in list: {requestPath}");
                    var response = await Http.Send(request, context);
                    return response;
                }, TimeSpan.FromSeconds(30));

            var step3 = Step.Create("add_second_item_to_list",
                clientFactory: http,
                execute: async context =>
                {
                    var listId = Guid.Parse((string)context.Data["listId"]);
                    var userId = Guid.Parse((string)context.Data["userId"]);
                    var listPrefix = $"/ShoppingList/{listId}";

                    var requestPath = serverPath + listPrefix + "/AddItemToList";
                    var request = Http.CreateRequest("POST", requestPath)
                        .WithHeader("Accept", "*/*")
                        .WithHeader("userId", userId.ToString())
                        .WithHeader("deviceId", deviceId)
                        .WithBody(new StringContent(JsonConvert.SerializeObject(new ListItem { Description = $"Item 2", Quantity = 1 }), Encoding.UTF8, "application/json"));

                    context.Logger.Debug($"Creating item in list: {requestPath}");
                    var response = await Http.Send(request, context);
                    return response;
                }, TimeSpan.FromSeconds(30));

            var scenario = ScenarioBuilder
                .CreateScenario("simple_http", step, step2, step3)
                .WithWarmUpDuration(TimeSpan.FromSeconds(5))
                .WithLoadSimulations(
                    Simulation.InjectPerSec(rate: 1, during: TimeSpan.FromSeconds(5)),
                    Simulation.InjectPerSec(rate: 5, during: TimeSpan.FromSeconds(5)),
                    Simulation.InjectPerSec(rate: 10, during: TimeSpan.FromSeconds(5)),
                    Simulation.InjectPerSec(rate: 15, during: TimeSpan.FromSeconds(10)),
                    Simulation.InjectPerSec(rate: 30, during: TimeSpan.FromSeconds(10)),
                    Simulation.InjectPerSec(rate: 60, during: TimeSpan.FromSeconds(10)),
                    Simulation.InjectPerSec(rate: 120, during: TimeSpan.FromSeconds(10)),
                    Simulation.InjectPerSec(rate: 240, during: TimeSpan.FromSeconds(10)),
                    Simulation.InjectPerSec(rate: 480, during: TimeSpan.FromSeconds(10))
                );;

            NBomberRunner
                .RegisterScenarios(scenario)
                .Run();
        }
    }
}