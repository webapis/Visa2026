using System.Globalization;
using Visa2026.Module;
using Xunit;

namespace Visa2026.Module.Tests;

public sealed class PersonNestedTabCaptionHelperTests
{
    [Fact]
    public void TryGetBaseCaption_EmployeePersonDocuments_returnsLocalizedCaption()
    {
        var caption = PersonNestedTabCaptionHelper.TryGetBaseCaption(
            PersonDetailViewIds.Employee,
            PersonNestedCollectionLayout.CvAndPersonalFilesTab);

        Assert.Equal("CV & personal files", caption);
    }

    [Fact]
    public void TryGetBaseCaption_PassportDocuments_returnsPassportCopies()
    {
        var caption = PersonNestedTabCaptionHelper.TryGetBaseCaption(
            "Passport_DetailView",
            PersonNestedCollectionLayout.CvAndPersonalFilesTab);

        Assert.Equal("Passport copies", caption);
    }

    [Fact]
    public void TryGetBaseCaption_EmployeeEducations_followsUiCulture()
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tk-TM");
            var caption = PersonNestedTabCaptionHelper.TryGetBaseCaption(
                PersonDetailViewIds.Employee,
                "Educations");
            Assert.Equal("Bilim", caption);

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            caption = PersonNestedTabCaptionHelper.TryGetBaseCaption(
                PersonDetailViewIds.Employee,
                "ApplicationProfileInstances");
            Assert.Equal("Applications (linked)", caption);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void TryGetBaseCaption_FamilyMemberDocuments_returnsNull()
    {
        // Person.Documents is employee-only; family member view has no Documents caption override.
        Assert.Null(PersonNestedTabCaptionHelper.TryGetBaseCaption(
            PersonDetailViewIds.FamilyMember,
            PersonNestedCollectionLayout.CvAndPersonalFilesTab));
    }
}