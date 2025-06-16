using System.Linq.Expressions;
using System.Reflection;

namespace ReflectionBenchmarks;

public static class OrderExtensions2
{
    // We're accepting Expression<Func<T, object?>> as the key selector
    // The compiler does the conversion from Expression<Func<T, TKey>> while accepting as an argument.
    // We'll reconstruct the original expression and call the appropriate OrderBy/ThenBy methods by reflection.

    private static readonly MethodInfo _orderByMethod = typeof(Queryable)
        .GetTypeInfo()
        .GetDeclaredMethods(nameof(Queryable.OrderBy))
        .Single(m => m.GetParameters().Length == 2);

    private static readonly MethodInfo _thenByMethod = typeof(Queryable)
        .GetTypeInfo()
        .GetDeclaredMethods(nameof(Queryable.ThenBy))
        .Single(m => m.GetParameters().Length == 2);

    public static IQueryable<T> OrderByCustom2<T>(
        this IQueryable<T> source,
        Expression<Func<T, object?>> keySelector)
    {
        var expr = RemoveConvert(keySelector);
        var mi = _orderByMethod.MakeGenericMethod(typeof(T), expr.ReturnType);
        var result = (IQueryable<T>)mi.Invoke(null, [source, expr])!;
        return result;
    }

    public static IQueryable<T> ThenByCustom2<T>(
        this IQueryable<T> source,
        Expression<Func<T, object?>> keySelector)
    {
        var expr = RemoveConvert(keySelector);
        var mi = _thenByMethod.MakeGenericMethod(typeof(T), expr.ReturnType);
        var result = (IQueryable<T>)mi.Invoke(null, [source, expr])!;
        return result;
    }

    private static LambdaExpression RemoveConvert(LambdaExpression source)
    {
        var body = source.Body;

        while (body is UnaryExpression expr &&
            (expr.NodeType == ExpressionType.Convert || expr.NodeType == ExpressionType.ConvertChecked))
        {
            body = expr.Operand;
        }

        return Expression.Lambda(body, source.Parameters);
    }
}
