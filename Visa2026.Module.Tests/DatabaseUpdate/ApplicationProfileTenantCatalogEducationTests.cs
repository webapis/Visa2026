using Visa2026.Module.BusinessObjects;
using Visa2026.Module.DatabaseUpdate;
using Visa2026.Module.DatabaseUpdate.LookupCatalogs;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

public class ApplicationProfileTenantCatalogEducationTests
{
    [Fact]
    public void Calik_catalog_does_not_require_education_for_visa_cancellation()
    {
        Assert.True(ApplicationProfileTenantCatalogLoader.TryLoadRows(out var rows));
        Assert.NotEmpty(rows);

        foreach (var row in rows)
        {
            var visaCancel = ApplicationProfileEducationPolicy.IsVisaDocumentCancellation(
                row.CancelVisas,
                row.Code);
            if (visaCancel)
                Assert.False(row.RequirePersonEducation, row.Code);
        }

        Assert.Contains(rows, r => r.Code == "cancel_visa" && !r.RequirePersonEducation);
        Assert.Contains(rows, r => r.Code == "cancel_visa_wp" && !r.RequirePersonEducation);
        Assert.Contains(rows, r => r.Code == "cancel_visa_ext" && r.RequirePersonEducation);
        Assert.Contains(rows, r => r.Code == "get_invitation" && r.RequirePersonEducation);
    }

    [Fact]
    public void Mapper_turns_off_education_for_cancel_visa_code()
    {
        var cancel = new ApplicationProfile();
        ApplicationProfileFromApplicationTypeMapper.Apply(
            cancel,
            new ApplicationType
            {
                Name = "App_Cancel_Visa",
                Code = "cancel_visa",
                ShowCurrentEducation = true,
                ShowVisaIsCancelled = false,
            });
        Assert.False(cancel.RequirePersonEducation);

        var issuance = new ApplicationProfile();
        ApplicationProfileFromApplicationTypeMapper.Apply(
            issuance,
            new ApplicationType { ShowCurrentEducation = true });
        Assert.True(issuance.RequirePersonEducation);
    }
}