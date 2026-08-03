using System.Globalization;
using Visa2026.Module;
using Xunit;

namespace Visa2026.Module.Tests;

public sealed class PersonNestedTabCaptionHelperTests
{
    [Fact]
    public void TryGetBaseCaption_EmployeePersonDocuments_returnsLocalizedCaption()
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");

            var caption = PersonNestedTabCaptionHelper.TryGetBaseCaption(
                PersonDetailViewIds.Employee,
                PersonNestedCollectionLayout.CvAndPersonalFilesTab);

            Assert.Equal("CV & personal files", caption);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void TryGetBaseCaption_PassportDocuments_returnsPassportCopies()
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");

            var caption = PersonNestedTabCaptionHelper.TryGetBaseCaption(
                "Passport_DetailView",
                PersonNestedCollectionLayout.CvAndPersonalFilesTab);

            Assert.Equal("Passport copies", caption);
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
