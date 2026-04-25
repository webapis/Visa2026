using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using Microsoft.Extensions.DependencyInjection;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;

namespace Visa2026.Module.Controllers
{
    public class VisaListViewController : ObjectViewController<ListView, Visa>
    {
        private VisaFilterService _filterService;
        private VisaStateFilterService _stateFilterService;

        protected override void OnActivated()
        {
            base.OnActivated();
            _filterService = Application.ServiceProvider?.GetService<VisaFilterService>();
            _stateFilterService = Application.ServiceProvider?.GetService<VisaStateFilterService>();
            if (_filterService != null)
                _filterService.CriteriaRequested += OnCriteriaRequested;
            if (_stateFilterService != null)
                _stateFilterService.CriteriaRequested += OnStateCriteriaRequested;
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
            if (_stateFilterService != null)
            {
                _stateFilterService.CriteriaRequested -= OnStateCriteriaRequested;
                _stateFilterService = null;
            }
            View.CollectionSource.Criteria.Remove("NavFilter");
            View.CollectionSource.Criteria.Remove("NavPersonFilter");
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

            var (personIds, personCaption) = _stateFilterService?.TakeAndClear() ?? (Array.Empty<Guid>(), null);
            if (personIds.Count > 0)
            {
                View.CollectionSource.Criteria["NavPersonFilter"] =
                    new InOperator("Passport.Person.ID", personIds.Cast<object>().ToArray());
                if (!string.IsNullOrEmpty(personCaption))
                    View.Caption = personCaption;
            }
            else
            {
                View.CollectionSource.Criteria.Remove("NavPersonFilter");
            }
        }

        private void OnStateCriteriaRequested(IReadOnlyList<Guid> personIds, string caption)
        {
            if (personIds == null || personIds.Count == 0)
            {
                View.CollectionSource.Criteria["NavPersonFilter"] = CriteriaOperator.Parse("1 = 0");
            }
            else
            {
                View.CollectionSource.Criteria["NavPersonFilter"] =
                    new InOperator("Passport.Person.ID", personIds.Cast<object>().ToArray());
            }

            if (!string.IsNullOrEmpty(caption))
                View.Caption = caption;
        }
    }
}
