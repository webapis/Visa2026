#nullable enable

using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using DevExpress.ExpressApp;

namespace Visa2026.Module.Tests.TestSupport;

/// <summary>
/// Multi-type <see cref="IObjectSpace"/> seed stub for GetObjectsQuery / CreateObject /
/// IsNewObject / ModifiedObjects / GetObjectsToSave. Distinct from open coverage-PR stubs.
/// Not sealed — <see cref="DispatchProxy"/> requires an unsealed TProxy.
/// </summary>
internal class MultiTypeSeedObjectSpaceStub : DispatchProxy
{
    private readonly ConcurrentDictionary<Type, IList> _sets = new();
    private readonly HashSet<object> _newObjects = new(ReferenceEqualityComparer.Instance);
    private readonly List<object> _modified = new();
    private readonly List<object> _toSave = new();

    public static IObjectSpace Create(Action<MultiTypeSeedObjectSpaceStub>? configure = null)
    {
        var space = Create<IObjectSpace, MultiTypeSeedObjectSpaceStub>();
        var stub = (MultiTypeSeedObjectSpaceStub)(object)space;
        configure?.Invoke(stub);
        return space;
    }

    public void Seed<T>(params T[] items) where T : class
    {
        var list = (List<T>)_sets.GetOrAdd(typeof(T), _ => new List<T>());
        list.AddRange(items);
    }

    public void MarkNew(object entity) => _newObjects.Add(entity);

    public void TrackModified(params object[] entities) => _modified.AddRange(entities);

    public void TrackToSave(params object[] entities) => _toSave.AddRange(entities);

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
            var list = (IList)_sets.GetOrAdd(
                entityType,
                _ => (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(entityType))!);
            list.Add(instance);
            _newObjects.Add(instance);
            return instance;
        }

        if (name == nameof(IObjectSpace.IsNewObject) && args.Length == 1 && args[0] != null)
            return _newObjects.Contains(args[0]!);

        if (name == nameof(IObjectSpace.GetObject) && args.Length == 1)
            return args[0];

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

        if (name == nameof(IObjectSpace.GetKeyValue) && args.Length == 1)
        {
            var obj = args[0];
            if (obj == null)
                return null;
            return obj.GetType().GetProperty("ID", BindingFlags.Instance | BindingFlags.Public)?.GetValue(obj);
        }

        if (name == nameof(IObjectSpace.GetObjectsQuery) && targetMethod.IsGenericMethod)
        {
            var entityType = targetMethod.GetGenericArguments()[0];
            if (!_sets.TryGetValue(entityType, out var list))
                list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(entityType))!;

            var asQueryable = typeof(Queryable)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(m => m.Name == nameof(Queryable.AsQueryable)
                    && m.IsGenericMethodDefinition
                    && m.GetParameters().Length == 1);
            return asQueryable.MakeGenericMethod(entityType).Invoke(null, [list]);
        }

        if (name == nameof(IObjectSpace.GetObjects) && args.Length >= 1 && args[0] is Type type)
        {
            if (!_sets.TryGetValue(type, out var list))
                list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(type))!;
            return list;
        }

        if (name == nameof(IObjectSpace.GetObjectsToSave))
            return _toSave;

        if (name == "get_ModifiedObjects")
            return _modified;

        if (name is nameof(IDisposable.Dispose) or "Dispose" or nameof(IObjectSpace.Delete)
            or nameof(IObjectSpace.CommitChanges) or nameof(IObjectSpace.SetModified))
            return null;

        if (targetMethod.ReturnType == typeof(void))
            return null;
        if (targetMethod.ReturnType == typeof(bool))
            return false;
        if (targetMethod.ReturnType.IsValueType)
            return Activator.CreateInstance(targetMethod.ReturnType);
        return null;
    }

    private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public static readonly ReferenceEqualityComparer Instance = new();
        public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);
        public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }
}
