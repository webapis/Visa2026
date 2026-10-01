#nullable enable

using System.Collections;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;

namespace Visa2026.Module.Tests.TestSupport;

/// <summary>
/// Minimal <see cref="IObjectSpace"/> for unit tests that only need GetObject / GetKeyValue /
/// GetObjectsQuery. Not sealed — <see cref="DispatchProxy"/> requires an unsealed TProxy.
/// </summary>
internal class PassthroughObjectSpaceStub : DispatchProxy
{
    private readonly ConcurrentDictionary<Type, IList> _sets = new();

    public static IObjectSpace Create(Action<PassthroughObjectSpaceStub>? configure = null)
    {
        var space = Create<IObjectSpace, PassthroughObjectSpaceStub>();
        var stub = (PassthroughObjectSpaceStub)(object)space;
        configure?.Invoke(stub);
        return space;
    }

    public void Seed<T>(params T[] items) where T : class
    {
        var list = (List<T>)_sets.GetOrAdd(typeof(T), _ => new List<T>());
        list.AddRange(items);
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        ArgumentNullException.ThrowIfNull(targetMethod);
        args ??= Array.Empty<object?>();
        var name = targetMethod.Name;

        if (name == nameof(IObjectSpace.GetObject) && args.Length == 1)
            return args[0];

        if (name == nameof(IObjectSpace.GetKeyValue) && args.Length == 1)
        {
            var obj = args[0];
            if (obj == null)
                return null;
            var idProp = obj.GetType().GetProperty("ID", BindingFlags.Instance | BindingFlags.Public);
            return idProp?.GetValue(obj);
        }

        if (name == nameof(IObjectSpace.GetObjectsQuery) && targetMethod.IsGenericMethod)
        {
            var entityType = targetMethod.GetGenericArguments()[0];
            if (!_sets.TryGetValue(entityType, out var list))
                list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(entityType))!;

            var asQueryable = typeof(Queryable)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(m => m.Name == nameof(Queryable.AsQueryable) && m.IsGenericMethodDefinition && m.GetParameters().Length == 1);
            return asQueryable.MakeGenericMethod(entityType).Invoke(null, new object[] { list });
        }

        if (name == nameof(IObjectSpace.GetObjects) && args.Length >= 1 && args[0] is Type type)
        {
            if (!_sets.TryGetValue(type, out var list))
                list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(type))!;
            return list;
        }

        if (targetMethod.ReturnType == typeof(void))
            return null;
        if (targetMethod.ReturnType == typeof(bool))
            return false;
        if (targetMethod.ReturnType.IsValueType)
            return Activator.CreateInstance(targetMethod.ReturnType);
        return null;
    }
}
