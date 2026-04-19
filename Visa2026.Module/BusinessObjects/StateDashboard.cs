using System;
using DevExpress.ExpressApp.DC;

namespace Visa2026.Module.BusinessObjects
{
    [DomainComponent]
    public class StateDashboard
    {
        public Guid ID { get; set; } = Guid.NewGuid();
    }
}
