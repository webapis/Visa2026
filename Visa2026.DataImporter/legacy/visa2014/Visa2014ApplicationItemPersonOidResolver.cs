namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// Resolves which legacy person Oid an ApplicationItem PIA row refers to
/// (employee vs family member flags).
/// </summary>
internal static class Visa2014ApplicationItemPersonOidResolver
{
    internal static Guid? Resolve(
        bool forEmployee,
        bool forFamilyMember,
        Guid? legacyEmployeeOid,
        Guid? legacyFamilyMemberOid) =>
        forEmployee ? legacyEmployeeOid
        : forFamilyMember ? legacyFamilyMemberOid
        : null;
}
