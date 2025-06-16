using Microsoft.EntityFrameworkCore.Query;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace ReflectionBenchmarks;

public static class IncludeExtensions4
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
    private static readonly ConcurrentDictionary<CacheKey, Delegate> _cache = new();

    public static IQueryable<T> IncludeCustom4<T, TProperty>(
        this IQueryable<T> source,
        Expression<Func<T, TProperty>> expr) where T : class
    {
        var key = new CacheKey(1, typeof(T), expr.ReturnType, null);
        var del = (Func<IQueryable<T>, Expression<Func<T, TProperty>>, IQueryable<T>>)_cache.GetOrAdd(key, _ =>
            Delegate.CreateDelegate(
                typeof(Func<IQueryable<T>, Expression<Func<T, TProperty>>, IQueryable<T>>),
                _includeMethodInfo.MakeGenericMethod(typeof(T), expr.ReturnType)));
        return del(source, expr);
    }

    public static IQueryable<T> ThenIncludeCustom4<T, TPreviousProperty, TProperty>(
        this IQueryable<T> source,
        Expression<Func<TPreviousProperty, TProperty>> expr) where T : class
    {
        var key = new CacheKey(2, typeof(T), typeof(TProperty), typeof(TPreviousProperty));
        var del = (Func<IIncludableQueryable<T, TPreviousProperty>, Expression<Func<TPreviousProperty, TProperty>>, IIncludableQueryable<T, TProperty>>)_cache.GetOrAdd(key, _ =>
            Delegate.CreateDelegate(
                typeof(Func<IIncludableQueryable<T, TPreviousProperty>, Expression<Func<TPreviousProperty, TProperty>>, IIncludableQueryable<T, TProperty>>),
                _thenIncludeAfterReferenceMethodInfo.MakeGenericMethod(typeof(T), typeof(TPreviousProperty), typeof(TProperty))));
        return (IQueryable<T>)del((IIncludableQueryable<T, TPreviousProperty>)source, expr);
    }

    public static IQueryable<T> ThenIncludeCustom4<T, TPreviousProperty, TProperty>(
        this IQueryable<T> source,
        Expression<Func<TPreviousProperty, IEnumerable<TProperty>>> expr) where T : class
    {
        var key = new CacheKey(3, typeof(T), typeof(TProperty), typeof(TPreviousProperty));
        var del = (Func<IIncludableQueryable<T, TPreviousProperty>, Expression<Func<TPreviousProperty, IEnumerable<TProperty>>>, IIncludableQueryable<T, TProperty>>)_cache.GetOrAdd(key, _ =>
            Delegate.CreateDelegate(
                typeof(Func<IIncludableQueryable<T, TPreviousProperty>, Expression<Func<TPreviousProperty, IEnumerable<TProperty>>>, IIncludableQueryable<T, TProperty>>),
                _thenIncludeAfterEnumerableMethodInfo.MakeGenericMethod(typeof(T), typeof(TPreviousProperty), typeof(TProperty))));
        return (IQueryable<T>)del((IIncludableQueryable<T, TPreviousProperty>)source, expr);
    }
}
