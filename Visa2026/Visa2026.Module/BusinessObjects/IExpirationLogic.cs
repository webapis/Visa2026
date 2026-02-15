using System;

namespace Visa2026.Module.BusinessObjects
{
    public interface IExpirationLogic
    {
        bool IsActive { get; set; }
        DateTime? ExpirationDate { get; }
        int DaysRemaining { get; }
        ExpirationState ExpirationState { get; }
    }
}