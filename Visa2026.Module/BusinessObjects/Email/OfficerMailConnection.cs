using System;
using System.ComponentModel.DataAnnotations;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;

namespace Visa2026.Module.BusinessObjects.Email;

/// <summary>
/// An officer's consent for Visa2026 to send mail from their own Office 365 mailbox via Microsoft Graph.
/// Read and written only by the host mail services through a non-secured object space; officers never
/// get type permissions on it.
/// </summary>
[NavigationItem(false)]
public class OfficerMailConnection : BaseObject
{
    public virtual Guid UserID { get; set; }

    [MaxLength(320)]
    public virtual string AccountEmail { get; set; }

    [MaxLength(256)]
    public virtual string AccountDisplayName { get; set; }

    /// <summary>Microsoft refresh token encrypted with ASP.NET Core Data Protection.</summary>
    [FieldSize(FieldSizeAttribute.Unlimited)]
    public virtual string ProtectedRefreshToken { get; set; }

    public virtual DateTime ConnectedOnUtc { get; set; }

    public virtual DateTime? LastUsedOnUtc { get; set; }
}
