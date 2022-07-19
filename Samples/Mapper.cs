using Sgbj;
using System.Collections.Concurrent;

namespace Samples;

public class Mapper
{
    private static readonly ConcurrentDictionary<Type, DependencyInjectionDelegate> _mappers = new();

    private readonly IServiceProvider _serviceProvider;

    public Mapper(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task<T> MapAsync<T>(object value)
    {
        var type = value.GetType();

        if (!_mappers.TryGetValue(type, out var mapper))
        {
            var methodInfo = type.GetMethod("MapAsync");

            if (methodInfo is null)
            {
                throw new InvalidOperationException($"MapAsync method not found for '{type}'.");
            }

            mapper = DependencyInjectionDelegateFactory.Create(methodInfo);
            _mappers[type] = mapper;
        }

        return (Task<T>)mapper(_serviceProvider, value)!;
    }
}
