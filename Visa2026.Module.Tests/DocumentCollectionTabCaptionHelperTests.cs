using System.Globalization;
using Visa2026.Module;
using Xunit;

namespace Visa2026.Module.Tests;

public sealed class DocumentCollectionTabCaptionHelperTests
{
    [Theory]
    [InlineData(PersonDetailViewIds.Employee, "CV & personal files")]
    [InlineData("Passport_DetailView", "Passport copies")]
    [InlineData("Education_DetailView", "Diploma copies")]
    [InlineData("Visa_DetailView", "Visa copies")]
    [InlineData("WorkPermit_DetailView", "Work permit copies")]
    [InlineData("Invitation_DetailView", "Invitation copies")]
    [InlineData("Rejection_DetailView", "Rejection copies")]
    [InlineData("BorderZone_DetailView", "Border zone copies")]
    [InlineData("MedicalRecord_DetailView", "Medical record copies")]
    [InlineData("Lodging_DetailView", "Lodging copies")]
    [InlineData("ProjectContract_DetailView", "Project contract copies")]
    [InlineData("AddressOfResidence_DetailView", "Address copies")]
    public void TryGetBaseCaption_DocumentsLayout_returnsParentSpecificCaption(string detailViewId, string expected)
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            // Caption helpers follow CurrentUICulture; pin en-US so assertions stay stable
            // when the app default culture is Turkish (tr-TR).
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");

            Assert.Equal(
                expected,
                DocumentCollectionTabCaptionHelper.TryGetBaseCaption(detailViewId, "Documents"));
            Assert.Equal(
                expected,
                DocumentCollectionTabCaptionHelper.TryGetBaseCaption(detailViewId, "Documents_Group"));
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void TryGetBaseCaption_unrelatedDetailView_returnsNull()
    {
        Assert.Null(DocumentCollectionTabCaptionHelper.TryGetBaseCaption("Application_DetailView", "Documents"));
    }
}
