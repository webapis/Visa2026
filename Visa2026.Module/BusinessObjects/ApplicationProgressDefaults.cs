namespace Visa2026.Module.BusinessObjects
{
    /// <summary>
    /// Legacy office codes. New applications do not seed a prep progress row —
    /// office is implied until the first explicit step.
    /// </summary>
    public static class ApplicationProgressDefaults
    {
        public const string InitialStateCode = "IS_BEING_PREPARED";
        public const string InitialLocationCode = "AT_OFFICE";
    }
}