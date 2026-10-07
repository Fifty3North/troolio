using Microsoft.EntityFrameworkCore;
using Troolio.Core;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<TodoReadModelDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("ReadModels") ?? "Data Source=todo-readmodels.db"));
builder.Services.AddSingleton<DemoIdentity>();
builder.Services.AddSingleton<TodoRuntime>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<TodoRuntime>());
var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();
using (var scope = app.Services.CreateScope())
    await scope.ServiceProvider.GetRequiredService<TodoReadModelDbContext>().Database.EnsureCreatedAsync();

app.MapPost("/todos", async (CreateTodoRequest request, HttpContext http, TodoRuntime runtime, DemoIdentity identity) =>
{
    if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Trim().Length > 200 || request.Id == Guid.Empty)
        return Results.BadRequest(new { error = "Title must contain 1–200 characters; an optional ID must be nonempty." });
    var id = request.Id ?? Guid.NewGuid();
    return await ApiErrors.Run(async () =>
    {
        await runtime.Tell(id, new CreateTodoCommand(IngressHeaders(http, identity), id, request.Title.Trim()));
        return Results.Created($"/todos/{id}", new { id, title = request.Title.Trim() });
    });
});
app.MapPost("/todos/{id:guid}/complete", async (Guid id, HttpContext http, TodoRuntime runtime, DemoIdentity identity) =>
    await ApiErrors.Run(async () =>
    {
        await runtime.Tell(id, new CompleteTodoCommand(IngressHeaders(http, identity)));
        return Results.Accepted($"/todos/{id}");
    }));
app.MapGet("/todos/{id:guid}", async (Guid id, TodoReadModelDbContext db, DemoIdentity identity) =>
{
    var todo = await db.Todos.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.OwnerId == identity.UserId);
    return todo is null ? Results.NotFound() : Results.Ok(todo);
});
app.MapGet("/todos", async (TodoReadModelDbContext db, DemoIdentity identity, int skip = 0, int take = 50) =>
{
    if (skip < 0 || take is < 1 or > 100)
        return Results.BadRequest(new { error = "skip must be nonnegative; take must be 1–100." });
    var todos = await db.Todos.AsNoTracking().Where(x => x.OwnerId == identity.UserId)
        .OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id).Skip(skip).Take(take).ToListAsync();
    return Results.Ok(todos);
});
app.MapGet("/health", () => Results.Ok(new { status = "ready" }));
app.Run();

static Metadata IngressHeaders(HttpContext http, DemoIdentity identity)
{
    var correlationId = Guid.TryParse(http.Request.Headers["X-Correlation-Id"], out var value) && value != Guid.Empty
        ? value : Guid.NewGuid();
    http.Response.Headers["X-Correlation-Id"] = correlationId.ToString();
    return new Metadata(correlationId, identity.UserId, Guid.Empty);
}

public sealed record CreateTodoRequest(string? Title, Guid? Id = null);
public sealed class DemoIdentity(IConfiguration configuration)
{
    // A single trusted server-configured demonstration user; replace with verified authentication in production.
    public Guid UserId { get; } = Guid.Parse(configuration["Todo:DemoUserId"] ?? "11111111-1111-1111-1111-111111111111");
}
public static class ApiErrors
{
    public static async Task<IResult> Run(Func<Task<IResult>> action)
    {
        try { return await action(); }
        catch (KeyNotFoundException) { return Results.NotFound(); }
        catch (UnauthorizedAccessException) { return Results.StatusCode(StatusCodes.Status403Forbidden); }
        catch (ArgumentException e) { return Results.BadRequest(new { error = e.Message }); }
        catch (InvalidOperationException e) { return Results.Conflict(new { error = e.Message }); }
    }
}
public partial class Program;
