using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
_ = builder.Services
    .AddDatabaseDeveloperPageExceptionFilter()
    .AddDbContext<TodoDbContext>(opt => opt.UseInMemoryDatabase("TodoList"))
    .AddAWSLambdaHosting(LambdaEventSource.HttpApi);

var app = builder.Build();

app.UseHttpsRedirection();

app.MapFallback(() => Results.Redirect("/todos"));

app.MapGet("/todos", async (TodoDbContext db) =>
    await db.Todos.ToListAsync());

app.MapGet("/todos/complete", async (TodoDbContext db) =>
    await db.Todos
        .Where(t => t.IsComplete)
        .ToListAsync());

app.MapGet("/todos/{id}", async (int id, TodoDbContext db) =>
    await db.Todos.FindAsync(id)
        is Todo todo
            ? Results.Ok(todo)
            : Results.NotFound());

app.MapPost("/todos", async (Todo todo, TodoDbContext db) =>
{
    db.Todos.Add(todo);
    await db.SaveChangesAsync();

    return Results.Created($"/todos/{todo.Id}", todo);
});

app.MapPut("/todos/{id}", async (int id, Todo inputTodo, TodoDbContext db) =>
{
    var todo = await db.Todos.FindAsync(id);

    if (todo is null)
        return Results.NotFound();

    todo.Title = inputTodo.Title;
    todo.IsComplete = inputTodo.IsComplete;

    await db.SaveChangesAsync();

    return Results.NoContent();
});

app.MapDelete("/todos/{id}", async (int id, TodoDbContext db) =>
{
    if (await db.Todos.FindAsync(id) is Todo todo)
    {
        db.Todos.Remove(todo);
        await db.SaveChangesAsync();
        return Results.Ok(todo);
    }

    return Results.NotFound();
});

app.Run();

public class Todo
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public bool IsComplete { get; set; }
}

public class TodoDbContext : DbContext
{
    public TodoDbContext(DbContextOptions<TodoDbContext> options)
        : base(options) { }

    public DbSet<Todo> Todos => Set<Todo>();
}
