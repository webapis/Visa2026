using System;
using System.ComponentModel;
using DevExpress.Data.Filtering;
using DevExpress.Data.Filtering.Helpers;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.ApplicationProfilePicker;

public static class ApplicationProfileApplicabilityHelper
{
    public static bool IsProfileSelectable(
        ApplicationProfile profile,
        ApplicationProfileInstance? applicabilityProbe,
        ApplicationProfileInstanceProgressRouteKind? progressRouteFilter)
    {
        if (profile == null || !profile.IsActive)
            return false;

        // Seretmezlik (and any RequireTarget* profile) appears on both Via and Direct create lists;
        // CreationProgressRoute on the new instance comes from the list the officer opened.
        if (progressRouteFilter.HasValue
            && profile.ProgressRoute != progressRouteFilter.Value
            && !ApplicationProfileConfigurationResolver.IsSeretmezlikProfile(profile)
            && !ApplicationProfileConfigurationResolver.IsSubmitToOrganizationProfile(profile))
            return false;

        if (string.IsNullOrWhiteSpace(profile.ApplicabilityCriteria))
            return true;

        if (applicabilityProbe == null)
            return false;

        return EvaluateCriteriaOnInstance(profile.ApplicabilityCriteria, applicabilityProbe);
    }

    private static bool EvaluateCriteriaOnInstance(string criteriaString, object instance)
    {
        if (instance == null || string.IsNullOrWhiteSpace(criteriaString))
            return false;

        try
        {
            var criteria = CriteriaOperator.Parse(criteriaString);
            var evaluator = new ExpressionEvaluator(TypeDescriptor.GetProperties(instance), criteria, false);
            var value = evaluator.Evaluate(instance);
            return value is bool b && b;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
