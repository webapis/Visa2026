using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using Microsoft.Extensions.DependencyInjection;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;

namespace Visa2026.Module.Controllers
{
    public class VisaCancelExtStatusController : ObjectViewController<ListView, VisaCancelExtStatus>
    {
        private VisaCancelExtFilterService _filterService;

        protected override void OnActivated()
        {
            base.OnActivated();
            _filterService = Application.ServiceProvider?.GetService<VisaCancelExtFilterService>();
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
    }
}
