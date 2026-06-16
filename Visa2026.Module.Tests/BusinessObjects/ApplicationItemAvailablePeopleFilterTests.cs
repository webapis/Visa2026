using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public class ApplicationItemAvailablePeopleFilterTests
{
    [Fact]
    public void GetExcludedPersonIds_ExcludesSiblingItems()
    {
        var personA = new Person { ID = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa") };
        var personB = new Person { ID = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb") };
        var existingItem = new ApplicationItem
        {
            ID = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Person = personA
        };
        var newItem = new ApplicationItem
        {
            ID = Guid.Parse("22222222-2222-2222-2222-222222222222")
        };
        var application = new Application
        {
            ApplicationItems = new ObservableCollection<ApplicationItem> { existingItem, newItem }
        };
        existingItem.Application = application;
        newItem.Application = application;

        var excluded = InvokeGetExcludedPersonIds(application, newItem.ID, objectSpace: null);

        Assert.Contains(personA.ID, excluded);
        Assert.DoesNotContain(personB.ID, excluded);
    }

    [Fact]
    public void GetExcludedPersonIds_KeepsCurrentPersonWhenEditing()
    {
        var personA = new Person { ID = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa") };
        var currentItem = new ApplicationItem
        {
            ID = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Person = personA
        };
        var application = new Application
        {
            ApplicationItems = new ObservableCollection<ApplicationItem> { currentItem }
        };
        currentItem.Application = application;

        var excluded = InvokeGetExcludedPersonIds(application, currentItem.ID, objectSpace: null);

        Assert.Empty(excluded);
    }

    private static HashSet<Guid> InvokeGetExcludedPersonIds(
        Application application,
        Guid currentApplicationItemId,
        DevExpress.ExpressApp.IObjectSpace? objectSpace)
    {
        var method = typeof(ApplicationItemAvailablePeopleFilter).GetMethod(
            "GetExcludedPersonIds",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.NotNull(method);
        return (HashSet<Guid>)method.Invoke(null, [application, currentApplicationItemId, objectSpace])!;
    }
}
