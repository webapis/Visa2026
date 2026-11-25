using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.PersonIntake;

/// <summary>
/// Invitation-line state for the person intake wizard.
/// Buckets match <see cref="InvitationItemColorTab"/> (Çakylyklar).
/// </summary>
public static class PersonIntakeInvitationState
{
    public const string Ready = InvitationItemColorTab.Ready;
    public const string Used = InvitationItemColorTab.Used;
    public const string Cancelled = InvitationItemColorTab.Cancelled;
    public const string Changed = InvitationItemColorTab.Changed;
    public const string Expired = InvitationItemColorTab.Expired;

    public static string Code(InvitationItem item) =>
        item == null ? string.Empty : InvitationItemColorTab.ResolveBucketKey(item);
}
