using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.EntityFrameworkCore;
using Samples;
using Sgbj;
using System.Numerics;

//BenchmarkRunner.Run<Benchmarks>();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<TodoDbContext>(options => options.UseInMemoryDatabase("Todo"));
builder.Services
    .AddTransient<Mapper>()
    .AddTransient<Mediator>()
    .AddTransient<Validator>()
    .AddTransient<Calculator>();

var app = builder.Build();

app.MapGet("/", async (Mediator mediator) => await mediator.HandleAsync(new GetTodos()));
app.MapPost("/", async (CreateTodo request, Mediator mediator) => await mediator.HandleAsync(request))
    .AddFilter<ValidationExceptionFilter>();

app.Run();

class Todo
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public bool IsComplete { get; set; }
}

class TodoDbContext : DbContext
{
    public TodoDbContext(DbContextOptions<TodoDbContext> options) : base(options) { }

    public DbSet<Todo> Todos => Set<Todo>();
}

record GetTodos : IRequest<List<Todo>>
{
    public async Task<List<Todo>> HandleAsync(TodoDbContext db) => await db.Todos.ToListAsync();
}

record CreateTodo(string Name, int Age) : IRequest<Todo>
{
    public Task<Todo> MapAsync() => Task.FromResult(new Todo { Name = Name });

    public async Task<ValidationResult> ValidateAsync(TodoDbContext db)
    {
        var validationResult = new ValidationResult();

        if (string.IsNullOrWhiteSpace(Name))
        {
            validationResult.Add(nameof(Name), "Name is required.");
        }

        if (await db.Todos.AnyAsync(p => p.Name == Name))
        {
            validationResult.Add(nameof(Name), "Name already exists.");
        }

        return validationResult;
    }

    public async Task<Todo> HandleAsync(TodoDbContext db, Validator validator, Mapper mapper)
    {
        await validator.ValidateAndThrowAsync(this);
        var todo = await mapper.MapAsync<Todo>(this);
        db.Add(todo);
        await db.SaveChangesAsync();
        return todo;
    }
}

class Calculator
{
    public T Add<T>(T a, T b) where T : INumber<T> => a + b;
}

public class Benchmarks
{
    private readonly Func<int, int, int> _add;

    public Benchmarks()
    {
        var services = new ServiceCollection().AddSingleton<Calculator>().BuildServiceProvider();
        var add = DependencyInjectionDelegateFactory.Create((int a, int b, Calculator calculator) => calculator.Add(a, b), typeof(int), typeof(int));
        _add = (int a, int b) => (int)add(services, null, new object[] { a, b })!;
    }

    [Benchmark]
    public int Add() => _add(10, 20);
}
