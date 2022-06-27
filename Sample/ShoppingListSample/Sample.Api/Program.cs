using Sample.Api;
using Sample.Shared.ActorInterfaces;
using Troolio.Core;
using Troolio.Core.Client;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

IConfigurationBuilder cb = new ConfigurationBuilder();
cb.AddUserSecrets(typeof(IAllShoppingListsActor).Assembly);

builder.Services.AddSingleton<ITroolioClient>(
    new TroolioClient(new[] { typeof(IAllShoppingListsActor).Assembly }, "Shopping", cb));

builder.Logging.AddConsole();

var app = builder.Build();

// get the client
var client = app.Services.GetRequiredService<ITroolioClient>();

// Start the tracing but don't await
ApiTracing apiTracing = new ApiTracing();

// Enable tracing
if (client is not null)
{
    await apiTracing.EnableTracing(client);
}

apiTracing.StartTracingToFile("test.trace");

// Configure the HTTP request pipeline.
if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
