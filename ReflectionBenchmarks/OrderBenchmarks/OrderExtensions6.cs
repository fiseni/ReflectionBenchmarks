using System.Linq.Expressions;

namespace ReflectionBenchmarks;

public static class OrderExtensions6
{
    // What if use dynamic?

    public static IQueryable<T> OrderByCustom6<T, TKey>(
        this IQueryable<T> source,
        Expression<Func<T, TKey?>> keySelector)
    {
        LambdaExpression expr = keySelector;
        return Queryable.OrderBy(source, (dynamic)expr);
    }

    public static IQueryable<T> ThenByCustom6<T, TKey>(
        this IQueryable<T> source,
        Expression<Func<T, TKey?>> keySelector)
    {
        LambdaExpression expr = keySelector;
        var orderedQueryable = source as IOrderedQueryable<T>;
        return Queryable.ThenBy(orderedQueryable!, (dynamic)expr);
    }
}
