using System;
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
        byte[] content)
    {
        if (objectSpace == null)
            return OfficerShellCaseProgressResult.Failed("ObjectSpace is required.");

        if (content == null || content.Length == 0)
            return OfficerShellCaseProgressResult.Failed("The uploaded file is empty.");

        var application = objectSpace.GetObjectByKey<ApplicationProfileInstance>(applicationId);
        if (application == null)
            return OfficerShellCaseProgressResult.Failed("ApplicationProfileInstance not found.");

        var latest = ApplicationProfileInstanceProgressHelper.GetLatest(application.ProgressHistory, objectSpace);
        if (latest == null)
            return OfficerShellCaseProgressResult.Failed("No progress history on this application.");

        if (!latest.IsMinistryDecisionStep)
            return OfficerShellCaseProgressResult.Failed("Ministry letter upload is only available on ministry decision steps.");

        var maxBytes = latest.MaxDocumentSizeInMB * 1024L * 1024L;
        if (content.LongLength > maxBytes)
            return OfficerShellCaseProgressResult.Failed($"The ministry letter exceeds the maximum allowed size of {latest.MaxDocumentSizeInMB} MB.");

        var file = latest.MinistryLetterFile ?? objectSpace.CreateObject<FileData>();
        file.FileName = fileName;
        file.Content = content;
        file.Size = content.Length;

        if (!DocumentFileUploadConstraints.TryValidate(file, out var validationError))
            return OfficerShellCaseProgressResult.Failed(validationError ?? "The ministry letter file is not valid.");

        latest.MinistryLetterFile = file;
        return OfficerShellCaseProgressResult.Succeeded();
    }

    public OfficerShellCaseProgressResult Advance(
        IObjectSpace objectSpace,
        Guid applicationId,
        string? stateCode,
        string? notesOnLatestStep)
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

        var progress = objectSpace.CreateObject<ApplicationProfileInstanceProgress>();
        progress.ApplicationProfileInstance = application;
        progress.Date = DateTime.Today;
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

        ApplicationLatestProgressSyncHelper.Sync(application, objectSpace);
        return OfficerShellCaseProgressResult.Succeeded(chosenCode);
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
                    .ThenInclude(p => p!.ProgressStateSettings)
                .Include(a => a.ApprovalLegProfile)
                    .ThenInclude(p => p!.MinistryLegs)
                .Include(a => a.ApprovalLegSnapshots)
                .Include(a => a.ProgressHistory)
                    .ThenInclude(p => p.State)
                .Include(a => a.ApplicationType)
                .FirstOrDefault(a => a.ID == applicationId);
        }
        catch (Exception)
        {
            return objectSpace.GetObjectByKey<ApplicationProfileInstance>(applicationId);
        }
    }
}
