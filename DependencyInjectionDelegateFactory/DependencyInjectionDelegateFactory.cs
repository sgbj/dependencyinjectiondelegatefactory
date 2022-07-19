using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Sgbj;

public delegate object? DependencyInjectionDelegate(IServiceProvider serviceProvider, object? target, params object?[] args);

public static class DependencyInjectionDelegateFactory
{
    private static readonly MethodInfo GetServiceMethod = typeof(IServiceProvider).GetMethod(nameof(IServiceProvider.GetService));
    private static readonly ParameterExpression ServiceProviderExpr = Expression.Parameter(typeof(IServiceProvider));
    private static readonly ParameterExpression TargetExpr = Expression.Parameter(typeof(object));
    private static readonly ParameterExpression ArgsExpr = Expression.Parameter(typeof(object[]));

    public static DependencyInjectionDelegate Create(Delegate method, params Type[] argumentTypes) =>
        Create(method.Method, method.Target is null ? null : Expression.Convert(Expression.Constant(method.Target), method.Target.GetType()), argumentTypes);

    public static DependencyInjectionDelegate Create(MethodInfo method, params Type[] argumentTypes) =>
        Create(method, method.IsStatic ? null : Expression.Convert(TargetExpr, method.DeclaringType), argumentTypes);

    private static DependencyInjectionDelegate Create(MethodInfo method, Expression? target, Type[] argumentTypes)
    {
        var parameters = method.GetParameters();
        var arguments = new Expression[parameters.Length];

        for (int parameterPointer = 0, argumentPointer = 0; parameterPointer < parameters.Length; parameterPointer++)
        {
            var parameterType = parameters[parameterPointer].ParameterType;

            if (argumentTypes.Length > argumentPointer && parameterType.IsAssignableFrom(argumentTypes[argumentPointer]))
            {
                arguments[parameterPointer] = Expression.Convert(Expression.ArrayAccess(ArgsExpr, Expression.Constant(argumentPointer)), parameterType);
                argumentPointer++;
            }
            else
            {
                arguments[parameterPointer] = Expression.Convert(Expression.Call(ServiceProviderExpr, GetServiceMethod, Expression.Constant(parameterType)), parameterType);
            }
        }

        var methodCall = target is null ? Expression.Call(method, arguments) : Expression.Call(target, method, arguments);
        Expression body = method.ReturnType == typeof(void) ? Expression.Block(methodCall, Expression.Constant(null)) : Expression.Convert(methodCall, typeof(object));
        var lambda = Expression.Lambda<DependencyInjectionDelegate>(body, ServiceProviderExpr, TargetExpr, ArgsExpr);
        return lambda.Compile();
    }
}
