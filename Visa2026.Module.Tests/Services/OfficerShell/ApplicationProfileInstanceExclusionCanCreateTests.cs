using System;
using System.Collections.ObjectModel;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.OfficerShell;
using Xunit;

namespace Visa2026.Module.Tests.Services.OfficerShell;

/// <summary>
/// Seretmezlik create gate: after office prep, before terminal close.
/// </summary>
public class ApplicationProfileInstanceExclusionCanCreateTests
{
    private readonly ApplicationProfileInstanceExclusionService _service =
        new(new UnusedProgressService());

    private sealed class UnusedProgressService : IOfficerShellCaseProgressService
    {
        public OfficerShellCaseProgressResult SaveOfficerNotes(IObjectSpace objectSpace, Guid applicationId, string? notes) =>
            throw new NotSupportedException();

        public OfficerShellCaseProgressResult SetMinistryLetter(
            IObjectSpace objectSpace,
            Guid applicationId,
            string fileName,
            byte[] content,
            Guid? progressId = null) =>
            throw new NotSupportedException();

        public OfficerShellCaseProgressResult Advance(
            IObjectSpace objectSpace,
            Guid applicationId,
            string? stateCode,
            string? notesOnLatestStep,
            DateTime? stepDate,
            string? letterFileName = null,
            byte[]? letterContent = null,
            string? processNumber = null) =>
            throw new NotSupportedException();

        public OfficerShellCaseProgressResult Revert(IObjectSpace objectSpace, Guid applicationId, string? stepKey) =>
            throw new NotSupportedException();
    }

    [Fact]
    public void CanCreate_null_instance_is_blocked()
    {
        Assert.False(_service.CanCreate(null!, out var reason));
        Assert.Equal("Case not found.", reason);
    }

    [Fact]
    public void CanCreate_blocked_during_office_preparation()
    {
        var instance = BuildInstance(ApplicationProfileInstanceProgressStateCodes.IsBeingPrepared);

        Assert.False(instance.IsLockedAfterOfficePreparation);
        Assert.False(_service.CanCreate(instance, out var reason));
        Assert.Contains("office preparation", reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CanCreate_allowed_after_ministry_step()
    {
        var instance = BuildInstance(
            ApplicationProfileInstanceProgressStateCodes.IsBeingPrepared,
            ApplicationProfileInstanceProgressStateCodes.Review1Started);

        Assert.True(instance.IsLockedAfterOfficePreparation);
        Assert.False(instance.IsWorkflowTerminal);
        Assert.True(_service.CanCreate(instance, out var reason));
        Assert.Null(reason);
    }

    [Theory]
    [InlineData(ApplicationProfileInstanceProgressStateCodes.ProcessCancelled)]
    [InlineData(ApplicationProfileInstanceProgressStateCodes.ProcessRejected)]
    [InlineData(ApplicationProfileInstanceProgressStateCodes.ProcessIssued)]
    public void CanCreate_blocked_when_workflow_terminal(string terminalState)
    {
        var instance = BuildInstance(
            ApplicationProfileInstanceProgressStateCodes.IsBeingPrepared,
            ApplicationProfileInstanceProgressStateCodes.Review1Started,
            terminalState);

        Assert.True(instance.IsLockedAfterOfficePreparation);
        Assert.True(instance.IsWorkflowTerminal);
        Assert.False(_service.CanCreate(instance, out var reason));
        Assert.Contains("closed", reason, StringComparison.OrdinalIgnoreCase);
    }

    private static ApplicationProfileInstance BuildInstance(params string[] stateCodes)
    {
        var history = new ObservableCollection<ApplicationProfileInstanceProgress>();
        for (var i = 0; i < stateCodes.Length; i++)
        {
            history.Add(new ApplicationProfileInstanceProgress
            {
                Order = i + 1,
                Date = new DateTime(2026, 1, 1).AddDays(i),
                State = new ApplicationState { Code = stateCodes[i] },
            });
        }

        return new ApplicationProfileInstance { ProgressHistory = history };
    }
}
