#nullable enable

using System;
using System.Collections.Generic;
using System.Reflection;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Add / Sync success path for skip-nav child M2M (Passport + WorkDuty).
/// Gate / Remove coverage lives in open PR #66; this covers GetObjectByKey hits.
/// </summary>
public class ApplicationProfileInstanceChildMembershipAddTests
{
    [Fact]
    public void Add_Passport_appends_once_and_skips_duplicate()
    {
        var passportId = Guid.NewGuid();
        var passport = new Passport { ID = passportId };
        var os = CatalogObjectSpaceStub.Create(passport);
        var app = new ApplicationProfileInstance();

        ApplicationProfileInstanceChildMembership.Add(
            os,
            app,
            ApplicationProfileInstancePersonLinkKind.Passport,
            passportId);

        Assert.Single(app.Passports!);
        Assert.Same(passport, app.Passports![0]);

        ApplicationProfileInstanceChildMembership.Add(
            os,
            app,
            ApplicationProfileInstancePersonLinkKind.Passport,
            passportId);

        Assert.Single(app.Passports!);
    }

    [Fact]
    public void Add_WorkDuty_appends_when_entity_found()
    {
        var dutyId = Guid.NewGuid();
        var duty = new WorkDuty { ID = dutyId, Description = "site duty" };
        var os = CatalogObjectSpaceStub.Create(duty);
        var app = new ApplicationProfileInstance();

        ApplicationProfileInstanceChildMembership.Add(
            os,
            app,
            ApplicationProfileInstancePersonLinkKind.WorkDuty,
            dutyId);

        Assert.Single(app.WorkDuties!);
        Assert.Equal(dutyId, app.WorkDuties![0].ID);
    }

    [Fact]
    public void Add_missing_entity_is_noop()
    {
        var os = CatalogObjectSpaceStub.Create();
        var app = new ApplicationProfileInstance();

        ApplicationProfileInstanceChildMembership.Add(
            os,
            app,
            ApplicationProfileInstancePersonLinkKind.Passport,
            Guid.NewGuid());

        Assert.Empty(app.Passports!);
        Assert.Empty(app.WorkDuties!);
    }

    [Fact]
    public void SyncFromResolvedLinks_adds_WorkDuty_from_sticky_link()
    {
        var dutyId = Guid.NewGuid();
        var duty = new WorkDuty { ID = dutyId };
        var os = CatalogObjectSpaceStub.Create(duty);
        var app = new ApplicationProfileInstance();

        ApplicationProfileInstanceChildMembership.SyncFromResolvedLinks(
            os,
            app,
            [
                new ApplicationProfileInstancePersonResolvedLink
                {
                    LinkKind = ApplicationProfileInstancePersonLinkKind.WorkDuty,
                    LinkedObjectId = dutyId,
                },
            ]);

        Assert.Single(app.WorkDuties!);
        Assert.Same(duty, app.WorkDuties![0]);
    }

    /// <summary>DispatchProxy stub: GetObjectByKey returns seeded entities; other calls fail.</summary>
    private class CatalogObjectSpaceStub : DispatchProxy
    {
        private readonly Dictionary<Guid, object> _byId = new();

        public static IObjectSpace Create(params object[] entities)
        {
            var proxy = Create<IObjectSpace, CatalogObjectSpaceStub>();
            var stub = (CatalogObjectSpaceStub)proxy;
            foreach (var entity in entities)
            {
                var idProp = entity.GetType().GetProperty("ID");
                if (idProp?.GetValue(entity) is Guid id && id != Guid.Empty)
                    stub._byId[id] = entity;
            }

            return proxy;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == nameof(IObjectSpace.GetObjectByKey)
                && args is { Length: 1 }
                && args[0] is Guid id)
            {
                return _byId.TryGetValue(id, out var entity) ? entity : null;
            }

            throw new InvalidOperationException(
                $"Unexpected IObjectSpace call: {targetMethod?.Name}");
        }
    }
}
