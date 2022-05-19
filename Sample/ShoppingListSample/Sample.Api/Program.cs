using Troolio.Core.Client;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

IConfigurationBuilder cb = new ConfigurationBuilder();
cb.AddUserSecrets(typeof(Sample.Shared.ShoppingList.IAllShoppingListsActor).Assembly);

builder.Services.AddSingleton<ITroolioClient>(
    new TroolioClient(new[] { typeof(Sample.Shared.ShoppingList.IAllShoppingListsActor).Assembly }, "Shopping", cb));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
