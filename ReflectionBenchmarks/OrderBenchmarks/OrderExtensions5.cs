using System.Linq.Expressions;
using System.Reflection;
using System.Collections.Concurrent;

namespace ReflectionBenchmarks;

public static class OrderExtensions5
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

    private readonly record struct CacheKey(Type EntityType, Type KeyType);
    private static readonly ConcurrentDictionary<CacheKey, Func<IQueryable, LambdaExpression, IQueryable>> _cachedDelegates = new();

    public static IQueryable<T> OrderByCustom5<T, TKey>(
        this IQueryable<T> source,
        Expression<Func<T, TKey?>> keySelector)
    {
        LambdaExpression expr = keySelector;
        var cacheKey = new CacheKey(typeof(T), expr.ReturnType);
        var del = _cachedDelegates.GetOrAdd(cacheKey, CreateOrderByDelegate);
        return (IQueryable<T>)del(source, expr);
    }

    public static IQueryable<T> ThenByCustom5<T, TKey>(
        this IQueryable<T> source,
        Expression<Func<T, TKey?>> keySelector)
    {
        LambdaExpression expr = keySelector;
        var cacheKey = new CacheKey(typeof(T), expr.ReturnType);
        var del = _cachedDelegates.GetOrAdd(cacheKey, CreateThenByDelegate);
        return (IQueryable<T>)del(source, expr);
    }

    private static Func<IQueryable, LambdaExpression, IQueryable> CreateOrderByDelegate(CacheKey cacheKey)
    {
        var method = _orderByMethod.MakeGenericMethod(cacheKey.EntityType, cacheKey.KeyType);
        var sourceParam = Expression.Parameter(typeof(IQueryable));
        var exprParam = Expression.Parameter(typeof(LambdaExpression));
        var call = Expression.Call(
            method,
            Expression.Convert(sourceParam, typeof(IQueryable<>).MakeGenericType(cacheKey.EntityType)),
            Expression.Convert(exprParam, typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(cacheKey.EntityType, cacheKey.KeyType)))
        );
        var lambda = Expression.Lambda<Func<IQueryable, LambdaExpression, IQueryable>>(call, sourceParam, exprParam);
        return lambda.Compile();
    }

    private static Func<IQueryable, LambdaExpression, IQueryable> CreateThenByDelegate(CacheKey cacheKey)
    {
        var method = _thenByMethod.MakeGenericMethod(cacheKey.EntityType, cacheKey.KeyType);
        var sourceParam = Expression.Parameter(typeof(IQueryable));
        var exprParam = Expression.Parameter(typeof(LambdaExpression));
        var call = Expression.Call(
            method,
            Expression.Convert(sourceParam, typeof(IOrderedQueryable<>).MakeGenericType(cacheKey.EntityType)),
            Expression.Convert(exprParam, typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(cacheKey.EntityType, cacheKey.KeyType)))
        );
        var lambda = Expression.Lambda<Func<IQueryable, LambdaExpression, IQueryable>>(call, sourceParam, exprParam);
        return lambda.Compile();
    }
}
