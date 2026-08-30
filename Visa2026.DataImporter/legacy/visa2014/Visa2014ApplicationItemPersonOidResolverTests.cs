using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

public sealed class Visa2014ApplicationItemPersonOidResolverTests
{
    [Fact]
    public void Resolve_employee_uses_employee_oid()
    {
        var employeeOid = Guid.NewGuid();
        var familyOid = Guid.NewGuid();

        Assert.Equal(
            employeeOid,
            Visa2014ApplicationItemPersonOidResolver.Resolve(
                forEmployee: true,
                forFamilyMember: false,
                legacyEmployeeOid: employeeOid,
                legacyFamilyMemberOid: familyOid));
    }

    [Fact]
    public void Resolve_family_uses_family_oid()
    {
        var employeeOid = Guid.NewGuid();
        var familyOid = Guid.NewGuid();

        Assert.Equal(
            familyOid,
            Visa2014ApplicationItemPersonOidResolver.Resolve(
                forEmployee: false,
                forFamilyMember: true,
                legacyEmployeeOid: employeeOid,
                legacyFamilyMemberOid: familyOid));
    }

    [Fact]
    public void Resolve_neither_flag_returns_null()
    {
        Assert.Null(
            Visa2014ApplicationItemPersonOidResolver.Resolve(
                forEmployee: false,
                forFamilyMember: false,
                legacyEmployeeOid: Guid.NewGuid(),
                legacyFamilyMemberOid: Guid.NewGuid()));
    }

    [Fact]
    public void Resolve_employee_flag_with_null_oid_returns_null()
    {
        Assert.Null(
            Visa2014ApplicationItemPersonOidResolver.Resolve(
                forEmployee: true,
                forFamilyMember: false,
                legacyEmployeeOid: null,
                legacyFamilyMemberOid: Guid.NewGuid()));
    }

    [Fact]
    public void Resolve_employee_flag_wins_over_family_flag()
    {
        // Defensive: both flags set should still prefer employee (legacy ForEmployee path).
        var employeeOid = Guid.NewGuid();
        var familyOid = Guid.NewGuid();

        Assert.Equal(
            employeeOid,
            Visa2014ApplicationItemPersonOidResolver.Resolve(
                forEmployee: true,
                forFamilyMember: true,
                legacyEmployeeOid: employeeOid,
                legacyFamilyMemberOid: familyOid));
    }
}
