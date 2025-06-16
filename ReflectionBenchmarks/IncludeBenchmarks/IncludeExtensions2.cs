using Microsoft.EntityFrameworkCore.Query;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace ReflectionBenchmarks;

public static class IncludeExtensions2
{
    private static readonly MethodInfo _includeMethodInfo = typeof(EntityFrameworkQueryableExtensions)
        .GetTypeInfo().GetDeclaredMethods(nameof(EntityFrameworkQueryableExtensions.Include))
        .Single(mi => mi.IsPublic && mi.GetGenericArguments().Length == 2
            && mi.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(IQueryable<>)
            && mi.GetParameters()[1].ParameterType.GetGenericTypeDefinition() == typeof(Expression<>));

    private static readonly MethodInfo _thenIncludeAfterReferenceMethodInfo
        = typeof(EntityFrameworkQueryableExtensions)
            .GetTypeInfo().GetDeclaredMethods(nameof(EntityFrameworkQueryableExtensions.ThenInclude))
            .Single(mi => mi.IsPublic && mi.GetGenericArguments().Length == 3
                && mi.GetParameters()[0].ParameterType.GenericTypeArguments[1].IsGenericParameter
                && mi.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(IIncludableQueryable<,>)
                && mi.GetParameters()[1].ParameterType.GetGenericTypeDefinition() == typeof(Expression<>));

    private static readonly MethodInfo _thenIncludeAfterEnumerableMethodInfo
        = typeof(EntityFrameworkQueryableExtensions)
            .GetTypeInfo().GetDeclaredMethods(nameof(EntityFrameworkQueryableExtensions.ThenInclude))
            .Single(mi => mi.IsPublic && mi.GetGenericArguments().Length == 3
                && !mi.GetParameters()[0].ParameterType.GenericTypeArguments[1].IsGenericParameter
                && mi.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(IIncludableQueryable<,>)
                && mi.GetParameters()[1].ParameterType.GetGenericTypeDefinition() == typeof(Expression<>));

    private readonly record struct CacheKey(int IncludeType, Type EntityType, Type PropertyType, Type? PreviousReturnType);
    private static readonly ConcurrentDictionary<CacheKey, Func<IQueryable, LambdaExpression, IQueryable>> _cache = new();


    public static IQueryable<T> IncludeCustom2<T, TProperty>(
        this IQueryable<T> source,
        Expression<Func<T, TProperty>> expr) where T : class
    {
        var key = new CacheKey(1, typeof(T), expr.ReturnType, null);
        var include = _cache.GetOrAdd(key, CreateIncludeDelegate);
        source = (IQueryable<T>)include(source, expr);

        return source;
    }

    public static IQueryable<T> ThenIncludeCustom2<T, TPreviousProperty, TProperty>(
        this IQueryable<T> source,
        Expression<Func<TPreviousProperty, TProperty>> expr,
        Type previousReturnType) where T : class
    {
        var key = new CacheKey(2, typeof(T), expr.ReturnType, previousReturnType);
        var include = _cache.GetOrAdd(key, CreateThenIncludeDelegate);
        source = (IQueryable<T>)include(source, expr);

        return source;
    }

    public static IQueryable<T> ThenIncludeCustom2<T, TPreviousProperty, TProperty>(
        this IQueryable<T> source,
        Expression<Func<TPreviousProperty, IEnumerable<TProperty>>> expr,
        Type previousReturnType) where T : class
    {
        var key = new CacheKey(3, typeof(T), expr.ReturnType, previousReturnType);
        var include = _cache.GetOrAdd(key, CreateThenIncludeDelegate);
        source = (IQueryable<T>)include(source, expr);

        return source;
    }

    private static Func<IQueryable, LambdaExpression, IQueryable> CreateIncludeDelegate(CacheKey cacheKey)
    {
        var includeMethod = _includeMethodInfo.MakeGenericMethod(cacheKey.EntityType, cacheKey.PropertyType);
        var sourceParameter = Expression.Parameter(typeof(IQueryable));
        var selectorParameter = Expression.Parameter(typeof(LambdaExpression));

        var call = Expression.Call(
              includeMethod,
              Expression.Convert(sourceParameter, typeof(IQueryable<>).MakeGenericType(cacheKey.EntityType)),
              Expression.Convert(selectorParameter, typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(cacheKey.EntityType, cacheKey.PropertyType))));

        var lambda = Expression.Lambda<Func<IQueryable, LambdaExpression, IQueryable>>(call, sourceParameter, selectorParameter);
        return lambda.Compile();
    }

    private static Func<IQueryable, LambdaExpression, IQueryable> CreateThenIncludeDelegate(CacheKey cacheKey)
    {
        Debug.Assert(cacheKey.PreviousReturnType is not null);

        var (thenIncludeMethod, previousPropertyType) = cacheKey.IncludeType == 2
            ? (_thenIncludeAfterReferenceMethodInfo, cacheKey.PreviousReturnType)
            : (_thenIncludeAfterEnumerableMethodInfo, cacheKey.PreviousReturnType.GenericTypeArguments[0]);

        var thenIncludeMethodGeneric = thenIncludeMethod.MakeGenericMethod(cacheKey.EntityType, previousPropertyType, cacheKey.PropertyType);
        var sourceParameter = Expression.Parameter(typeof(IQueryable));
        var selectorParameter = Expression.Parameter(typeof(LambdaExpression));

        var call = Expression.Call(
                thenIncludeMethodGeneric,
                // We must pass cacheKey.PreviousReturnType. It must be exact type, not the generic type argument
                Expression.Convert(sourceParameter, typeof(IIncludableQueryable<,>).MakeGenericType(cacheKey.EntityType, cacheKey.PreviousReturnType)),
                Expression.Convert(selectorParameter, typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(previousPropertyType, cacheKey.PropertyType))));

        var lambda = Expression.Lambda<Func<IQueryable, LambdaExpression, IQueryable>>(call, sourceParameter, selectorParameter);
        return lambda.Compile();
    }
}

