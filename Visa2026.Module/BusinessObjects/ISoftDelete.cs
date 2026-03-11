using System;

namespace Visa2026.Module.BusinessObjects
{
    public interface ISoftDelete
    {
        bool IsDeleted { get; set; }
        DateTime? DateDeleted { get; set; }
        ApplicationUser DeletedBy { get; set; }
    }
}