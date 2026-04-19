using System;
using System.ComponentModel;
using DevExpress.ExpressApp.DC;

namespace Visa2026.Module.BusinessObjects
{
    [DomainComponent]
    public class StateDashboard
    {
        [Browsable(false)]
        public Guid ID { get; set; } = Guid.NewGuid();
    }
}
