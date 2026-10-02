using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Where an invitee may stay for one Application Profile instance.
/// Region and City are the first place. <see cref="AlternativeAddress"/> holds the other places.
/// </summary>
[DefaultClassOptions]
[NavigationItem(false)]
[DefaultProperty(nameof(FullAddressText))]
[XafDisplayName("Invitation address")]
public class InvitationAddress : BaseObject
{
    [ImmediatePostData]
    [XafDisplayName("Region")]
    public virtual Region Region { get; set; }

    public virtual Guid? RegionId { get; set; }

    [DataSourceCriteria("[Region] = '@This.Region'")]
    [XafDisplayName("City")]
    public virtual City City { get; set; }

    public virtual Guid? CityId { get; set; }

    [ImmediatePostData]
    [XafDisplayName("Address type")]
    public virtual ResidenceType? Type { get; set; }

    [DataSourceCriteria("City = '@This.City'")]
    [XafDisplayName("Lodging")]
    public virtual Lodging Lodging { get; set; }

    public virtual Guid? LodgingId { get; set; }

    [DataSourceCriteria("City = '@This.City'")]
    [XafDisplayName("Hotel")]
    public virtual Hotel Hotel { get; set; }

    public virtual Guid? HotelId { get; set; }

    [DataSourceCriteria("City = '@This.City'")]
    [XafDisplayName("Hospital")]
    public virtual Hospital Hospital { get; set; }

    public virtual Guid? HospitalId { get; set; }

    [DataSourceCriteria("City = '@This.City'")]
    [XafDisplayName("Other site")]
    public virtual OtherSite OtherSite { get; set; }

    public virtual Guid? OtherSiteId { get; set; }

    [MaxLength(255)]
    [XafDisplayName("Private house")]
    public virtual string PrivateHouseAddress { get; set; }

    [XafDisplayName("Alternative addresses")]
    public virtual AlternativeAddressesForInvitation AlternativeAddress { get; set; }

    public virtual Guid? AlternativeAddressId { get; set; }

    [RuleRequiredField]
    [XafDisplayName("Application profile instance")]
    public virtual ApplicationProfileInstance ApplicationProfileInstance { get; set; }

    public virtual Guid? ApplicationProfileInstanceId { get; set; }

    public void ClearSitesExcept(ResidenceType? keep)
    {
        if (keep != ResidenceType.Lodging)
            Lodging = null;
        if (keep != ResidenceType.Hotel)
            Hotel = null;
        if (keep != ResidenceType.Hospital)
            Hospital = null;
        if (keep != ResidenceType.Other)
            OtherSite = null;
        if (keep != ResidenceType.PrivateHouse)
            PrivateHouseAddress = null;
    }

    [NotMapped]
    [VisibleInDetailView(false)]
    [VisibleInListView(false)]
    [ModelDefault("AllowEdit", "False")]
    public string FullAddressText => InvitationAddressText.Format(this);
}
