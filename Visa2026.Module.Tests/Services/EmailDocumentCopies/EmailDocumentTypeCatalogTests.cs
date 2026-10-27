using Visa2026.Module.Services.EmailDocumentCopies;
using Xunit;

namespace Visa2026.Module.Tests.Services.EmailDocumentCopies;

public class EmailDocumentTypeCatalogTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("Unknown:abc", null)]
    [InlineData("Passport:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", EmailDocumentType.Passport)]
    [InlineData("Passport:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa/Visa:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb", EmailDocumentType.Visa)]
    [InlineData("AddressOfResidence:cccccccccccccccccccccccccccccccc", EmailDocumentType.AddressOfResidence)]
    [InlineData("WorkPermitItem:dddddddddddddddddddddddddddddddd", EmailDocumentType.WorkPermit)]
    [InlineData("InvitationItem:eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee", EmailDocumentType.Invitation)]
    [InlineData("Education:ffffffffffffffffffffffffffffffff", EmailDocumentType.Education)]
    [InlineData("MedicalRecord:11111111111111111111111111111111", EmailDocumentType.MedicalRecord)]
    [InlineData("FamilyRelationDocument:22222222222222222222222222222222", EmailDocumentType.FamilyProof)]
    public void Classify_MapsRecordKeys(string? key, EmailDocumentType? expected)
    {
        Assert.Equal(expected, EmailDocumentTypeCatalog.Classify(key));
    }

    [Fact]
    public void DefaultSelection_IsPassportAndVisa()
    {
        Assert.Equal(
            new[] { EmailDocumentType.Passport, EmailDocumentType.Visa },
            EmailDocumentTypeCatalog.DefaultSelection);
    }

    [Fact]
    public void FamilyProof_HasNoCurrentRecord()
    {
        Assert.False(EmailDocumentTypeCatalog.HasCurrentRecord(EmailDocumentType.FamilyProof));
        Assert.True(EmailDocumentTypeCatalog.HasCurrentRecord(EmailDocumentType.Passport));
        Assert.Equal("EmailDocumentCopies.Type.Visa", EmailDocumentTypeCatalog.LabelKey(EmailDocumentType.Visa));
    }
}
