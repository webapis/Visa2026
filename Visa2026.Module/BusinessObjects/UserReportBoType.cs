namespace Visa2026.Module.BusinessObjects;

/// <summary>Root Business Object for user-defined Word report templates.</summary>
public enum UserReportBoType
{
    /// <summary>Application-level reports (cover letters, headers).</summary>
    Application = 0,

    /// <summary>Per-item reports (individual sanawy forms).</summary>
    ApplicationItem = 1,

    /// <summary>Registration/check-in/check-out reports.</summary>
    Registration = 2,

    /// <summary>Business trip related reports.</summary>
    BusinessTrip = 3,

    /// <summary>Person-centric reports.</summary>
    Person = 4
}
