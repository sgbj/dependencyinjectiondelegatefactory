# DependencyInjectionDelegateFactory

Dependency injection for delegates. Inspired by .NET 6 minimal APIs.

```c#
app.MapPost("/products", async (CreateProduct request, Mediator mediator) => await mediator.HandleAsync(request))

...

record Product(int Id, string Name, decimal Price);

record CreateProduct(string Name, decimal Price) : IRequest<IResult>
{
    public Task<Product> MapAsync() => Task.FromResult(new Product { Name = Name, Price = Price });

    public async Task<ValidationResult> ValidateAsync(ProductRepository repository)
    {
        var validationResult = new ValidationResult();

        if (string.IsNullOrWhiteSpace(Name))
        {
            validationResult.Add(nameof(Name), "Name is required.");
        }

        if (await repository.GetByNameAsync(Name) is not null)
        {
            validationResult.Add(nameof(Name), "Name already exists.");
        }

        if (Price < 0)
        {
            validationResult.Add(nameof(Price), "Invalid price");
        }

        return validationResult;
    }

    public async Task<IResult> HandleAsync(ProductRepository repository, Validator validator, Mapper mapper)
    {
        var validationResult = await validator.ValidateAsync(this);

        if (!validationResult.IsValid)
        {
            return Results.BadRequest(validationResult.Errors);
        }

        var product = await mapper.MapAsync<Product>(this);
        await repository.AddAsync(product);
        return Results.Ok(product);
    }
}

```
