using ShoppingList.Api;
using ShoppingList.Shared.ActorInterfaces;
using Troolio.Core.Client;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

IConfigurationBuilder cb = new ConfigurationBuilder();
cb.AddUserSecrets(typeof(IAllShoppingListsActor).Assembly);

builder.Services
    .AddSingleton<ITroolioClient>(
        new TroolioClient(new[] { typeof(IAllShoppingListsActor).Assembly }, "Shopping", cb))
    .AddSingleton<ApiTracing>();

builder.Logging.AddConsole();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseCors((cpb) =>
      cpb.AllowAnyOrigin()
          .AllowAnyHeader()
          .AllowAnyMethod()
  );
}

app.UseAuthorization();

app.MapControllers();

app.Run();
