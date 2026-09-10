using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014.Tests;

public class Visa2014TravelHistoryTransformTests
{
    [Theory]
    [InlineData("External", "Entry", typeof(ExternalArrival))]
    [InlineData("External", "Exit", typeof(ExternalDeparture))]
    [InlineData("Internal", "Entry", typeof(InternalArrival))]
    [InlineData("Internal", "Exit", typeof(InternalDeparture))]
    public void ResolveConcreteType_MapsRegistrationPair(string travelType, string movementType, Type expected)
    {
        Assert.Same(expected, Visa2014TravelHistoryTransform.ResolveConcreteType(travelType, movementType));
    }

    [Fact]
    public void ResolveConcreteType_IgnoresNonRegistration()
    {
        Assert.Null(Visa2014TravelHistoryTransform.ResolveConcreteType(null, null));
        Assert.Null(Visa2014TravelHistoryTransform.ResolveConcreteType("External", null));
    }
}
