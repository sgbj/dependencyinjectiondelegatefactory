using Sgbj;
using System.Collections.Concurrent;

namespace Samples;

public class ValidationResult
{
    public bool IsValid => !Errors.Any();
    public Dictionary<string, List<string>> Errors { get; } = new();

    public void Add(string key, string error)
    {
        if (!Errors.TryGetValue(key, out var errors))
        {
            errors = new();
            Errors.Add(key, errors);
        }
        errors.Add(error);
    }
}

public class ValidationException : Exception
{
    public ValidationException(ValidationResult validationResult)
        : base($"Validation failed:{string.Join("", validationResult.Errors.Select(e => $"{Environment.NewLine}-- {e.Key}: {e.Value}"))}")
    {
        ValidationResult = validationResult;
    }

    public ValidationResult ValidationResult { get; set; }
}

public class ValidationExceptionFilter : IRouteHandlerFilter
{
    public async ValueTask<object?> InvokeAsync(RouteHandlerInvocationContext context, RouteHandlerFilterDelegate next)
    {
        try
        {
            return await next(context);
        }
        catch (ValidationException ex)
        {
            return Results.BadRequest(ex.ValidationResult.Errors);
        }
    }
}

public class Validator
{
    private static readonly ConcurrentDictionary<Type, DependencyInjectionDelegate> _validators = new();

    private readonly IServiceProvider _serviceProvider;

    public Validator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task<ValidationResult> ValidateAsync(object value)
    {
        var type = value.GetType();

        if (!_validators.TryGetValue(type, out var validator))
        {
            var methodInfo = type.GetMethod("ValidateAsync");

            if (methodInfo is null)
            {
                throw new InvalidOperationException($"ValidateAsync method not found for type '{type}'.");
            }

            validator = DependencyInjectionDelegateFactory.Create(methodInfo);
            _validators[type] = validator;
        }

        return (Task<ValidationResult>)validator(_serviceProvider, value)!;
    }

    public async Task ValidateAndThrowAsync(object value)
    {
        var validationResult = await ValidateAsync(value);

        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult);
        }
    }
}
