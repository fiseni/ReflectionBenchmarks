using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ReflectionBenchmarks;

public struct LikeDto<T>
{
    public Expression<Func<T, string?>> KeySelector;
    public string Pattern;
    public int Group;
    public LikeDto(Expression<Func<T, string?>> keySelector, string pattern, int group)
    {
        KeySelector = keySelector;
        Pattern = pattern;
        Group = group;
    }
}

public static class LikeExtension1
{
    private static readonly MethodInfo _likeMethodInfo = typeof(DbFunctionsExtensions)
        .GetMethod(nameof(DbFunctionsExtensions.Like), [typeof(DbFunctions), typeof(string), typeof(string)])!;

    private static readonly MemberExpression _functions = Expression.Property(null, typeof(EF).GetProperty(nameof(EF.Functions))!);

    // It's required so EF can generate parameterized query.
    // In the past I've been creating closures for this, e.g. var patternAsExpression = ((Expression<Func<string>>)(() => pattern)).Body;
    // But, that allocates 168 bytes. So, this is more efficient way.
    private static MemberExpression StringAsExpression(string value) => Expression.Property(
            Expression.Constant(new StringVar(value)),
            typeof(StringVar).GetProperty(nameof(StringVar.Format))!);

    // We'll name the property Format just so we match the produced SQL query parameter name (in case of interpolated strings).
    private record StringVar(string Format);

    public static IQueryable<T> Like<T>(this IQueryable<T> source, ReadOnlySpan<LikeDto<T>> likeItems)
    {
        //var span = CollectionsMarshal.AsSpan(likeItems);
        var span = likeItems;
        var groupStart = 0;
        for (var i = 1; i <= span.Length; i++)
        {
            // If we reached the end of the span or the group has changed, we slice and process the group.
            if (i == span.Length || span[i].Group != span[groupStart].Group)
            {
                source = source.ApplyLikesAsOrGroup(span[groupStart..i]);
                groupStart = i;
            }
        }
        return source;
    }

    private static IQueryable<T> ApplyLikesAsOrGroup<T>(this IQueryable<T> source, ReadOnlySpan<LikeDto<T>> likeItems)
    {
        Debug.Assert(_likeMethodInfo is not null);

        Expression? combinedExpr = null;
        ParameterExpression? mainParam = null;
        ParameterReplacerVisitor? visitor = null;

        foreach (var likeItem in likeItems)
        {
            mainParam ??= likeItem.KeySelector.Parameters[0];

            var selectorExpr = likeItem.KeySelector.Body;
            if (mainParam != likeItem.KeySelector.Parameters[0])
            {
                visitor ??= new ParameterReplacerVisitor(likeItem.KeySelector.Parameters[0], mainParam);

                // If there are more than 2 likes, we want to avoid creating a new visitor instance (saving 32 bytes per instance).
                // We're in a sequential loop, no concurrency issues.
                visitor.Update(likeItem.KeySelector.Parameters[0], mainParam);
                selectorExpr = visitor.Visit(selectorExpr);
            }

            var patternExpr = StringAsExpression(likeItem.Pattern);

            var likeExpr = Expression.Call(
                null,
                _likeMethodInfo,
                _functions,
                selectorExpr,
                patternExpr);

            combinedExpr = combinedExpr is null
                ? likeExpr
                : Expression.OrElse(combinedExpr, likeExpr);
        }

        return combinedExpr is null || mainParam is null
            ? source
            : source.Where(Expression.Lambda<Func<T, bool>>(combinedExpr, mainParam));
    }
}

internal sealed class ParameterReplacerVisitor : ExpressionVisitor
{
    private ParameterExpression _oldParameter;
    private Expression _newExpression;

    internal ParameterReplacerVisitor(ParameterExpression oldParameter, Expression newExpression) =>
        (_oldParameter, _newExpression) = (oldParameter, newExpression);

    internal void Update(ParameterExpression oldParameter, Expression newExpression) =>
        (_oldParameter, _newExpression) = (oldParameter, newExpression);

    protected override Expression VisitParameter(ParameterExpression node) =>
        node == _oldParameter ? _newExpression : node;
}
