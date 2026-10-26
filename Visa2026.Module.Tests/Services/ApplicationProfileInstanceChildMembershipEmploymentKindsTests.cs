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
/// Skip-nav child kinds not covered by open coverage PRs #69 (Passport/WorkDuty)
/// and #76 (Visa/Education/TravelHistory): employment + issued-item membership.
/// </summary>
public class ApplicationProfileInstanceChildMembershipEmploymentKindsTests
{
    [Fact]
    public void Add_employment_and_item_kinds_append_once_each()
    {
        var addressId = Guid.NewGuid();
        var positionId = Guid.NewGuid();
        var salaryId = Guid.NewGuid();
        var medicalId = Guid.NewGuid();
        var invitationItemId = Guid.NewGuid();
        var workPermitItemId = Guid.NewGuid();
        var borderZoneItemId = Guid.NewGuid();

        var address = new AddressOfResidence { ID = addressId };
        var position = new EmployeePositionHistory { ID = positionId };
        var salary = new EmployeeSalary { ID = salaryId };
        var medical = new MedicalRecord { ID = medicalId };
        var invitationItem = new InvitationItem { ID = invitationItemId };
        var workPermitItem = new WorkPermitItem { ID = workPermitItemId };
        var borderZoneItem = new BorderZoneItem { ID = borderZoneItemId };

        var os = KeyObjectSpaceStub.Create(
            address, position, salary, medical, invitationItem, workPermitItem, borderZoneItem);
        var app = new ApplicationProfileInstance();

        ApplicationProfileInstanceChildMembership.Add(
            os, app, ApplicationProfileInstancePersonLinkKind.AddressOfResidence, addressId);
        ApplicationProfileInstanceChildMembership.Add(
            os, app, ApplicationProfileInstancePersonLinkKind.Position, positionId);
        ApplicationProfileInstanceChildMembership.Add(
            os, app, ApplicationProfileInstancePersonLinkKind.Salary, salaryId);
        ApplicationProfileInstanceChildMembership.Add(
            os, app, ApplicationProfileInstancePersonLinkKind.MedicalRecord, medicalId);
        ApplicationProfileInstanceChildMembership.Add(
            os, app, ApplicationProfileInstancePersonLinkKind.InvitationItem, invitationItemId);
        ApplicationProfileInstanceChildMembership.Add(
            os, app, ApplicationProfileInstancePersonLinkKind.WorkPermitItem, workPermitItemId);
        ApplicationProfileInstanceChildMembership.Add(
            os, app, ApplicationProfileInstancePersonLinkKind.BorderZoneItem, borderZoneItemId);

        Assert.Single(app.AddressesOfResidence!);
        Assert.Same(address, app.AddressesOfResidence![0]);
        Assert.Single(app.PositionHistories!);
        Assert.Same(position, app.PositionHistories![0]);
        Assert.Single(app.Salaries!);
        Assert.Same(salary, app.Salaries![0]);
        Assert.Single(app.MedicalRecords!);
        Assert.Same(medical, app.MedicalRecords![0]);
        Assert.Single(app.InvitationItems!);
        Assert.Same(invitationItem, app.InvitationItems![0]);
        Assert.Single(app.WorkPermitItems!);
        Assert.Same(workPermitItem, app.WorkPermitItems![0]);
        Assert.Single(app.BorderZoneItems!);
        Assert.Same(borderZoneItem, app.BorderZoneItems![0]);

        ApplicationProfileInstanceChildMembership.Add(
            os, app, ApplicationProfileInstancePersonLinkKind.AddressOfResidence, addressId);
        Assert.Single(app.AddressesOfResidence!);
    }

    [Fact]
    public void SyncFromResolvedLinks_adds_employment_kinds()
    {
        var addressId = Guid.NewGuid();
        var positionId = Guid.NewGuid();
        var salaryId = Guid.NewGuid();
        var address = new AddressOfResidence { ID = addressId };
        var position = new EmployeePositionHistory { ID = positionId };
        var salary = new EmployeeSalary { ID = salaryId };
        var os = KeyObjectSpaceStub.Create(address, position, salary);
        var app = new ApplicationProfileInstance();

        ApplicationProfileInstanceChildMembership.SyncFromResolvedLinks(
            os,
            app,
            [
                new ApplicationProfileInstancePersonResolvedLink
                {
                    LinkKind = ApplicationProfileInstancePersonLinkKind.AddressOfResidence,
                    LinkedObjectId = addressId,
                },
                new ApplicationProfileInstancePersonResolvedLink
                {
                    LinkKind = ApplicationProfileInstancePersonLinkKind.Position,
                    LinkedObjectId = positionId,
                },
                new ApplicationProfileInstancePersonResolvedLink
                {
                    LinkKind = ApplicationProfileInstancePersonLinkKind.Salary,
                    LinkedObjectId = salaryId,
                },
            ]);

        Assert.Single(app.AddressesOfResidence!);
        Assert.Single(app.PositionHistories!);
        Assert.Single(app.Salaries!);
    }

    [Fact]
    public void Remove_Medical_and_InvitationItem_drop_membership_without_inverse_when_os_null()
    {
        var medicalId = Guid.NewGuid();
        var invitationItemId = Guid.NewGuid();
        var app = new ApplicationProfileInstance();
        app.MedicalRecords!.Add(new MedicalRecord { ID = medicalId });
        app.InvitationItems!.Add(new InvitationItem { ID = invitationItemId });

        ApplicationProfileInstanceChildMembership.Remove(
            app,
            ApplicationProfileInstancePersonLinkKind.MedicalRecord,
            medicalId,
            objectSpace: null);
        ApplicationProfileInstanceChildMembership.Remove(
            app,
            ApplicationProfileInstancePersonLinkKind.InvitationItem,
            invitationItemId,
            objectSpace: null);

        Assert.Empty(app.MedicalRecords!);
        Assert.Empty(app.InvitationItems!);
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
