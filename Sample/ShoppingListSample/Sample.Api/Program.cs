using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Sample.Api;
using Sample.Host.App.ShoppingList;
using Sample.Host.App;
using Microsoft.Extensions.Hosting;
using Troolio.Core;
using Troolio.Core.Serialization;
using Troolio.Stores;
using Troolio.MessageQueue;
using Sample.Database.Model;
using Sample.Shared.ActorInterfaces;
using Troolio.Core.Client;

var builder = WebApplication.CreateBuilder(args);
if (!builder.Environment.IsDevelopment() && !builder.Environment.IsEnvironment("Testing"))
    throw new InvalidOperationException("Demo authentication is local only. Configure a real identity provider before deployment.");
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAuthentication("Demo").AddScheme<AuthenticationSchemeOptions, DemoAuthenticationHandler>("Demo", _ => { });
builder.Services.AddAuthorization(options => options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy.WithOrigins("http://localhost:5173", "http://localhost:8080").AllowAnyHeader().AllowAnyMethod().WithExposedHeaders("correlationId")));
builder.Services.AddDbContext<ShoppingListsDbContext>(options => options.UseSqlite(ShoppingListsDbContext.ConnectionString(builder.Configuration)));
builder.Services.AddSingleton<ITroolioClient>(_ => new TroolioClient([typeof(IAllShoppingListsActor).Assembly], "Shopping"));
builder.Services.AddSingleton<ApiTracing>();
var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true && context.Request.Headers.TryGetValue("userId", out var supplied)
        && (!Guid.TryParse(supplied, out var id) || id.ToString() != context.User.FindFirstValue(ClaimTypes.NameIdentifier)))
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return;
    }
    await next();
});
app.MapGet("/demo/users", () => DemoUsers.All).AllowAnonymous();
app.MapControllers();
IHost? silo = null;
if (builder.Configuration.GetValue<bool>("Shopping:EmbeddedHost"))
{
    silo = Host.CreateDefaultBuilder(args).TroolioServer("Shopping",
        [typeof(IAllShoppingListsActor).Assembly, typeof(ShoppingListActor).Assembly], services =>
        {
            services.AddDbContext<ShoppingListsDbContext>(options => options.UseSqlite(ShoppingListsDbContext.ConnectionString(builder.Configuration)));
            services.AddSingleton<IMessageQueueProvider, InMemoryMessageQueueProvider>();
            services.AddSingleton<IStore>(provider => new FileSystemStore(provider.GetRequiredService<IEventSerializer>(), builder.Configuration["Shopping:EventFolder"] ?? "troolio-samples-shopping"));
        }, builder => builder.ConfigureServices(services => services.AddShoppingReadModels()));
    using (var scope = silo.Services.CreateScope())
        await scope.ServiceProvider.GetRequiredService<ShoppingListsDbContext>().Database.EnsureCreatedAsync();
    await silo.StartAsync();
}
app.Lifetime.ApplicationStopping.Register(() => silo?.StopAsync().GetAwaiter().GetResult());
try { app.Run(); } finally { silo?.Dispose(); }
public partial class Program;
