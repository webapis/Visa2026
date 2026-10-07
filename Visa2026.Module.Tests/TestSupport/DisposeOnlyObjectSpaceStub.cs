#nullable enable

using System.Reflection;
using DevExpress.ExpressApp;

namespace Visa2026.Module.Tests.TestSupport;

/// <summary>
/// Minimal <see cref="IObjectSpace"/> stub for fail-closed gate tests that must pass a non-null
/// ObjectSpace but never query it. Distinct from open coverage-PR stubs (must not be sealed for DispatchProxy).
/// </summary>
internal class DisposeOnlyObjectSpaceStub : DispatchProxy
{
    public static IObjectSpace Create() =>
        Create<IObjectSpace, DisposeOnlyObjectSpaceStub>();

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        ArgumentNullException.ThrowIfNull(targetMethod);

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
