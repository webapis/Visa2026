using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.Persistent.BaseImpl.EF;
using Microsoft.EntityFrameworkCore;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;
using Visa2026.Module.Services;
using Visa2026.Module.Services.ApplicationWorkspace;

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
        byte[]? letterContent = null,
        string? processNumber = null,
        string? resultNumber = null,
        string? submissionDocumentNumber = null)
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
            return OfficerShellCaseProgressResult.Failed(VisaUiMessages.Get("ApplicationProgress.InvalidForRoute"));

        var state = objectSpace.GetObjectsQuery<ApplicationState>()
            .FirstOrDefault(s => s.Code == chosenCode);
        if (state == null)
            return OfficerShellCaseProgressResult.Failed(VisaUiMessages.Get("ApplicationProgress.InvalidForRoute"));

        var date = stepDate is { } chosen && chosen != default
            ? chosen.Date
            : DateTime.Today;
        if (date == default)
            return OfficerShellCaseProgressResult.Failed("Date is required.");

        string? normalizedResultNumber = null;
        if (ApplicationProgressResultNumberHelper.AppliesTo(chosenCode)
            && !ApplicationProgressResultNumberHelper.TryNormalize(resultNumber, required: true, out normalizedResultNumber, out var resultNumberError))
        {
            return OfficerShellCaseProgressResult.Failed(
                resultNumberError ?? VisaUiMessages.Get("ApplicationProfileInstance.Workspace.ResultNumberRequired"));
        }

        var viaMinistries = ApplicationProfileInstanceProgressRouteHelper.GetTypePickerRouteFilter(application)
            == ApplicationProfileInstanceProgressRouteKind.ViaMinistries
            && ApplicationProfileInstanceProgressProfileResolver.GetMinistryLegCount(application) > 0;
        if (viaMinistries
            && ApplicationMigrationSlaHelper.IsMigrationServiceProcessStartedStep(chosenCode)
            && !ApplicationMigrationSubmission.IsComplete(latest))
        {
            return OfficerShellCaseProgressResult.Failed(
                VisaUiMessages.Get("ApplicationProfileInstance.Workspace.SubmissionRequiredBeforeMigration"));
        }

        string? normalizedSubmissionNumber = null;
        if (ApplicationMigrationSubmission.IsState(chosenCode)
            && !ApplicationMigrationSubmission.TryNormalizeDocumentNumber(
                submissionDocumentNumber, out normalizedSubmissionNumber, out var submissionNumberError))
        {
            return OfficerShellCaseProgressResult.Failed(
                submissionNumberError ?? VisaUiMessages.Get("ApplicationProfileInstance.Workspace.SubmissionDocumentNumberRequired"));
        }

        if (ApplicationMigrationSubmission.IsState(chosenCode)
            && (letterContent == null || letterContent.Length == 0))
        {
            return OfficerShellCaseProgressResult.Failed(
                VisaUiMessages.Get("ApplicationProfileInstance.Workspace.SubmissionDocumentCopyRequired"));
        }

        var requireProcessNumber = ApplicationMigrationSlaHelper.IsMigrationServiceProcessStartedStep(chosenCode)
            && ApplicationProfileConfigurationResolver.ShowProcessNumber(application);
        if (requireProcessNumber)
        {
            var assigned = !string.IsNullOrWhiteSpace(processNumber)
                ? processNumber
                : application.ProcessNumber;
            if (!ApplicationProcessNumberHelper.TryAssign(
                    objectSpace,
                    application,
                    assigned,
                    requireWhenVisible: true,
                    out var processNumberError)
                || string.IsNullOrWhiteSpace(application.ProcessNumber))
            {
                return OfficerShellCaseProgressResult.Failed(
                    processNumberError ?? "Process number is required when submitting to Migration Service.");
            }
        }

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

        if (normalizedResultNumber != null)
            progress.ResultNumber = normalizedResultNumber;

        if (normalizedSubmissionNumber != null)
            progress.SubmissionDocumentNumber = normalizedSubmissionNumber;

        if (!ApplicationProfileInstanceProgressTransitionHelper.TryValidateProgressStep(progress, objectSpace, out var progressError))
            return OfficerShellCaseProgressResult.Failed(progressError ?? VisaUiMessages.Get("ApplicationProgress.InvalidForRoute"));

        if (latest == null)
            ApplicationProfileInstanceOfficeNotesHelper.CopyOntoNewRow(application, progress, notesOnLatestStep);

        if (letterContent is { Length: > 0 } && ApplicationMigrationSubmission.IsState(chosenCode))
        {
            var copyResult = AttachSubmissionDocument(objectSpace, progress, letterFileName ?? "submission.pdf", letterContent);
            if (!copyResult.Success)
                return copyResult;
        }
        else if (letterContent is { Length: > 0 }
            && (progress.IsMinistryDecisionStep || latest == null))
        {
            var letterResult = AttachMinistryLetter(objectSpace, progress, letterFileName ?? "progress-file.pdf", letterContent);
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

    public OfficerShellCaseProgressResult SaveMigrationSubmission(
        IObjectSpace objectSpace,
        Guid applicationId,
        DateTime? submittedOn,
        string? documentNumber,
        string? fileName,
        byte[]? fileContent)
    {
        if (objectSpace == null)
            return OfficerShellCaseProgressResult.Failed("ObjectSpace is required.");

        var application = LoadApplicationForAdvance(objectSpace, applicationId);
        if (application == null)
            return OfficerShellCaseProgressResult.Failed("ApplicationProfileInstance not found.");

        var date = submittedOn is { } chosen && chosen != default ? chosen.Date : default;
        if (date == default)
            return OfficerShellCaseProgressResult.Failed(VisaUiMessages.Get("ApplicationProfileInstance.Workspace.DateRequired"));

        if (!ApplicationMigrationSubmission.TryNormalizeDocumentNumber(documentNumber, out var number, out var numberError))
            return OfficerShellCaseProgressResult.Failed(numberError ?? VisaUiMessages.Get("ApplicationProfileInstance.Workspace.SubmissionDocumentNumberRequired"));

        var row = application.ProgressHistory?
            .LastOrDefault(p => ApplicationMigrationSubmission.IsState(p.State?.Code));
        var created = false;
        if (row == null)
        {
            var state = objectSpace.GetObjectsQuery<ApplicationState>()
                .FirstOrDefault(s => s.Code == ApplicationProfileInstanceProgressStateCodes.SubmittedToMigration);
            if (state == null)
                return OfficerShellCaseProgressResult.Failed(VisaUiMessages.Get("ApplicationProgress.InvalidForRoute"));

            row = objectSpace.CreateObject<ApplicationProfileInstanceProgress>();
            row.ApplicationProfileInstance = application;
            row.State = state;
            row.Order = 1;
            application.ProgressHistory ??= new System.Collections.ObjectModel.ObservableCollection<ApplicationProfileInstanceProgress>();
            application.ProgressHistory.Add(row);
            created = true;
        }

        row.Date = date;
        row.SubmissionDocumentNumber = number;
        if (fileContent is { Length: > 0 })
        {
            var copyResult = AttachSubmissionDocument(objectSpace, row, fileName ?? "submission.pdf", fileContent);
            if (!copyResult.Success)
                return copyResult;
        }
        else if (!ApplicationMigrationSubmission.HasDocumentCopy(row))
        {
            return OfficerShellCaseProgressResult.Failed(
                VisaUiMessages.Get("ApplicationProfileInstance.Workspace.SubmissionDocumentCopyRequired"));
        }

        if (created)
            ApplicationProfileInstanceProgressOrderHelper.AssignTimelineOrders(application.ProgressHistory.ToList());

        ApplicationLatestProgressSyncHelper.Sync(application, objectSpace);
        return OfficerShellCaseProgressResult.Succeeded();
    }

    private static OfficerShellCaseProgressResult AttachSubmissionDocument(
        IObjectSpace objectSpace,
        ApplicationProfileInstanceProgress row,
        string fileName,
        byte[] content)
    {
        var maxBytes = row.MaxDocumentSizeInMB * 1024L * 1024L;
        if (content.LongLength > maxBytes)
            return OfficerShellCaseProgressResult.Failed($"The document copy exceeds the maximum allowed size of {row.MaxDocumentSizeInMB} MB.");

        var file = row.SubmissionDocumentFile ?? objectSpace.CreateObject<FileData>();
        file.FileName = string.IsNullOrWhiteSpace(fileName) ? "submission.pdf" : fileName.Trim();
        file.Content = content;
        file.Size = content.Length;
        if (!DocumentFileUploadConstraints.TryValidate(file, out var validationError))
            return OfficerShellCaseProgressResult.Failed(validationError ?? "The document copy is not valid.");

        row.SubmissionDocumentFile = file;
        return OfficerShellCaseProgressResult.Succeeded();
    }

    private static OfficerShellCaseProgressResult AttachMinistryLetter(
        IObjectSpace objectSpace,
        ApplicationProfileInstanceProgress row,
        string fileName,
        byte[] content)
    {
        if (!CanStoreProgressFile(row))
            return OfficerShellCaseProgressResult.Failed("File upload is only available on office preparation or an approval leg.");

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

    private static bool CanStoreProgressFile(ApplicationProfileInstanceProgress row) =>
        row.IsMinistryDecisionStep
        || ApplicationWorkspaceProgressAdvancePreview.IsOfficeSubmitted(
            ApplicationWorkspaceProgressTimeline.OfficeKey,
            row.State?.Code)
        || string.Equals(
            row.State?.Code,
            ApplicationProfileInstanceProgressStateCodes.ProcessCancelled,
            StringComparison.OrdinalIgnoreCase);

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
                .Include(a => a.ProgressHistory)
                    .ThenInclude(p => p.SubmissionDocumentFile)
                .Include(a => a.ApplicationType)
                .FirstOrDefault(a => a.ID == applicationId);
        }
        catch (Exception)
        {
            return objectSpace.GetObjectByKey<ApplicationProfileInstance>(applicationId);
        }
    }
}
