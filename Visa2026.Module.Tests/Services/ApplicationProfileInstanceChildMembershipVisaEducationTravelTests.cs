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
/// Remaining skip-nav child kinds (Visa / Education / TravelHistory) beyond Passport+WorkDuty
/// covered in open coverage PR #69 — officers link these from sticky ResolvedLinks.
/// </summary>
public class ApplicationProfileInstanceChildMembershipVisaEducationTravelTests
{
    [Fact]
    public void Add_Visa_Education_TravelHistory_append_once_each()
    {
        var visaId = Guid.NewGuid();
        var educationId = Guid.NewGuid();
        var travelId = Guid.NewGuid();
        var visa = new Visa { ID = visaId };
        var education = new Education { ID = educationId };
        var travel = new ExternalArrival { ID = travelId };
        var os = KeyObjectSpaceStub.Create(visa, education, travel);
        var app = new ApplicationProfileInstance();

        ApplicationProfileInstanceChildMembership.Add(
            os, app, ApplicationProfileInstancePersonLinkKind.Visa, visaId);
        ApplicationProfileInstanceChildMembership.Add(
            os, app, ApplicationProfileInstancePersonLinkKind.Education, educationId);
        ApplicationProfileInstanceChildMembership.Add(
            os, app, ApplicationProfileInstancePersonLinkKind.TravelHistory, travelId);

        Assert.Single(app.Visas!);
        Assert.Same(visa, app.Visas![0]);
        Assert.Single(app.Educations!);
        Assert.Same(education, app.Educations![0]);
        Assert.Single(app.TravelHistories!);
        Assert.Same(travel, app.TravelHistories![0]);

        ApplicationProfileInstanceChildMembership.Add(
            os, app, ApplicationProfileInstancePersonLinkKind.Visa, visaId);
        Assert.Single(app.Visas!);
    }

    [Fact]
    public void SyncFromResolvedLinks_adds_visa_education_and_travel_history()
    {
        var visaId = Guid.NewGuid();
        var educationId = Guid.NewGuid();
        var travelId = Guid.NewGuid();
        var visa = new Visa { ID = visaId };
        var education = new Education { ID = educationId };
        var travel = new ExternalArrival { ID = travelId };
        var os = KeyObjectSpaceStub.Create(visa, education, travel);
        var app = new ApplicationProfileInstance();

        ApplicationProfileInstanceChildMembership.SyncFromResolvedLinks(
            os,
            app,
            [
                new ApplicationProfileInstancePersonResolvedLink
                {
                    LinkKind = ApplicationProfileInstancePersonLinkKind.Visa,
                    LinkedObjectId = visaId,
                },
                new ApplicationProfileInstancePersonResolvedLink
                {
                    LinkKind = ApplicationProfileInstancePersonLinkKind.Education,
                    LinkedObjectId = educationId,
                },
                new ApplicationProfileInstancePersonResolvedLink
                {
                    LinkKind = ApplicationProfileInstancePersonLinkKind.TravelHistory,
                    LinkedObjectId = travelId,
                },
            ]);

        Assert.Single(app.Visas!);
        Assert.Single(app.Educations!);
        Assert.Single(app.TravelHistories!);
    }

    [Fact]
    public void Remove_Visa_drops_membership_without_inverse_when_os_null()
    {
        var visaId = Guid.NewGuid();
        var visa = new Visa { ID = visaId };
        var app = new ApplicationProfileInstance();
        app.Visas!.Add(visa);

        ApplicationProfileInstanceChildMembership.Remove(
            app,
            ApplicationProfileInstancePersonLinkKind.Visa,
            visaId,
            objectSpace: null);

        Assert.Empty(app.Visas!);
    }

    /// <summary>DispatchProxy stub: GetObjectByKey returns seeded entities; other calls fail.</summary>
    private class KeyObjectSpaceStub : DispatchProxy
    {
        private readonly Dictionary<Guid, object> _byId = new();

        public static IObjectSpace Create(params object[] entities)
        {
            var proxy = Create<IObjectSpace, KeyObjectSpaceStub>();
            var stub = (KeyObjectSpaceStub)proxy;
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
            if (targetMethod?.Name == nameof(IObjectSpace.GetObjectByKey))
            {
                if (args is { Length: 1 } && args[0] is Guid id1)
                    return _byId.TryGetValue(id1, out var entity1) ? entity1 : null;

                if (args is { Length: 2 } && args[1] is Guid id2)
                    return _byId.TryGetValue(id2, out var entity2) ? entity2 : null;
            }

            throw new InvalidOperationException(
                $"Unexpected IObjectSpace call: {targetMethod?.Name}");
        }
    }
}
