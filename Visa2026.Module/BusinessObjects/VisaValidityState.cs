namespace Visa2026.Module.BusinessObjects
{
    /// <summary>
    /// Primary validity state of a <see cref="Visa"/> (flags + expiration window).
    /// Process/workflow states (extension in progress, at ministry, etc.) are separate dimensions.
    /// </summary>
    public enum VisaValidityState
    {
        Valid = 0,
        Expiring = 1,
        Expired = 2,
        Changed = 3,
        Cancelled = 4
    }
}
