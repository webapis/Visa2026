using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using Visa2026.Module.Editors;
using Visa2026.Module.Services;

namespace Visa2026.Module.BusinessObjects.Email;

public enum SmtpMailSecurity
{
    StartTls = 0,
    SslOnConnect = 1,
    Auto = 2,
    None = 3
}

/// <summary>
/// One shared SMTP account for emailed document copies. Edited under Configuration → Mail server.
/// The password is stored encrypted; appsettings / <c>SmtpMail__Password</c> is only a fallback.
/// </summary>
[DefaultClassOptions]
[NavigationItem("Configuration")]
[ImageName("Actions_Send")]
[DisplayName("Mail server")]
[ModelDefault("Caption", "Mail server")]
public class EmailSmtpSettings : BaseObject
{
    public const string DefaultHost = "smtp.office365.com";
    public const int DefaultPort = 587;
    public const string DefaultFromName = "Visa2026";
    public const long DefaultMaxBytes = 20L * 1024 * 1024;
    public const int DefaultTimeoutSeconds = 120;

    [MaxLength(256)]
    public virtual string Host { get; set; }

    [RuleValueComparison(DefaultContexts.Save, ValueComparisonType.GreaterThan, 0)]
    public virtual int Port { get; set; }

    public virtual SmtpMailSecurity Security { get; set; }

    /// <summary>
    /// SMTP login. Office 365 usually needs the full mailbox address, not only a short account name.
    /// </summary>
    [MaxLength(320)]
    public virtual string UserName { get; set; }

    /// <summary>Encrypted mailbox password. Hidden; unlimited so the cipher text is not truncated.</summary>
    [Browsable(false)]
    [FieldSize(FieldSizeAttribute.Unlimited)]
    public virtual string ProtectedPassword { get; set; }

    /// <summary>
    /// Mailbox password shown masked, with a show/hide button. Not mapped — the stored secret stays in <see cref="ProtectedPassword"/>.
    /// </summary>
    [NotMapped]
    [EditorAlias(SmtpPasswordEditorAliases.Editor)]
    [VisibleInDetailView(true)]
    [VisibleInListView(false)]
    [MaxLength(256)]
    [ModelDefault("Caption", "Password")]
    [DisplayName("Password")]
    public string NewPassword
    {
        get => pendingPassword;
        set
        {
            pendingPassword = value ?? string.Empty;
            if (string.IsNullOrWhiteSpace(pendingPassword))
                return;
            if (pendingPassword.StartsWith("dp:v1:", StringComparison.Ordinal))
                return;
            ProtectedPassword = pendingPassword;
            ObjectSpace?.SetModified(this);
        }
    }

    private string pendingPassword = string.Empty;

    [NotMapped]
    [ModelDefault("AllowEdit", "False")]
    public bool PasswordIsSet => !string.IsNullOrWhiteSpace(ProtectedPassword);

    /// <summary>From address. Must be an address this login may send as.</summary>
    [MaxLength(320)]
    public virtual string FromAddress { get; set; }

    [MaxLength(256)]
    public virtual string FromName { get; set; }

    public virtual bool BccOfficer { get; set; }

    [RuleValueComparison(DefaultContexts.Save, ValueComparisonType.GreaterThan, 0)]
    public virtual long MaxTotalAttachmentBytes { get; set; }

    [RuleValueComparison(DefaultContexts.Save, ValueComparisonType.GreaterThan, 0)]
    public virtual int TimeoutSeconds { get; set; }

    [NotMapped]
    [ModelDefault("AllowEdit", "False")]
    public bool IsReady =>
        !string.IsNullOrWhiteSpace(Host)
        && !string.IsNullOrWhiteSpace(FromAddress)
        && PasswordIsSet;

    public override void OnCreated()
    {
        base.OnCreated();
        Host = DefaultHost;
        Port = DefaultPort;
        Security = SmtpMailSecurity.StartTls;
        FromName = DefaultFromName;
        BccOfficer = true;
        MaxTotalAttachmentBytes = DefaultMaxBytes;
        TimeoutSeconds = DefaultTimeoutSeconds;
    }

    public static EmailSmtpSettings? TryGetInstance(IObjectSpace objectSpace) =>
        OrganizationSingletonHelper.TryGet(objectSpace, (EmailSmtpSettings _) => "Mail server");

    public static EmailSmtpSettings GetOrCreateInstance(IObjectSpace objectSpace) =>
        TryGetInstance(objectSpace) ?? objectSpace.CreateObject<EmailSmtpSettings>();
}
