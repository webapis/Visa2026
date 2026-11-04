namespace Visa2026.Module.Services.ApplicationProfilePicker;

public static class ApplicationProfilePickerShelfCodes
{
    public const string Invitations = "invitations";
    public const string Visa = "visa";
    public const string Registration = "registration";
    public const string FamilyMemberOnly = "family_member_only";
    public const string BusinessTrip = "business_trip";
    public const string WorkPermitOnly = "work_permit_only";
    public const string BorderZone = "border_zone";
    public const string Seretmezlik = "seretmezlik";

    public const int RecentCap = 12;

    public static readonly (string Code, string DefaultName, int SortOrder)[] SystemShelves =
    [
        (Invitations, "Invitations", 10),
        (Visa, "Visa", 20),
        (Registration, "Registration", 30),
        (FamilyMemberOnly, "Family member", 40),
        (BusinessTrip, "Business trip", 45),
        (WorkPermitOnly, "Work permit", 50),
        (BorderZone, "Border zone", 55),
        (Seretmezlik, "Seretmezlik", 60),
    ];
}