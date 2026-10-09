#nullable enable

using System;
using System.Collections.ObjectModel;
using System.Reflection;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Early gates and Remove identity for skip-nav child M2M sync (incl. WorkDuty).
/// </summary>
public class ApplicationProfileInstanceChildMembershipGateTests
{
    [Fact]
    public void SyncFromResolvedLinks_null_object_space_or_application_is_noop()
    {
        var app = new ApplicationProfileInstance();
        var links = new[]
        {
            new ApplicationProfileInstancePersonResolvedLink
            {
                LinkKind = ApplicationProfileInstancePersonLinkKind.Passport,
                LinkedObjectId = Guid.NewGuid(),
            },
        };

        ApplicationProfileInstanceChildMembership.SyncFromResolvedLinks(null!, app, links);
        Assert.Empty(app.Passports ?? []);

        ApplicationProfileInstanceChildMembership.SyncFromResolvedLinks(
            objectSpace: null!,
            application: null!,
            links);
    }

    [Fact]
    public void SyncFromResolvedLinks_skips_null_empty_and_invalid_links()
    {
        var app = new ApplicationProfileInstance();
        var os = ThrowingGetObjectObjectSpaceStub.Create();

        ApplicationProfileInstanceChildMembership.SyncFromResolvedLinks(os, app, links: null);

        ApplicationProfileInstanceChildMembership.SyncFromResolvedLinks(
            os,
            app,
            [
                null!,
                new ApplicationProfileInstancePersonResolvedLink(),
                new ApplicationProfileInstancePersonResolvedLink
                {
                    LinkKind = ApplicationProfileInstancePersonLinkKind.Passport,
                    LinkedObjectId = Guid.Empty,
                },
            ]);

        // Stub throws if Add → GetObjectByKey is reached.
        Assert.Empty(app.Passports ?? []);
        Assert.Empty(app.WorkDuties ?? []);
    }

    [Fact]
    public void Remove_WorkDuty_by_id_without_object_space()
    {
        var dutyId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var app = new ApplicationProfileInstance
        {
            WorkDuties = new ObservableCollection<WorkDuty>
            {
                new() { ID = dutyId, Description = "keep-remove" },
                new() { ID = otherId, Description = "stay" },
            },
        };

        ApplicationProfileInstanceChildMembership.Remove(
            app,
            ApplicationProfileInstancePersonLinkKind.WorkDuty,
            dutyId);

        Assert.Single(app.WorkDuties!);
        Assert.Equal(otherId, app.WorkDuties![0].ID);
    }

    [Fact]
    public void Remove_Passport_clears_collection_without_object_space()
    {
        var passportId = Guid.NewGuid();
        var app = new ApplicationProfileInstance
        {
            ID = Guid.NewGuid(),
            Passports = new ObservableCollection<Passport>
            {
                new() { ID = passportId },
            },
        };

        ApplicationProfileInstanceChildMembership.Remove(
            app,
            ApplicationProfileInstancePersonLinkKind.Passport,
            passportId,
            objectSpace: null);

        Assert.Empty(app.Passports!);
    }

    [Fact]
    public void Remove_null_collection_and_unknown_kind_do_not_throw()
    {
        var app = new ApplicationProfileInstance();

        ApplicationProfileInstanceChildMembership.Remove(
            app,
            ApplicationProfileInstancePersonLinkKind.WorkDuty,
            Guid.NewGuid());

        // RejectionItem is not a skip-nav child in Add/Remove switch.
        ApplicationProfileInstanceChildMembership.Remove(
            app,
            ApplicationProfileInstancePersonLinkKind.RejectionItem,
            Guid.NewGuid());
    }

    /// <summary>DispatchProxy stub: any call fails — proves Sync skipped Add for invalid links.</summary>
    private class ThrowingGetObjectObjectSpaceStub : DispatchProxy
    {
        public static IObjectSpace Create() =>
            Create<IObjectSpace, ThrowingGetObjectObjectSpaceStub>();

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException(
                $"Unexpected IObjectSpace call: {targetMethod?.Name}");
    }
}
