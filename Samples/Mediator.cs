using Sgbj;
using System.Collections.Concurrent;

namespace Samples;

public interface IRequest<T>
{
}

public class Mediator
{
    private static readonly ConcurrentDictionary<Type, DependencyInjectionDelegate> _handlers = new();

    private readonly IServiceProvider _serviceProvider;
    
    public Mediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task<T> HandleAsync<T>(IRequest<T> request)
    {
        var type = request.GetType();

        if (!_handlers.TryGetValue(type, out var handler))
        {
            var methodInfo = type.GetMethod("HandleAsync");

            if (methodInfo is null)
            {
                throw new InvalidOperationException($"HandleAsync method not found for '{type}'.");
            }

            handler = DependencyInjectionDelegateFactory.Create(methodInfo);
            _handlers[type] = handler;
        }

        return (Task<T>)handler(_serviceProvider, request)!;
    }
}
