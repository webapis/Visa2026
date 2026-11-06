using Visa2026.Module.BusinessObjects;
using Visa2026.Module.DatabaseUpdate.LookupCatalogs;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

public class ApplicationProfileProgressStepVisibilityCatalogTests
{
    [Fact]
    public void Calik_profiles_list_every_progress_step_and_match_todays_timeline()
    {
        Assert.True(ApplicationProfileTenantCatalogLoader.TryLoadRows(out var rows));
        Assert.NotEmpty(rows);

        foreach (var row in rows)
        {
            Assert.NotNull(row.ProgressSteps);
            Assert.Equal(ApplicationProfileProgressStepVisibility.AllKeys, row.ProgressSteps!.Select(s => s.Key).ToArray());

            var via = row.ProgressRoute == nameof(ApplicationProfileInstanceProgressRouteKind.ViaMinistries);
            var directAs = !via
                && row.RequireSubmitToOrganization == false
                && row.ActionFamily is nameof(ApplicationProfileActionFamily.Issuance)
                    or nameof(ApplicationProfileActionFamily.Change);

            Assert.True(Visible(row, ApplicationProfileProgressStepVisibility.Office), row.Code);
            Assert.Equal(via, Visible(row, ApplicationProfileProgressStepVisibility.Ministry));
            Assert.True(Visible(row, ApplicationProfileProgressStepVisibility.Submission), row.Code);
            Assert.Equal(via || directAs, Visible(row, ApplicationProfileProgressStepVisibility.AsNumber));
            var issuedCompletion = row.Code is "cancel_invitation"
                or "cancel_invitation_wp"
                or "cancel_visa"
                or "cancel_visa_wp"
                or "cancel_workpermit";
            // Issued stays on direct Issuance/Change, and on invitation/visa/work-permit cancellation (Işlendi).
            Assert.Equal(directAs || issuedCompletion, Visible(row, ApplicationProfileProgressStepVisibility.Issued));
            Assert.Equal(directAs, Visible(row, ApplicationProfileProgressStepVisibility.Rejected));
            Assert.Equal(directAs, Visible(row, ApplicationProfileProgressStepVisibility.Cancelled));
        }
    }

    private static bool Visible(ApplicationProfileTenantCatalogRow row, string key) =>
        row.ProgressSteps!.Single(s => s.Key == key).Visible;
}
