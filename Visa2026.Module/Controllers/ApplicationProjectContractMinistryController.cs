using DevExpress.ExpressApp;

using DevExpress.Persistent.Base;

using Visa2026.Module.BusinessObjects;

using Visa2026.Module.Localization;



namespace Visa2026.Module.Controllers;



/// <summary>

/// Snapshots ministry legs when <see cref="BusinessObjects.ApplicationProfileInstance.ApprovalLegProfile"/> changes;

/// locks header fields after progress.

/// </summary>

public sealed class ApplicationProjectContractMinistryController : ObjectViewController<DetailView, BusinessObjects.ApplicationProfileInstance>

{

    protected override void OnActivated()

    {

        base.OnActivated();

        ObjectSpace.ObjectChanged += ObjectSpace_ObjectChanged;

        ObjectSpace.Committed += ObjectSpace_Committed;

    }



    protected override void OnDeactivated()

    {

        ObjectSpace.ObjectChanged -= ObjectSpace_ObjectChanged;

        ObjectSpace.Committed -= ObjectSpace_Committed;

        base.OnDeactivated();

    }



    private void ObjectSpace_Committed(object? sender, EventArgs e) => View?.Refresh();



    private void ObjectSpace_ObjectChanged(object sender, ObjectChangedEventArgs e)

    {

        if (e.Object is ApplicationProfileInstanceProgress progress

            && progress.ApplicationProfileInstance != null

            && ReferenceEquals(progress.ApplicationProfileInstance, ViewCurrentObject)

            && e.PropertyName is nameof(ApplicationProfileInstanceProgress.State))

        {

            View?.Refresh();

            return;

        }



        if (e.Object is not BusinessObjects.ApplicationProfileInstance application)

            return;



        if (e.PropertyName == nameof(BusinessObjects.ApplicationProfileInstance.ApprovalLegProfile))

        {

            HandleApprovalLegProfileChanged(application, e.OldValue as ApprovalLegProfile, e.NewValue as ApprovalLegProfile);

            return;

        }



        if (e.PropertyName == nameof(BusinessObjects.ApplicationProfileInstance.ProjectContract))

            HandleProjectContractChanged(application, e.OldValue as ProjectContract, e.NewValue as ProjectContract);

    }



    private void HandleApprovalLegProfileChanged(

        BusinessObjects.ApplicationProfileInstance application,

        ApprovalLegProfile? previousProfile,

        ApprovalLegProfile? newProfile)

    {

        if (ReferenceEquals(previousProfile, newProfile))

            return;



        if (ApplicationProfileInstanceProgressProfileResolver.IsApplicationLockedAfterOfficePreparation(application, ObjectSpace))

        {

            application.ApprovalLegProfile = previousProfile;

            Application.ShowViewStrategy.ShowMessage(

                VisaUiMessages.Get("Application.FieldsLockedAfterProgress"),

                InformationType.Warning,

                8000,

                InformationPosition.Top);

            return;

        }



        ApprovalLegProfileMinistryHelper.ApplySnapshot(ObjectSpace, application, newProfile);

        if (application.ProjectContract != null
            && newProfile != null
            && (application.ProjectContract.ApprovalLegProfile == null
                || application.ProjectContract.ApprovalLegProfile.ID != newProfile.ID))
        {
            application.ProjectContract = null;
        }



        if (!ApplicationProfileInstanceProgressProfileResolver.HasAnyProgressHistory(application, ObjectSpace))

            return;



        Application.ShowViewStrategy.ShowMessage(

            VisaUiMessages.Get("Application.ApprovalLegProfileChangedAfterProgress"),

            InformationType.Warning,

            8000,

            InformationPosition.Top);



        if (ApplicationProfileInstanceProgressProfileResolver.WouldMinistryDepthChange(application, previousProfile, newProfile))

        {

            var previousLabel = ApplicationProfileInstanceProgressProfileResolver.FormatMinistryLegCountLabel(

                ApprovalLegProfileMinistryHelper.GetLegCount(previousProfile));

            var newLabel = ApplicationProfileInstanceProgressProfileResolver.FormatMinistryLegCountLabel(

                ApprovalLegProfileMinistryHelper.GetLegCount(newProfile));

            Application.ShowViewStrategy.ShowMessage(

                VisaUiMessages.Format(

                    "Application.ApprovalLegProfileMinistryDepthChanged",

                    previousLabel,

                    newLabel),

                InformationType.Warning,

                8000,

                InformationPosition.Top);

        }

    }



    private void HandleProjectContractChanged(

        BusinessObjects.ApplicationProfileInstance application,

        ProjectContract? previousContract,

        ProjectContract? newContract)

    {

        if (ReferenceEquals(previousContract, newContract))

            return;



        if (ApplicationProfileInstanceProgressProfileResolver.IsApplicationLockedAfterOfficePreparation(application, ObjectSpace))

        {

            application.ProjectContract = previousContract;

            Application.ShowViewStrategy.ShowMessage(

                VisaUiMessages.Get("Application.FieldsLockedAfterProgress"),

                InformationType.Warning,

                8000,

                InformationPosition.Top);

            return;

        }



        if (!ApplicationProfileInstanceProgressProfileResolver.HasAnyProgressHistory(application, ObjectSpace))

            return;



        Application.ShowViewStrategy.ShowMessage(

            VisaUiMessages.Get("Application.ProjectContractChangedAfterProgress"),

            InformationType.Warning,

            8000,

            InformationPosition.Top);

    }

}


