using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DevExpress.ExpressApp;

namespace Visa2026.Module.Tests.TestSupport;

/// <summary>
/// Minimal <see cref="IObjectSpace"/>: empty <c>GetObjectsQuery</c>, pass-through <c>GetObject</c>,
/// and no-op <c>Delete</c>/<c>Dispose</c>. Distinct from open coverage-PR stubs.
/// </summary>
internal class EmptyQueryObjectSpaceStub : DispatchProxy
{
    public static IObjectSpace Create() =>
        Create<IObjectSpace, EmptyQueryObjectSpaceStub>();

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod == null)
            return null;

        if (targetMethod.Name == nameof(IObjectSpace.GetObject) && args?.Length == 1)
            return args[0];

        if (targetMethod.Name == "GetObjectByKey")
            return null;

        if (targetMethod.Name == nameof(IObjectSpace.GetKeyValue) && args?.Length == 1)
        {
            var target = args[0];
            if (target == null)
                return null;
            return target.GetType().GetProperty("ID", BindingFlags.Instance | BindingFlags.Public)
                ?.GetValue(target);
        }

        if (targetMethod.Name == nameof(IObjectSpace.GetObjectsQuery) && targetMethod.IsGenericMethod)
        {
            var entityType = targetMethod.GetGenericArguments()[0];
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

        if (targetMethod.Name is nameof(IDisposable.Dispose) or "Dispose" or nameof(IObjectSpace.Delete))
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
