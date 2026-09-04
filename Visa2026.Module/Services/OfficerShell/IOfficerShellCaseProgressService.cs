using System;
using DevExpress.ExpressApp;

namespace Visa2026.Module.Services.OfficerShell;

public interface IOfficerShellCaseProgressService
{
    OfficerShellCaseProgressResult SaveOfficerNotes(IObjectSpace objectSpace, Guid applicationId, string? notes);

    OfficerShellCaseProgressResult SetMinistryLetter(
        IObjectSpace objectSpace,
        Guid applicationId,
        string fileName,
        byte[] content,
        Guid? progressId = null);

    OfficerShellCaseProgressResult Advance(
        IObjectSpace objectSpace,
        Guid applicationId,
        string? stateCode,
        string? notesOnLatestStep,
        DateTime? stepDate,
        string? letterFileName = null,
        byte[]? letterContent = null,
        string? processNumber = null);

    OfficerShellCaseProgressResult Revert(
        IObjectSpace objectSpace,
        Guid applicationId,
        string? stepKey);
}
