namespace Visa2026.Module.BusinessObjects;

/// <summary>Root Business Object for user-defined Word report templates.</summary>
public enum UserReportBoType
{
    /// <summary>Application-level reports (cover letters, headers).</summary>
    Application = 0,

    /// <summary>Per-item reports (individual sanawy forms, registration lines, business-trip lines).</summary>
    ApplicationItem = 1,

    /// <summary>Person-centric reports.</summary>
    Person = 2
}
