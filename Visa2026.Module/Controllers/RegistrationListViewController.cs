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
    public class RegistrationListViewController : ObjectViewController<ListView, Registration>
    {
        private RegistrationStateFilterService _filterService;

        protected override void OnActivated()
        {
            base.OnActivated();
            _filterService = Application.ServiceProvider?.GetService<RegistrationStateFilterService>();
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

        private void OnCriteriaRequested(IReadOnlyList<Guid> personIds, string caption)
        {
            ApplyPersonFilter(personIds);
            if (!string.IsNullOrEmpty(caption))
                View.Caption = caption;
        }

        private void ApplyPendingFilter()
        {
            var (personIds, caption) = _filterService?.TakeAndClear() ?? (Array.Empty<Guid>(), null);
            ApplyPersonFilter(personIds);
            if (!string.IsNullOrEmpty(caption))
                View.Caption = caption;
        }

        private void ApplyPersonFilter(IReadOnlyList<Guid> personIds)
        {
            if (personIds == null || personIds.Count == 0)
            {
                View.CollectionSource.Criteria["NavFilter"] = CriteriaOperator.Parse("1 = 0");
                return;
            }

            View.CollectionSource.Criteria["NavFilter"] = new InOperator("Person.ID", personIds.Cast<object>().ToArray());
        }
    }
}
