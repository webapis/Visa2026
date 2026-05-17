namespace Visa2026.Module.BusinessObjects;

/// <summary>When should a user-defined template appear in Resminamalar.</summary>
public enum ApplicabilityMode
{
    /// <summary>Visible for all Application Types.</summary>
    AllTypes = 0,

    /// <summary>Only for specific Application Types (criteria: [ApplicationType.Name] In (...)).</summary>
    SpecificTypes = 1,

    /// <summary>When data-driven condition is met (criteria: [ApplicationItems].Count() > 0).</summary>
    DataDriven = 2
}
