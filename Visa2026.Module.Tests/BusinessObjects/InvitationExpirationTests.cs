using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public class InvitationExpirationTests
{
    [Fact]
    public void ExpirationFromIssued_AddsNinetyCalendarDays()
    {
        var issued = new DateTime(2026, 10, 17);

        Assert.Equal(new DateTime(2027, 1, 15), Invitation.ExpirationFromIssued(issued));
        Assert.Equal(90, Invitation.ExpirationOffsetDays);
    }

    [Fact]
    public void OnCreated_SetsIssuedTodayAndExpirationNinetyDaysLater()
    {
        var invitation = new Invitation();

        invitation.OnCreated();

        Assert.Equal(DateTime.Today, invitation.IssuedDate.Date);
        Assert.Equal(Invitation.ExpirationFromIssued(DateTime.Today), invitation.ExpirationDate);
    }
}