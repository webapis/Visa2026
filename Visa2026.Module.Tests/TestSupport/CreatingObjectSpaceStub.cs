#nullable enable

using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using DevExpress.ExpressApp;

namespace Visa2026.Module.Tests.TestSupport;

/// <summary>
/// Minimal <see cref="IObjectSpace"/> for unit tests that need <c>CreateObject</c> /
/// <c>GetObjectByKey</c>. Not sealed — <see cref="DispatchProxy"/> requires an unsealed TProxy.
/// Distinct from open-PR stubs (<c>PassthroughObjectSpaceStub</c> / <c>QueryableObjectSpaceStub</c>).
/// </summary>
internal class CreatingObjectSpaceStub : DispatchProxy
{
    private readonly ConcurrentDictionary<Type, IList> _sets = new();

    public static IObjectSpace Create(Action<CreatingObjectSpaceStub>? configure = null)
    {
        var space = Create<IObjectSpace, CreatingObjectSpaceStub>();
        var stub = (CreatingObjectSpaceStub)(object)space;
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

        if (name == nameof(IObjectSpace.CreateObject) && targetMethod.IsGenericMethod)
        {
            var entityType = targetMethod.GetGenericArguments()[0];
            var instance = Activator.CreateInstance(entityType)
                ?? throw new InvalidOperationException($"Cannot create {entityType.Name}.");
            var list = (IList)_sets.GetOrAdd(entityType, _ => (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(entityType))!);
            list.Add(instance);
            return instance;
        }

        if (name == nameof(IObjectSpace.GetObjectByKey) && targetMethod.IsGenericMethod && args.Length >= 1)
        {
            var entityType = targetMethod.GetGenericArguments()[0];
            if (!_sets.TryGetValue(entityType, out var list) || args[0] is not Guid id || id == Guid.Empty)
                return null;

            foreach (var item in list)
            {
                var idProp = item?.GetType().GetProperty("ID", BindingFlags.Instance | BindingFlags.Public);
                if (idProp?.GetValue(item) is Guid itemId && itemId == id)
                    return item;
            }

            return null;
        }

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
