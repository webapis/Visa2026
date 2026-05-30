namespace Visa2026.Module.BusinessObjects
{
    /// <summary>
    /// Person-scoped history rows that expose <see cref="IsActive"/> for expiration/state logic
    /// and optional current-item maintenance via <see cref="CurrentPersonItemSync"/>.
    /// </summary>
    public interface ICurrentPersonItem
    {
        bool IsActive { get; set; }
    }
}
