using Microsoft.EntityFrameworkCore.Query;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace ReflectionBenchmarks;

public static class IncludeExtensions3
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
    private static readonly ConcurrentDictionary<CacheKey, Func<object, object, object>> _cache = new();

    public static IQueryable<T> IncludeCustom3<T, TProperty>(
        this IQueryable<T> source,
        Expression<Func<T, TProperty>> expr) where T : class
    {
        var key = new CacheKey(1, typeof(T), expr.ReturnType, null);
        var include = _cache.GetOrAdd(key, k => CreateDynamicDelegate(_includeMethodInfo, k.EntityType, k.PropertyType));
        return (IQueryable<T>)include(source, expr);
    }

    public static IQueryable<T> ThenIncludeCustom3<T, TPreviousProperty, TProperty>(
        this IQueryable<T> source,
        Expression<Func<TPreviousProperty, TProperty>> expr) where T : class
    {
        var key = new CacheKey(2, typeof(T), typeof(TProperty), typeof(TPreviousProperty));
        var include = _cache.GetOrAdd(key, k => CreateDynamicDelegate(_thenIncludeAfterReferenceMethodInfo, k.EntityType, k.PreviousReturnType!, k.PropertyType));
        return (IQueryable<T>)include(source, expr);
    }

    public static IQueryable<T> ThenIncludeCustom3<T, TPreviousProperty, TProperty>(
        this IQueryable<T> source,
        Expression<Func<TPreviousProperty, IEnumerable<TProperty>>> expr) where T : class
    {
        var key = new CacheKey(3, typeof(T), typeof(TProperty), typeof(TPreviousProperty));
        var include = _cache.GetOrAdd(key, k => CreateDynamicDelegate(_thenIncludeAfterEnumerableMethodInfo, k.EntityType, k.PreviousReturnType!, k.PropertyType));
        return (IQueryable<T>)include(source, expr);
    }

    private static Func<object, object, object> CreateDynamicDelegate(MethodInfo methodInfo, params Type[] genericTypes)
    {
        var genericMethod = methodInfo.MakeGenericMethod(genericTypes);
        var dm = new DynamicMethod($"FastInclude_{genericMethod.Name}_{Guid.NewGuid()}", typeof(object), new[] { typeof(object), typeof(object) }, true);
        var il = dm.GetILGenerator();
        // Load arguments and cast
        il.Emit(OpCodes.Ldnull); // static method, so null for 'this'
        il.Emit(OpCodes.Ldarg_0); // source
        il.Emit(OpCodes.Castclass, genericMethod.GetParameters()[0].ParameterType);
        il.Emit(OpCodes.Ldarg_1); // expr
        il.Emit(OpCodes.Castclass, genericMethod.GetParameters()[1].ParameterType);
        il.Emit(OpCodes.Call, genericMethod);
        if (genericMethod.ReturnType.IsValueType)
            il.Emit(OpCodes.Box, genericMethod.ReturnType);
        il.Emit(OpCodes.Ret);
        return (Func<object, object, object>)dm.CreateDelegate(typeof(Func<object, object, object>));
    }
}
