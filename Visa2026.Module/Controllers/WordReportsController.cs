using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Security;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.UserReports;
using Visa2026.Module.Localization;
using Visa2026.Module.Services.WordReports;

namespace Visa2026.Module.Controllers
{
    /// <summary>
    /// Single "Resminamalar" button on the Application detail view.
    /// Queues a background job; the zip is downloaded via <c>/api/WordReportBatches/{id}/zip</c>
    /// (same pattern as My PDF Jobs).
    /// </summary>
    public class WordReportsController : ViewController<DetailView>
    {
        private SimpleAction resminamalarAction;

        public WordReportsController()
        {
            TargetObjectType = typeof(Application);
            TargetViewType = ViewType.DetailView;

            resminamalarAction = new SimpleAction(this, "GenerateWordReports", "Reports");
            resminamalarAction.ImageName = "BO_FileAttachment";
            resminamalarAction.Execute += ResminamalarAction_Execute;
        }

        protected override void OnActivated()
        {
            base.OnActivated();
            UpdateActionState();
            View.CurrentObjectChanged += (_, _) => UpdateActionState();
        }

        private void UpdateActionState()
        {
            var application = View?.CurrentObject as Application;
            if (application == null)
            {
                resminamalarAction.Enabled["NoApplicableReports"] = false;
                return;
            }

            var definitions = Application.ServiceProvider.GetServices<IWordReportDefinition>();
            bool anySystem = definitions.Any(d => IsDefinitionApplicable(d, application));

            var visibilityService = Application.ServiceProvider.GetService<IUserReportVisibilityService>();
            bool anyUser = false;
            if (visibilityService != null)
                anyUser = UserReportTemplateVisibilityHelper.AnyVisibleActiveTemplate(
                    ObjectSpace, visibilityService, application);

            resminamalarAction.Enabled["NoApplicableReports"] = anySystem || anyUser;
        }

        private void ResminamalarAction_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            var application = (Application)e.CurrentObject;
            var applicationId = (Guid)ObjectSpace.GetKeyValue(application);

            var definitions = Application.ServiceProvider
                .GetServices<IWordReportDefinition>()
                .Where(d => IsDefinitionApplicable(d, application))
                .ToList();

            var userTemplates = new List<UserReportTemplate>();
            var visibilityService = Application.ServiceProvider.GetService<IUserReportVisibilityService>();
            if (visibilityService != null)
            {
                userTemplates = UserReportTemplateVisibilityHelper.GetVisibleActiveTemplates(
                    ObjectSpace, visibilityService, application);
            }

            if (definitions.Count == 0 && userTemplates.Count == 0)
            {
                Application.ShowViewStrategy.ShowMessage(
                    VisaUiMessages.Get("WordReports.NoApplicableReports"),
                    InformationType.Warning);
                return;
            }

            var factory = Application.ServiceProvider.GetRequiredService<INonSecuredObjectSpaceFactory>();
            using (var os = factory.CreateNonSecuredObjectSpace<WordReportGenerationBatch>())
            {
                var batch = os.CreateObject<WordReportGenerationBatch>();
                batch.RequestedBy = SecuritySystem.CurrentUserName;
                batch.ApplicationID = applicationId;
                batch.TotalReports = definitions.Count + userTemplates.Count;
                batch.ProcessedReports = 0;
                batch.Status = WordReportGenerationBatchStatus.Queued;
                os.CommitChanges();
            }

            Application.ShowViewStrategy.ShowMessage(
                VisaUiMessages.Format("WordReports.QueuedSuccess", definitions.Count + userTemplates.Count),
                InformationType.Success);
        }

        private static bool IsDefinitionApplicable(IWordReportDefinition def, Application application)
        {
            var names = def.ApplicableApplicationTypeNames;
            if (names != null && names.Length > 0)
            {
                var appTypeName = application.ApplicationType?.Name;
                if (appTypeName == null || !names.Contains(appTypeName, StringComparer.Ordinal))
                    return false;
            }

            return def.IsApplicable(application);
        }
    }
}
