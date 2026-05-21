using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.Persistent.Base;
using Microsoft.Extensions.DependencyInjection;
using AppBO = Visa2026.Module.BusinessObjects.Application;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;

namespace Visa2026.Module.Controllers
{
    public class VisaExtensionStatusController : ObjectViewController<ListView, VisaExtensionStatus>
    {
        private readonly SimpleAction _openApplicationAction;
        private VisaExtFilterService _filterService;

        public VisaExtensionStatusController()
        {
            _openApplicationAction = new SimpleAction(this, "OpenApplicationFromVisaStatus", PredefinedCategory.View)
            {
                ImageName = "Action_Edit_Object",
                SelectionDependencyType = SelectionDependencyType.RequireSingleObject,
                ToolTip = "Open the full Application record for the selected row",
            };
            _openApplicationAction.Execute += OnOpenApplication;
        }

        protected override void OnActivated()
        {
            base.OnActivated();
            _filterService = Application.ServiceProvider?.GetService<VisaExtFilterService>();
            if (_filterService != null)
                _filterService.CriteriaRequested += OnCriteriaRequested;
        }

        protected override void OnViewControlsCreated()
        {
            base.OnViewControlsCreated();
            ApplyPendingFilter();
        }

        protected override void OnDeactivated()
        {
            if (_filterService != null)
            {
                _filterService.CriteriaRequested -= OnCriteriaRequested;
                _filterService = null;
            }
            View.CollectionSource.Criteria.Remove("NavFilter");
            base.OnDeactivated();
        }

        private void OnCriteriaRequested(string criteria, string caption)
        {
            if (!string.IsNullOrEmpty(criteria))
                View.CollectionSource.Criteria["NavFilter"] = CriteriaOperator.TryParse(criteria);
            else
                View.CollectionSource.Criteria.Remove("NavFilter");

            if (!string.IsNullOrEmpty(caption))
                View.Caption = caption;
        }

        private void ApplyPendingFilter()
        {
            var (criteria, caption) = _filterService?.TakeAndClear() ?? (null, null);

            if (!string.IsNullOrEmpty(criteria))
            {
                View.CollectionSource.Criteria["NavFilter"] = CriteriaOperator.TryParse(criteria);
                if (!string.IsNullOrEmpty(caption))
                    View.Caption = caption;
            }
        }

        private void OnOpenApplication(object sender, SimpleActionExecuteEventArgs e)
        {
            var status = View.CurrentObject as VisaExtensionStatus;
            if (status?.ApplicationID == null) return;

            var os = Application.CreateObjectSpace(typeof(AppBO));
            var app = os.GetObjectByKey<AppBO>(status.ApplicationID.Value);
            if (app == null) return;

            var svp = new ShowViewParameters
            {
                CreatedView = Application.CreateDetailView(os, app),
                TargetWindow = TargetWindow.NewWindow,
            };
            Application.ShowViewStrategy.ShowView(svp, new ShowViewSource(Frame, _openApplicationAction));
        }
    }
}
