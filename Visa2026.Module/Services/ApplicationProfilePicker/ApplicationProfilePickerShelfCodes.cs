namespace Visa2026.Module.Services.ApplicationProfilePicker;

public static class ApplicationProfilePickerShelfCodes
{
    public const string Invitations = "invitations";
    public const string Registration = "registration";
    public const string FamilyMemberOnly = "family_member_only";
    public const string WorkPermitOnly = "work_permit_only";

    public const int RecentCap = 12;

    public static readonly (string Code, string DefaultName, int SortOrder)[] SystemShelves =
    [
        (Invitations, "Invitations", 10),
        (Registration, "Registration", 20),
        (FamilyMemberOnly, "Family member only", 30),
        (WorkPermitOnly, "Work permit only", 40),
    ];
}