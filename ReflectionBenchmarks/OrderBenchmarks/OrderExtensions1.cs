using System.Linq.Expressions;

namespace ReflectionBenchmarks;

public static class OrderExtensions1
{
    // We're accepting Expression<Func<T, object?>> as the key selector
    // The compiler does the conversion from Expression<Func<T, TKey>> while accepting as an argument.
    // We're measuring the conversion and whether the extension methods work with object keys.

    public static IQueryable<T> OrderByCustom1<T>(
        this IQueryable<T> source,
        Expression<Func<T, object?>> keySelector)
    {
        return source.OrderBy(keySelector);
    }

    public static IQueryable<T> ThenByCustom1<T>(
        this IQueryable<T> source,
        Expression<Func<T, object?>> keySelector)
    {
        var orderedQueryable = source as IOrderedQueryable<T>;
        return orderedQueryable!.ThenBy(keySelector);
    }
}
