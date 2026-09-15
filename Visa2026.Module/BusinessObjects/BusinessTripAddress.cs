using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;

namespace Visa2026.Module.BusinessObjects
{
    /// <summary>
    /// Legacy business-trip address catalog. Prefer instance/profile Type + Lodging/Hotel/Hospital/OtherSite.
    /// </summary>
    [Obsolete("Use ApplicationProfileInstance.BusinessTripAddressType with Lodging/Hotel/Hospital/OtherSite tenant catalogs.")]
    [DefaultClassOptions]
    [NavigationItem(false)]
    public class BusinessTripAddress : BaseObject
    {
        public virtual City City { get; set; }

        [MaxLength(255)]
        public virtual string FullAddress { get; set; }
    }
}