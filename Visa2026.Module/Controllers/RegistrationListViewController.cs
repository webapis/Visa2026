using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;

namespace Visa2026.Module.Controllers
{
    /// <summary>
    /// Applies visa-based navigation filters on the standalone <see cref="ApplicationItem"/> list only.
    /// Must not filter nested <c>Application.ApplicationItems</c> collections on Application detail views.
    /// </summary>
    public class RegistrationListViewController : ObjectViewController<ListView, ApplicationItem>
    {
        private RegistrationStateFilterService _filterService;
        private ILogger<RegistrationListViewController> _logger;

        protected override void OnActivated()
        {
            base.OnActivated();
            if (!IsStandaloneRegistrationListView())
                return;

            _filterService = Application.ServiceProvider?.GetService<RegistrationStateFilterService>();
            _logger = Application.ServiceProvider?.GetService<ILogger<RegistrationListViewController>>();
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

            if (IsStandaloneRegistrationListView())
                View.CollectionSource.Criteria.Remove("NavFilter");

            base.OnDeactivated();
        }

        private bool IsStandaloneRegistrationListView() =>
            View.CollectionSource is not PropertyCollectionSource;

        private void OnCriteriaRequested(IReadOnlyList<Guid> visaIds, string caption, string stateKey)
        {
            if (!IsStandaloneRegistrationListView())
                return;

            ApplyVisaFilter(visaIds, stateKey);
            if (!string.IsNullOrEmpty(caption))
                View.Caption = caption;
            _logger?.LogInformation(
                "V06 registration filter requested state={StateKey} caption={Caption} count={Count} visaIds={VisaIds}",
                stateKey ?? string.Empty,
                caption ?? string.Empty,
                visaIds?.Count ?? 0,
                FormatIds(visaIds));
        }

        private void ApplyPendingFilter()
        {
            if (!IsStandaloneRegistrationListView() || _filterService == null)
                return;

            var pending = _filterService.TakeAndClear();
            if (pending == null)
                return;

            var (visaIds, caption, stateKey) = pending.Value;
            ApplyVisaFilter(visaIds, stateKey);
            if (!string.IsNullOrEmpty(caption))
                View.Caption = caption;
            _logger?.LogInformation(
                "V06 registration pending filter state={StateKey} caption={Caption} count={Count} visaIds={VisaIds}",
                stateKey ?? string.Empty,
                caption ?? string.Empty,
                visaIds?.Count ?? 0,
                FormatIds(visaIds));
        }

        private void ApplyVisaFilter(IReadOnlyList<Guid> visaIds, string stateKey)
        {
            if (visaIds == null || visaIds.Count == 0)
            {
                View.CollectionSource.Criteria["NavFilter"] = CriteriaOperator.Parse("1 = 0");
                _logger?.LogInformation("V06 registration applied empty criteria (1=0).");
                return;
            }

            // Keep navigation rows aligned with V-06 registration prerequisite:
            // registration must target one of snapshot visa owners and be a checkout registration.
            var visaCriteria = new InOperator("CurrentVisa.ID", visaIds.Cast<object>().ToArray());
            CriteriaOperator checkoutCriteria = new BinaryOperator("Application.ApplicationType.Name", "App_Reg_Check_Out");

            if (string.Equals(stateKey, "Visa|ExpiredCheckedOut", StringComparison.OrdinalIgnoreCase))
            {
                checkoutCriteria = GroupOperator.And(
                    checkoutCriteria,
                    new BinaryOperator("Application.CurrentState.State.Code", "PROCESS_ISSUED"));
            }
            else if (string.Equals(stateKey, "Visa|ExpiredOnCheckOutProcess", StringComparison.OrdinalIgnoreCase))
            {
                checkoutCriteria = GroupOperator.And(
                    checkoutCriteria,
                    new BinaryOperator("Application.CurrentState.State.Code", "PROCESS_ISSUED", BinaryOperatorType.NotEqual));
            }

            View.CollectionSource.Criteria["NavFilter"] = GroupOperator.And(visaCriteria, checkoutCriteria);
            _logger?.LogInformation(
                "V06 registration applied criteria state={StateKey} count={Count} visaIds={VisaIds}",
                stateKey ?? string.Empty,
                visaIds.Count,
                FormatIds(visaIds));
        }

        private static string FormatIds(IReadOnlyList<Guid> ids)
        {
            if (ids == null || ids.Count == 0)
                return "[]";

            var preview = ids.Take(10).Select(x => x.ToString("N")).ToList();
            var suffix = ids.Count > 10 ? $", ... (+{ids.Count - 10})" : string.Empty;
            return $"[{string.Join(", ", preview)}{suffix}]";
        }
    }
}
