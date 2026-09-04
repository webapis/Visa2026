using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.EFCore;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.ApplicationProfileWizard;

public static class ApplicationProfileWizardPersistHelper
{
    public static void Save(IObjectSpace objectSpace, ApplicationProfile profile)
    {
        ArgumentNullException.ThrowIfNull(objectSpace);
        ArgumentNullException.ThrowIfNull(profile);
        if (objectSpace.IsDisposed)
            throw new UserFriendlyException("The configuration session expired. Close this tab and open Configure again.");

        ApplicationProfileLockHelper.EnsureConfigurationEditable(profile, objectSpace);
        ApplicationProfileRegistrationKindHelper.ApplyRegistrationPersonDefaults(profile);

        if (objectSpace is EFCoreObjectSpace { DbContext: { } dbContext })
            dbContext.ChangeTracker.DetectChanges();

        objectSpace.SetModified(profile);
        try
        {
            objectSpace.CommitChanges();
        }
        catch (UserFriendlyException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new UserFriendlyException(FormatCommitError(ex));
        }
    }

    public static string FormatCommitError(Exception ex)
    {
        var text = (ex.GetBaseException().Message ?? ex.Message) ?? string.Empty;
        if (text.Contains("IX_ApplicationProfiles", StringComparison.OrdinalIgnoreCase)
            || (text.Contains("unique", StringComparison.OrdinalIgnoreCase)
                && text.Contains("Code", StringComparison.OrdinalIgnoreCase)))
        {
            return "A profile with this Code already exists. Choose a different Code and save again.";
        }

        return string.IsNullOrWhiteSpace(text)
            ? "Could not save the Application Profile."
            : text;
    }
}