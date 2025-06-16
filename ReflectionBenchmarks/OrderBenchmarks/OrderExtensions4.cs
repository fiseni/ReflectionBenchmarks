using System.Linq.Expressions;
using System.Reflection;

namespace ReflectionBenchmarks;

public static class OrderExtensions4
{
    // The whole point of Expression<Func<T, object?>> in the previous versions was that we can't store TKey.
    // What if define TKey in the method but store as LambdaExpression.
    // Then we won't need to recreate new Lambda.

    private static readonly MethodInfo _orderByMethod = typeof(Queryable)
        .GetTypeInfo()
        .GetDeclaredMethods(nameof(Queryable.OrderBy))
        .Single(m => m.GetParameters().Length == 2);

    private static readonly MethodInfo _thenByMethod = typeof(Queryable)
        .GetTypeInfo()
        .GetDeclaredMethods(nameof(Queryable.ThenBy))
        .Single(m => m.GetParameters().Length == 2);

    public static IQueryable<T> OrderByCustom4<T, TKey>(
        this IQueryable<T> source,
        Expression<Func<T, TKey?>> keySelector)
    {
        LambdaExpression expr = keySelector;
        var mi = _orderByMethod.MakeGenericMethod(typeof(T), expr.ReturnType);
        var result = (IQueryable<T>)mi.Invoke(null, [source, expr])!;
        return result;
    }

    public static IQueryable<T> ThenByCustom4<T, TKey>(
        this IQueryable<T> source,
        Expression<Func<T, TKey?>> keySelector)
    {
        LambdaExpression expr = keySelector;
        var mi = _thenByMethod.MakeGenericMethod(typeof(T), expr.ReturnType);
        var result = (IQueryable<T>)mi.Invoke(null, [source, expr])!;
        return result;
    }
}
