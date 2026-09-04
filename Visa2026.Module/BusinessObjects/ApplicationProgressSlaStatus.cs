namespace Visa2026.Module.BusinessObjects;

/// <summary>SLA evaluation for the current <see cref="ApplicationProfileInstanceProgress"/> ministry review step.</summary>
public enum ApplicationProfileInstanceProgressSlaStatus
{
    None = 0,
    Ok = 1,
    Warning = 2,
    Overdue = 3
}
