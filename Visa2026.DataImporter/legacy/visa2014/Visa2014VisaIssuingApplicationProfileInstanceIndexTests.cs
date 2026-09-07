using Visa2026.DataImporter.Legacy.Visa2014;
using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014.Tests;

public class Visa2014VisaIssuingApplicationProfileInstanceIndexTests
{
    [Fact]
    public void OverlayAsNumber_WinsOverStickyInvitationApplication()
    {
        var visa = Guid.NewGuid();
        var invitationApp = Guid.NewGuid();
        var passportChangeApp = Guid.NewGuid();
        var map = new Dictionary<Guid, Guid> { [visa] = invitationApp };

        var overlaid = Visa2014VisaIssuingApplicationProfileInstanceIndex.OverlayAsNumberMatches(
            map,
            visaAsNumbers: new Dictionary<Guid, string> { [visa] = "AS448575" },
            applicationByProcessNumber: new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase)
            {
                ["AS448575"] = passportChangeApp,
            });

        Assert.Equal(1, overlaid);
        Assert.Equal(passportChangeApp, map[visa]);
    }

    [Fact]
    public void OverlayAsNumber_SkipsAmbiguousProcessNumber()
    {
        var visa = Guid.NewGuid();
        var invitationApp = Guid.NewGuid();
        var map = new Dictionary<Guid, Guid> { [visa] = invitationApp };

        var overlaid = Visa2014VisaIssuingApplicationProfileInstanceIndex.OverlayAsNumberMatches(
            map,
            visaAsNumbers: new Dictionary<Guid, string> { [visa] = "AS1" },
            applicationByProcessNumber: new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase));

        Assert.Equal(0, overlaid);
        Assert.Equal(invitationApp, map[visa]);
    }

    [Fact]
    public void OverlayAsNumber_AddsVisaMissingFromPiaIndex()
    {
        var visa = Guid.NewGuid();
        var app = Guid.NewGuid();
        var map = new Dictionary<Guid, Guid>();

        var overlaid = Visa2014VisaIssuingApplicationProfileInstanceIndex.OverlayAsNumberMatches(
            map,
            visaAsNumbers: new Dictionary<Guid, string> { [visa] = "AS448575" },
            applicationByProcessNumber: new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase)
            {
                ["as448575"] = app,
            });

        Assert.Equal(1, overlaid);
        Assert.Equal(app, map[visa]);
    }
}
