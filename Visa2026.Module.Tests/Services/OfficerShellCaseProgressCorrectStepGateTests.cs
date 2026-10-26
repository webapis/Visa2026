#nullable enable

using System;
using System.Reflection;
using DevExpress.ExpressApp;
using Visa2026.Module.Services.OfficerShell;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Early CorrectStep / RemoveProgressFile guards that reject bad inputs before
/// any ObjectSpace load — regression net for the correct-finished-step path.
/// </summary>
public class OfficerShellCaseProgressCorrectStepGateTests
{
    [Fact]
    public void CorrectStep_requires_object_space()
    {
        var service = new OfficerShellCaseProgressService();

        var result = service.CorrectStep(
            objectSpace: null!,
            applicationId: Guid.NewGuid(),
            progressId: Guid.NewGuid(),
            stepDate: new DateTime(2026, 10, 1),
            submissionDocumentNumber: null);

        Assert.False(result.Success);
        Assert.Equal("ObjectSpace is required.", result.ErrorMessage);
    }

    [Fact]
    public void CorrectStep_empty_progress_id_fails_before_object_space_use()
    {
        var service = new OfficerShellCaseProgressService();
        var os = ThrowingObjectSpaceStub.Create();

        var result = service.CorrectStep(
            os,
            applicationId: Guid.NewGuid(),
            progressId: Guid.Empty,
            stepDate: new DateTime(2026, 10, 1),
            submissionDocumentNumber: null);

        Assert.False(result.Success);
        Assert.Equal("Progress step was not found.", result.ErrorMessage);
    }

    [Fact]
    public void RemoveProgressFile_requires_object_space()
    {
        var service = new OfficerShellCaseProgressService();

        var result = service.RemoveProgressFile(
            objectSpace: null!,
            applicationId: Guid.NewGuid(),
            progressId: Guid.NewGuid(),
            fileId: Guid.NewGuid());

        Assert.False(result.Success);
        Assert.Equal("ObjectSpace is required.", result.ErrorMessage);
    }

    private class ThrowingObjectSpaceStub : DispatchProxy
    {
        public static IObjectSpace Create() => Create<IObjectSpace, ThrowingObjectSpaceStub>();

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException(
                $"ObjectSpace should not be used for this gate ({targetMethod?.Name}).");
    }
}
