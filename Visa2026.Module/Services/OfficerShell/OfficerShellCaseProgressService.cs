using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.Persistent.BaseImpl.EF;
using Microsoft.EntityFrameworkCore;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;
using Visa2026.Module.Services;

namespace Visa2026.Module.Services.OfficerShell;

/// <summary>
/// Case workspace progress tab: officer notes, ministry letter, and in-shell advance.
/// </summary>
public sealed class OfficerShellCaseProgressService : IOfficerShellCaseProgressService
{
    public OfficerShellCaseProgressResult SaveOfficerNotes(IObjectSpace objectSpace, Guid applicationId, string? notes)
    {
        if (objectSpace == null)
            return OfficerShellCaseProgressResult.Failed("ObjectSpace is required.");

        var application = objectSpace.GetObjectByKey<ApplicationProfileInstance>(applicationId);
        if (application == null)
            return OfficerShellCaseProgressResult.Failed("ApplicationProfileInstance not found.");

        var latest = ApplicationProfileInstanceProgressHelper.GetLatest(application.ProgressHistory, objectSpace);
        ApplicationProfileInstanceOfficeNotesHelper.Save(application, latest, notes);
        return OfficerShellCaseProgressResult.Succeeded();
    }

    public OfficerShellCaseProgressResult SetMinistryLetter(
        IObjectSpace objectSpace,
        Guid applicationId,
        string fileName,
        byte[] content,
        Guid? progressId = null)
    {
        if (objectSpace == null)
            return OfficerShellCaseProgressResult.Failed("ObjectSpace is required.");

        if (content == null || content.Length == 0)
            return OfficerShellCaseProgressResult.Failed("The uploaded file is empty.");

        var application = objectSpace.GetObjectByKey<ApplicationProfileInstance>(applicationId);
        if (application == null)
            return OfficerShellCaseProgressResult.Failed("ApplicationProfileInstance not found.");

        var row = progressId is Guid id && id != Guid.Empty
            ? application.ProgressHistory?.FirstOrDefault(p => p.ID == id)
            : ApplicationProfileInstanceProgressHelper.GetLatest(application.ProgressHistory, objectSpace);
        if (row == null)
            return OfficerShellCaseProgressResult.Failed("No progress history on this application.");

        return AttachMinistryLetter(objectSpace, row, fileName, content);
    }

    public OfficerShellCaseProgressResult Advance(
        IObjectSpace objectSpace,
        Guid applicationId,
        string? stateCode,
        string? notesOnLatestStep,
        DateTime? stepDate,
        string? letterFileName = null,
        byte[]? letterContent = null)
    {
        if (objectSpace == null)
            return OfficerShellCaseProgressResult.Failed("ObjectSpace is required.");

        var application = LoadApplicationForAdvance(objectSpace, applicationId);
        if (application == null)
            return OfficerShellCaseProgressResult.Failed("ApplicationProfileInstance not found.");

        if (!ApplicationProfileInstanceProgressProfileResolver.TryValidateProjectContractOnApplication(application, objectSpace, out var contractError))
            return OfficerShellCaseProgressResult.Failed(contractError ?? VisaUiMessages.Get("ApplicationProfileInstanceProgress.ProjectContractRequired"));

        var latest = ApplicationProfileInstanceProgressHelper.GetLatest(application.ProgressHistory, objectSpace);
        if (latest != null && notesOnLatestStep != null)
            latest.Description = notesOnLatestStep.Trim();

        var allowedCodes = ApplicationProfileInstanceProgressTransitionHelper
            .GetAllowedNextStateCodes(application, latest)
            .ToList();

        if (allowedCodes.Count == 0)
            return OfficerShellCaseProgressResult.Failed(VisaUiMessages.Get("ApplicationProfileInstanceProgress.CannotAdvanceFromTerminal"));

        var chosenCode = string.IsNullOrWhiteSpace(stateCode)
            ? allowedCodes[0]
            : stateCode.Trim();

        if (!allowedCodes.Contains(chosenCode, StringComparer.OrdinalIgnoreCase))
            return OfficerShellCaseProgressResult.Failed(VisaUiMessages.Get("ApplicationProfileInstanceProgress.InvalidForRoute"));

        var state = objectSpace.GetObjectsQuery<ApplicationState>()
            .FirstOrDefault(s => s.Code == chosenCode);
        if (state == null)
            return OfficerShellCaseProgressResult.Failed(VisaUiMessages.Get("ApplicationProfileInstanceProgress.InvalidForRoute"));

        var date = stepDate is { } chosen && chosen != default
            ? chosen.Date
            : DateTime.Today;
        if (date == default)
            return OfficerShellCaseProgressResult.Failed("Date is required.");

        var progress = objectSpace.CreateObject<ApplicationProfileInstanceProgress>();
        progress.ApplicationProfileInstance = application;
        progress.Date = date;
        progress.State = state;
        application.ProgressHistory ??= new System.Collections.ObjectModel.ObservableCollection<ApplicationProfileInstanceProgress>();
        if (!application.ProgressHistory.Contains(progress))
            application.ProgressHistory.Add(progress);

        if (ApplicationMigrationSlaHelper.IsMigrationServiceProcessStartedStep(chosenCode)
            && !string.IsNullOrWhiteSpace(application.ProcessNumber))
        {
            progress.ProcessNumber = application.ProcessNumber.Trim();
        }

        if (!ApplicationProfileInstanceProgressTransitionHelper.TryValidateProgressStep(progress, objectSpace, out var progressError))
            return OfficerShellCaseProgressResult.Failed(progressError ?? VisaUiMessages.Get("ApplicationProfileInstanceProgress.InvalidForRoute"));

        if (latest == null)
            ApplicationProfileInstanceOfficeNotesHelper.CopyOntoNewRow(application, progress, notesOnLatestStep);

        if (letterContent is { Length: > 0 } && progress.IsMinistryDecisionStep)
        {
            var letterResult = AttachMinistryLetter(objectSpace, progress, letterFileName ?? "ministry-letter.pdf", letterContent);
            if (!letterResult.Success)
                return letterResult;
        }

        ApplicationLatestProgressSyncHelper.Sync(application, objectSpace);
        return OfficerShellCaseProgressResult.Succeeded(chosenCode);
    }

    public OfficerShellCaseProgressResult Revert(
        IObjectSpace objectSpace,
        Guid applicationId,
        string? stepKey)
    {
        if (objectSpace == null)
            return OfficerShellCaseProgressResult.Failed("ObjectSpace is required.");

        var application = LoadApplicationForAdvance(objectSpace, applicationId);
        if (application == null)
            return OfficerShellCaseProgressResult.Failed("ApplicationProfileInstance not found.");

        var toDelete = ApplicationProfileInstanceProgressRevertHelper
            .RowsToDelete(application.ProgressHistory, stepKey)
            .ToList();
        if (toDelete.Count == 0)
            return OfficerShellCaseProgressResult.Failed("Nothing to revert on this application.");

        foreach (var row in toDelete
                     .OrderByDescending(p => p, Comparer<ApplicationProfileInstanceProgress>.Create(
                         ApplicationProfileInstanceProgressOrderHelper.CompareSiblingOrder)))
        {
            if (row.MinistryLetterFile != null)
                objectSpace.Delete(row.MinistryLetterFile);
            objectSpace.Delete(row);
            application.ProgressHistory?.Remove(row);
        }

        ApplicationLatestProgressSyncHelper.Sync(application, objectSpace);
        return OfficerShellCaseProgressResult.Succeeded();
    }

    private static OfficerShellCaseProgressResult AttachMinistryLetter(
        IObjectSpace objectSpace,
        ApplicationProfileInstanceProgress row,
        string fileName,
        byte[] content)
    {
        if (!row.IsMinistryDecisionStep)
            return OfficerShellCaseProgressResult.Failed("Ministry letter upload is only available on ministry decision steps.");

        var maxBytes = row.MaxDocumentSizeInMB * 1024L * 1024L;
        if (content.LongLength > maxBytes)
            return OfficerShellCaseProgressResult.Failed($"The ministry letter exceeds the maximum allowed size of {row.MaxDocumentSizeInMB} MB.");

        var file = row.MinistryLetterFile ?? objectSpace.CreateObject<FileData>();
        file.FileName = string.IsNullOrWhiteSpace(fileName) ? "ministry-letter.pdf" : fileName.Trim();
        file.Content = content;
        file.Size = content.Length;

        if (!DocumentFileUploadConstraints.TryValidate(file, out var validationError))
            return OfficerShellCaseProgressResult.Failed(validationError ?? "The ministry letter file is not valid.");

        row.MinistryLetterFile = file;
        return OfficerShellCaseProgressResult.Succeeded();
    }

    private static ApplicationProfileInstance? LoadApplicationForAdvance(IObjectSpace objectSpace, Guid applicationId)
    {
        try
        {
            return objectSpace.GetObjectsQuery<ApplicationProfileInstance>()
                .Include(a => a.ApplicationProfile)
                    .ThenInclude(p => p!.ApprovalLegs)
                        .ThenInclude(l => l.ApprovingMinistry)
                .Include(a => a.ApplicationProfile)
                    .ThenInclude(p => p!.ApprovalLegVersions)
                        .ThenInclude(v => v.Legs)
                            .ThenInclude(l => l.ApprovingMinistry)
                .Include(a => a.ApprovalLegProfile)
                    .ThenInclude(p => p!.MinistryLegs)
                .Include(a => a.ApprovalLegSnapshots)
                .Include(a => a.ProgressHistory)
                    .ThenInclude(p => p.State)
                .Include(a => a.ProgressHistory)
                    .ThenInclude(p => p.MinistryLetterFile)
                .Include(a => a.ApplicationType)
                .FirstOrDefault(a => a.ID == applicationId);
        }
        catch (Exception)
        {
            return objectSpace.GetObjectByKey<ApplicationProfileInstance>(applicationId);
        }
    }
}
