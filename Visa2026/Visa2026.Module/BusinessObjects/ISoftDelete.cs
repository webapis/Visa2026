using System;

namespace Visa2026.Module.BusinessObjects
{
    public interface ISoftDelete
    {
        bool IsDeleted { get; set; }
    }
}