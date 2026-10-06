#nullable enable

using System.Collections;
using System.Reflection;
using DevExpress.ExpressApp;

namespace Visa2026.Module.Tests.TestSupport;

/// <summary>
/// Minimal <see cref="IObjectSpace"/> stub that serves seeded <see cref="GetObjectsQuery{T}"/>
/// results for culture-resolution tests. Distinct from open coverage-PR stubs (must not be sealed for DispatchProxy).
/// </summary>
internal class CultureQueryObjectSpaceStub : DispatchProxy
{
    private readonly Dictionary<Type, IList> _sets = new();

    public static IObjectSpace Create(Action<CultureQueryObjectSpaceStub>? configure = null)
    {
        var space = Create<IObjectSpace, CultureQueryObjectSpaceStub>();
        var stub = (CultureQueryObjectSpaceStub)(object)space;
        configure?.Invoke(stub);
        return space;
    }

    public void Seed<T>(params T[] items) where T : class
    {
        if (!_sets.TryGetValue(typeof(T), out var list))
        {
            list = new List<T>();
            _sets[typeof(T)] = list;
        }

        foreach (var item in items)
            list.Add(item);
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        ArgumentNullException.ThrowIfNull(targetMethod);
        args ??= Array.Empty<object?>();
        var name = targetMethod.Name;

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

        if (name is nameof(IDisposable.Dispose) or "Dispose")
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
