using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public class OrganizationPassportLineHelperTests
{
    [Fact]
    public void FormatNumberAuthorityPhone_joins_number_authority_and_phone()
    {
        var line = OrganizationPassportLineHelper.FormatNumberAuthorityPhone(
            "I-AŞ 476479",
            "Aşgabat ş. Berkararlyk etr. Häkimligi tarapyndan berlen",
            "+993 65 55-13-49");

        Assert.Equal(
            "I-AŞ 476479, Aşgabat ş. Berkararlyk etr. Häkimligi tarapyndan berlen, +993 65 55-13-49",
            line);
    }
}