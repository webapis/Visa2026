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

    [XafDisplayName("Alternative addresses")]
    public virtual AlternativeAddressesForInvitation AlternativeAddress { get; set; }

    public virtual Guid? AlternativeAddressId { get; set; }

    [RuleRequiredField]
    [XafDisplayName("Application profile instance")]
    public virtual ApplicationProfileInstance ApplicationProfileInstance { get; set; }

    public virtual Guid? ApplicationProfileInstanceId { get; set; }

    [NotMapped]
    [VisibleInDetailView(false)]
    [VisibleInListView(false)]
    [ModelDefault("AllowEdit", "False")]
    public string FullAddressText => InvitationAddressText.Format(this);
}
