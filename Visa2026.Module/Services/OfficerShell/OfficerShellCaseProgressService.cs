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
        string? submissionDocumentNumber = null,
        IReadOnlyList<(string Name, byte[] Content)>? additionalFiles = null)
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
            && (latest == null || CanStoreProgressFile(progress)))
        {
            var letterResult = AttachMinistryLetter(objectSpace, progress, letterFileName ?? "progress-file.pdf", letterContent);
            if (!letterResult.Success)
                return letterResult;
        }

        if (additionalFiles != null)
        {
            var slot = ApplicationMigrationSubmission.IsState(chosenCode)
                ? ApplicationProfileInstanceProgressFiles.SubmissionDocument
                : ApplicationProfileInstanceProgressFiles.MinistryLetter;
            foreach (var extra in additionalFiles)
            {
                if (extra.Content == null || extra.Content.Length == 0)
                    continue;
                var extraResult = AddFileToRow(objectSpace, progress, slot, extra.Name, extra.Content);
                if (!extraResult.Success)
                    return extraResult;
            }
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
            DeleteStoredFiles(objectSpace, row);
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

        if (created)
            ApplicationProfileInstanceProgressOrderHelper.AssignTimelineOrders(application.ProgressHistory.ToList());

        ApplicationLatestProgressSyncHelper.Sync(application, objectSpace);
        return OfficerShellCaseProgressResult.Succeeded();
    }

    public OfficerShellCaseProgressResult CorrectStep(
        IObjectSpace objectSpace,
        Guid applicationId,
        Guid progressId,
        DateTime stepDate,
        string? submissionDocumentNumber,
        string? resultNumber = null)
    {
        if (objectSpace == null)
            return OfficerShellCaseProgressResult.Failed("ObjectSpace is required.");
        if (progressId == Guid.Empty)
            return OfficerShellCaseProgressResult.Failed("Progress step was not found.");

        var application = LoadApplicationForAdvance(objectSpace, applicationId);
        if (application == null)
            return OfficerShellCaseProgressResult.Failed("ApplicationProfileInstance not found.");

        var row = application.ProgressHistory?.FirstOrDefault(p => p.ID == progressId);
        if (row == null)
            return OfficerShellCaseProgressResult.Failed("Progress step was not found.");

        var steps = ApplicationWorkspaceProgressTimeline.Build(
            application,
            application.ApplicationProfile,
            default,
            objectSpace);
        var step = steps.FirstOrDefault(s => s.CanCorrectDate && s.DateProgressId == progressId);
        if (step == null)
            return OfficerShellCaseProgressResult.Failed("This step cannot be corrected.");

        if (!ApplicationProfileInstanceProgressStepDates.TryValidate(stepDate, step.EarliestDate, step.LatestDate, out var dateError))
            return OfficerShellCaseProgressResult.Failed(dateError ?? VisaUiMessages.Get("ApplicationProfileInstance.Workspace.DateRequired"));

        row.Date = stepDate.Date;
        if (step.CanEditResultNumber)
        {
            if (!ApplicationProgressResultNumberHelper.TryNormalize(
                    resultNumber,
                    required: true,
                    out var normalizedResultNumber,
                    out var resultNumberError))
            {
                return OfficerShellCaseProgressResult.Failed(
                    resultNumberError ?? VisaUiMessages.Get("ApplicationProfileInstance.Workspace.ResultNumberRequired"));
            }

            row.ResultNumber = normalizedResultNumber;
        }

        if (step.CanEditSubmissionNumber)
        {
            if (!ApplicationMigrationSubmission.TryNormalizeDocumentNumber(submissionDocumentNumber, out var number, out var numberError))
                return OfficerShellCaseProgressResult.Failed(numberError ?? VisaUiMessages.Get("ApplicationProfileInstance.Workspace.SubmissionDocumentNumberRequired"));
            row.SubmissionDocumentNumber = number;
        }

        ApplicationLatestProgressSyncHelper.Sync(application, objectSpace);
        return OfficerShellCaseProgressResult.Succeeded();
    }

    public OfficerShellCaseProgressResult AddProgressFile(
        IObjectSpace objectSpace,
        Guid applicationId,
        Guid progressId,
        int slot,
        string fileName,
        byte[] content)
    {
        var row = FindProgressRow(objectSpace, applicationId, progressId, out var error);
        if (row == null)
            return OfficerShellCaseProgressResult.Failed(error ?? "Progress step was not found.");
        return AddFileToRow(objectSpace, row, slot, fileName, content);
    }

    public OfficerShellCaseProgressResult ReplaceProgressFile(
        IObjectSpace objectSpace,
        Guid applicationId,
        Guid progressId,
        Guid fileId,
        string fileName,
        byte[] content)
    {
        var row = FindProgressRow(objectSpace, applicationId, progressId, out var error);
        if (row == null)
            return OfficerShellCaseProgressResult.Failed(error ?? "Progress step was not found.");
        if (fileId == Guid.Empty)
            return OfficerShellCaseProgressResult.Failed("File was not found.");

        var target = FindStoredFile(row, fileId);
        if (target == null)
            return OfficerShellCaseProgressResult.Failed("File was not found.");

        var check = ValidateUpload(row, fileName, content, out var prepared);
        if (!check.Success || prepared == null)
            return check;

        target.FileName = prepared.FileName;
        target.Content = prepared.Content;
        target.Size = prepared.Size;
        return OfficerShellCaseProgressResult.Succeeded();
    }

    public OfficerShellCaseProgressResult RemoveProgressFile(
        IObjectSpace objectSpace,
        Guid applicationId,
        Guid progressId,
        Guid fileId)
    {
        if (objectSpace == null)
            return OfficerShellCaseProgressResult.Failed("ObjectSpace is required.");

        var row = FindProgressRow(objectSpace, applicationId, progressId, out var error);
        if (row == null)
            return OfficerShellCaseProgressResult.Failed(error ?? "Progress step was not found.");
        if (fileId == Guid.Empty)
            return OfficerShellCaseProgressResult.Failed("File was not found.");

        if (row.MinistryLetterFile?.ID == fileId)
            return RemovePrimary(objectSpace, row, ApplicationProfileInstanceProgressFiles.MinistryLetter);
        if (row.SubmissionDocumentFile?.ID == fileId)
            return RemovePrimary(objectSpace, row, ApplicationProfileInstanceProgressFiles.SubmissionDocument);

        var extra = row.Files?.FirstOrDefault(item => item.File != null && item.File.ID == fileId);
        if (extra == null)
            return OfficerShellCaseProgressResult.Failed("File was not found.");

        if (extra.File != null)
            objectSpace.Delete(extra.File);
        objectSpace.Delete(extra);
        row.Files?.Remove(extra);
        return OfficerShellCaseProgressResult.Succeeded();
    }

    private ApplicationProfileInstanceProgress? FindProgressRow(
        IObjectSpace objectSpace,
        Guid applicationId,
        Guid progressId,
        out string? error)
    {
        error = null;
        if (objectSpace == null)
        {
            error = "ObjectSpace is required.";
            return null;
        }

        var application = LoadApplicationForAdvance(objectSpace, applicationId);
        if (application == null)
        {
            error = "ApplicationProfileInstance not found.";
            return null;
        }

        var row = application.ProgressHistory?.FirstOrDefault(p => p.ID == progressId);
        if (row == null)
            error = "Progress step was not found.";
        return row;
    }

    private static OfficerShellCaseProgressResult AddFileToRow(
        IObjectSpace objectSpace,
        ApplicationProfileInstanceProgress row,
        int slot,
        string fileName,
        byte[] content)
    {
        if (!CanStoreSlot(row, slot))
            return OfficerShellCaseProgressResult.Failed("Files cannot be stored on this step.");

        var check = ValidateUpload(row, fileName, content, out var prepared);
        if (!check.Success || prepared == null)
            return check;

        var file = objectSpace.CreateObject<FileData>();
        file.FileName = prepared.FileName;
        file.Content = prepared.Content;
        file.Size = prepared.Size;

        var primary = ApplicationProfileInstanceProgressFiles.PrimaryFile(row, slot);
        var hasPrimary = primary != null && primary.Size > 0;
        var hasExtra = row.Files?.Any(item => item != null && item.Slot == slot && item.File != null && item.File.Size > 0) == true;
        if (!hasPrimary && !hasExtra)
        {
            ApplicationProfileInstanceProgressFiles.AssignPrimary(row, slot, file);
            return OfficerShellCaseProgressResult.Succeeded();
        }

        var item = objectSpace.CreateObject<ApplicationProfileInstanceProgressFile>();
        item.Progress = row;
        item.ProgressId = row.ID;
        item.File = file;
        item.Slot = slot;
        item.SortOrder = NextSortOrder(row, slot);
        row.Files ??= new System.Collections.ObjectModel.ObservableCollection<ApplicationProfileInstanceProgressFile>();
        if (!row.Files.Contains(item))
            row.Files.Add(item);
        return OfficerShellCaseProgressResult.Succeeded();
    }

    private static OfficerShellCaseProgressResult RemovePrimary(
        IObjectSpace objectSpace,
        ApplicationProfileInstanceProgress row,
        int slot)
    {
        var primary = ApplicationProfileInstanceProgressFiles.PrimaryFile(row, slot);
        if (primary != null)
            objectSpace.Delete(primary);
        ApplicationProfileInstanceProgressFiles.AssignPrimary(row, slot, null);

        var extra = row.Files?
            .Where(item => item != null && item.Slot == slot && item.File != null && item.File.Size > 0)
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.ID)
            .FirstOrDefault();
        if (extra?.File == null)
            return OfficerShellCaseProgressResult.Succeeded();

        var content = extra.File.Content;
        if (content == null || content.Length == 0)
            return OfficerShellCaseProgressResult.Failed("The remaining file could not be read.");

        var promoted = objectSpace.CreateObject<FileData>();
        promoted.FileName = string.IsNullOrWhiteSpace(extra.File.FileName) ? "file" : extra.File.FileName;
        promoted.Content = content;
        promoted.Size = content.Length;
        ApplicationProfileInstanceProgressFiles.AssignPrimary(row, slot, promoted);
        objectSpace.Delete(extra.File);
        objectSpace.Delete(extra);
        row.Files?.Remove(extra);
        return OfficerShellCaseProgressResult.Succeeded();
    }

    private static void DeleteStoredFiles(IObjectSpace objectSpace, ApplicationProfileInstanceProgress row)
    {
        if (row.Files != null)
        {
            foreach (var item in row.Files.ToList())
            {
                row.Files.Remove(item);
                if (item.File != null)
                    objectSpace.Delete(item.File);
                objectSpace.Delete(item);
            }
        }

        if (row.MinistryLetterFile != null)
            objectSpace.Delete(row.MinistryLetterFile);
        if (row.SubmissionDocumentFile != null)
            objectSpace.Delete(row.SubmissionDocumentFile);
    }

    private static FileData? FindStoredFile(ApplicationProfileInstanceProgress row, Guid fileId)
    {
        if (row.MinistryLetterFile?.ID == fileId)
            return row.MinistryLetterFile;
        if (row.SubmissionDocumentFile?.ID == fileId)
            return row.SubmissionDocumentFile;
        return row.Files?.FirstOrDefault(item => item.File != null && item.File.ID == fileId)?.File;
    }

    private static int NextSortOrder(ApplicationProfileInstanceProgress row, int slot)
    {
        var max = 0;
        if (row.Files == null)
            return 1;
        foreach (var item in row.Files)
        {
            if (item != null && item.Slot == slot && item.SortOrder > max)
                max = item.SortOrder;
        }

        return max + 1;
    }

    private static bool CanStoreSlot(ApplicationProfileInstanceProgress row, int slot)
    {
        if (slot == ApplicationProfileInstanceProgressFiles.SubmissionDocument)
            return row.IsMigrationSubmissionStep;
        return CanStoreProgressFile(row);
    }

    private static OfficerShellCaseProgressResult ValidateUpload(
        ApplicationProfileInstanceProgress row,
        string fileName,
        byte[] content,
        out FileData? prepared)
    {
        prepared = null;
        if (content == null || content.Length == 0)
            return OfficerShellCaseProgressResult.Failed("The uploaded file is empty.");

        var maxBytes = row.MaxDocumentSizeInMB * 1024L * 1024L;
        if (content.LongLength > maxBytes)
            return OfficerShellCaseProgressResult.Failed($"The file exceeds the maximum allowed size of {row.MaxDocumentSizeInMB} MB.");

        var name = string.IsNullOrWhiteSpace(fileName) ? "file" : fileName.Trim();
        prepared = new FileData
        {
            FileName = name,
            Content = content,
            Size = content.Length,
        };
        if (!DocumentFileUploadConstraints.TryValidate(prepared, out var validationError))
        {
            prepared = null;
            return OfficerShellCaseProgressResult.Failed(validationError ?? "The file is not valid.");
        }

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
            return OfficerShellCaseProgressResult.Failed("File upload is only available on office preparation, an approval leg, or the migration service step.");

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

    private static bool CanStoreProgressFile(ApplicationProfileInstanceProgress row)
    {
        if (row.IsMinistryDecisionStep)
            return true;

        var code = row.State?.Code;
        if (ApplicationWorkspaceProgressAdvancePreview.IsOfficeSubmitted(
                ApplicationWorkspaceProgressTimeline.OfficeKey,
                code)
            || ApplicationMigrationSlaHelper.IsMigrationServiceProcessStartedStep(code)
            || string.Equals(code, ApplicationProfileInstanceProgressStateCodes.ProcessCancelled, StringComparison.OrdinalIgnoreCase)
            || string.Equals(code, ApplicationProfileInstanceProgressStateCodes.ProcessIssued, StringComparison.OrdinalIgnoreCase)
            || string.Equals(code, ApplicationProfileInstanceProgressStateCodes.ProcessRejected, StringComparison.OrdinalIgnoreCase))
            return true;

        // Office (and similar) rows that already hold letter files stay editable even if the state code is unexpected.
        if (row.MinistryLetterFile != null && row.MinistryLetterFile.Size > 0)
            return true;

        return row.Files?.Any(item =>
                   item != null
                   && item.Slot == ApplicationProfileInstanceProgressFiles.MinistryLetter
                   && item.File != null
                   && item.File.Size > 0)
               == true;
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
                .Include(a => a.ProgressHistory)
                    .ThenInclude(p => p.SubmissionDocumentFile)
                .Include(a => a.ProgressHistory)
                    .ThenInclude(p => p.Files)
                        .ThenInclude(f => f.File)
                .Include(a => a.ApplicationType)
                .FirstOrDefault(a => a.ID == applicationId);
        }
        catch (Exception)
        {
            return objectSpace.GetObjectByKey<ApplicationProfileInstance>(applicationId);
        }
    }
}
