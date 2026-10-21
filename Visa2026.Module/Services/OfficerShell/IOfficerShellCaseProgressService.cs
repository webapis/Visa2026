using System;
using System.Collections.Generic;
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
        string? processNumber = null,
        string? resultNumber = null,
        string? submissionDocumentNumber = null,
        IReadOnlyList<(string Name, byte[] Content)>? additionalFiles = null);

    OfficerShellCaseProgressResult CorrectStep(
        IObjectSpace objectSpace,
        Guid applicationId,
        Guid progressId,
        DateTime stepDate,
        string? submissionDocumentNumber);

    OfficerShellCaseProgressResult AddProgressFile(
        IObjectSpace objectSpace,
        Guid applicationId,
        Guid progressId,
        int slot,
        string fileName,
        byte[] content);

    OfficerShellCaseProgressResult ReplaceProgressFile(
        IObjectSpace objectSpace,
        Guid applicationId,
        Guid progressId,
        Guid fileId,
        string fileName,
        byte[] content);

    OfficerShellCaseProgressResult RemoveProgressFile(
        IObjectSpace objectSpace,
        Guid applicationId,
        Guid progressId,
        Guid fileId);

    OfficerShellCaseProgressResult SaveMigrationSubmission(
        IObjectSpace objectSpace,
        Guid applicationId,
        DateTime? submittedOn,
        string? documentNumber,
        string? fileName,
        byte[]? fileContent);

    OfficerShellCaseProgressResult Revert(
        IObjectSpace objectSpace,
        Guid applicationId,
        string? stepKey);
}
