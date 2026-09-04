namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Legacy DB column only. Resminamalar visibility uses <see cref="UserReportTemplate.ApplicableTypeLinks"/>,
/// <see cref="UserReportTemplate.ApplicableGroupLinks"/>, <see cref="UserReportTemplate.ApplicableProjectContractLinks"/>,
/// and <see cref="UserReportTemplate.VisibilityCriteria"/> instead.
/// </summary>
[System.Obsolete("Use Applicable ApplicationProfileInstance Types, Applicable ApplicationProfileInstance Type Groups, Applicable Project Contracts, and Visibility Criteria on UserReportTemplate.")]
public enum ApplicabilityMode
{
    /// <summary>Visible for all ApplicationProfileInstance Types.</summary>
    AllTypes = 0,

    /// <summary>Filter by type/group links; when both lists are empty, all application types match.</summary>
    SpecificTypes = 1,

    /// <summary>When data-driven condition is met (criteria: [ApplicationItems].Count() > 0).</summary>
    DataDriven = 2
}
