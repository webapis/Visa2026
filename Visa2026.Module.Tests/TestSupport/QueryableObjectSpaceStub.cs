using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DevExpress.ExpressApp;

namespace Visa2026.Module.Tests.TestSupport;

/// <summary>
/// Minimal <see cref="IObjectSpace"/> for unit tests: <c>GetObject</c> pass-through,
/// <c>GetKeyValue</c> reads <c>ID</c>, and <c>GetObjectsQuery&lt;T&gt;</c> returns seeded sets (or empty).
/// </summary>
internal class QueryableObjectSpaceStub : DispatchProxy
{
    private readonly Dictionary<Type, object> _sets = new();

    public static IObjectSpace Create(Action<QueryableObjectSpaceStub>? configure = null)
    {
        var space = Create<IObjectSpace, QueryableObjectSpaceStub>();
        var stub = (QueryableObjectSpaceStub)(object)space;
        configure?.Invoke(stub);
        return space;
    }

    public void SetQuery<T>(IEnumerable<T> items)
        where T : class =>
        _sets[typeof(T)] = items.AsQueryable();

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod == null)
            return null;

        if (targetMethod.Name == nameof(IObjectSpace.GetObject) && args?.Length == 1)
            return args[0];

        // GetObjectByKey / GetObjectByKey<T> — return null unless a seeded entity matches the key.
        if (targetMethod.Name == "GetObjectByKey")
        {
            Type? entityType = null;
            object? key = null;
            if (targetMethod.IsGenericMethod && args?.Length == 1)
            {
                entityType = targetMethod.GetGenericArguments()[0];
                key = args[0];
            }
            else if (args?.Length == 2)
            {
                entityType = args[0] as Type;
                key = args[1];
            }

            if (entityType != null && _sets.TryGetValue(entityType, out var seeded)
                && seeded is System.Collections.IEnumerable enumerable)
            {
                foreach (var item in enumerable)
                {
                    if (item == null)
                        continue;
                    var id = item.GetType().GetProperty("ID", BindingFlags.Instance | BindingFlags.Public)
                        ?.GetValue(item);
                    if (Equals(id, key))
                        return item;
                }
            }

            return null;
        }

        if (targetMethod.Name == nameof(IObjectSpace.GetKeyValue) && args?.Length == 1)
        {
            var target = args[0];
            if (target == null)
                return null;
            var id = target.GetType().GetProperty("ID", BindingFlags.Instance | BindingFlags.Public);
            return id?.GetValue(target);
        }

        if (targetMethod.Name == nameof(IObjectSpace.GetObjectsQuery) && targetMethod.IsGenericMethod)
        {
            var entityType = targetMethod.GetGenericArguments()[0];
            if (_sets.TryGetValue(entityType, out var seeded))
                return seeded;

            var empty = typeof(Enumerable)
                .GetMethod(nameof(Enumerable.Empty), BindingFlags.Public | BindingFlags.Static)!
                .MakeGenericMethod(entityType)
                .Invoke(null, null)!;
            return typeof(Queryable)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(m => m.Name == nameof(Queryable.AsQueryable) && m.IsGenericMethodDefinition)
                .MakeGenericMethod(entityType)
                .Invoke(null, [empty]);
        }

        if (targetMethod.Name is nameof(IDisposable.Dispose) or "Dispose")
            return null;

        if (targetMethod.ReturnType == typeof(void))
            return null;

        if (targetMethod.ReturnType == typeof(bool))
            return false;

        if (targetMethod.ReturnType.IsValueType)
            return Activator.CreateInstance(targetMethod.ReturnType);

        return null;
    }
}
