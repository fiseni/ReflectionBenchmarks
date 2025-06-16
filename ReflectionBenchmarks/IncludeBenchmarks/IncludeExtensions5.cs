using System.Linq.Expressions;

namespace ReflectionBenchmarks;

public static class IncludeExtensions5
{
    public static IQueryable<T> IncludeCustom5<T, TProperty>(
        this IQueryable<T> source,
        Expression<Func<T, TProperty>> expr) where T : class
    {
        LambdaExpression lambda = expr;
        return EntityFrameworkQueryableExtensions.Include(source, (dynamic)lambda);
    }

    public static IQueryable<T> ThenIncludeCustom5<T, TPreviousProperty, TProperty>(
        this IQueryable<T> source,
        Expression<Func<TPreviousProperty, TProperty>> expr) where T : class
    {
        LambdaExpression lambda = expr;
        return EntityFrameworkQueryableExtensions.ThenInclude((dynamic)source, (dynamic)lambda);
    }

    public static IQueryable<T> ThenIncludeCustom5<T, TPreviousProperty, TProperty>(
        this IQueryable<T> source,
        Expression<Func<TPreviousProperty, IEnumerable<TProperty>>> expr) where T : class
    {
        LambdaExpression lambda = expr;
        return EntityFrameworkQueryableExtensions.ThenInclude((dynamic)source, (dynamic)lambda);
    }
}

