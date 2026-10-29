using System;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Cancel-visas linking may include a future-start issued visa, but still rejects
/// cancelled / changed / missing-start rows.
/// </summary>
public class CanLinkVisaIncludeNotYetStartedGateTests
{
    private static readonly DateTime Today = DateTime.Today;

    [Fact]
    public void CanLinkVisa_IncludeNotYetStarted_AllowsFutureStart()
    {
        var next = CreateVisa(Today.AddDays(20), Today.AddMonths(8));

        Assert.False(ApplicationProfileInstancePersonValidItems.CanLinkVisa(next));
        Assert.True(ApplicationProfileInstancePersonValidItems.CanLinkVisa(
            next, includeNotYetStarted: true));
    }

    [Fact]
    public void CanLinkVisa_IncludeNotYetStarted_StillRejectsCancelledChangedAndDefaultStart()
    {
        Assert.False(ApplicationProfileInstancePersonValidItems.CanLinkVisa(
            CreateVisa(Today.AddDays(20), Today.AddMonths(8), cancelled: true),
            includeNotYetStarted: true));
        Assert.False(ApplicationProfileInstancePersonValidItems.CanLinkVisa(
            CreateVisa(Today.AddDays(20), Today.AddMonths(8), changed: true),
            includeNotYetStarted: true));
        Assert.False(ApplicationProfileInstancePersonValidItems.CanLinkVisa(
            new Visa { ExpirationDate = Today.AddMonths(8) },
            includeNotYetStarted: true));
    }

    [Fact]
    public void CanAutoLink_CancelVisas_StillRejectsCancelledVisa()
    {
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile { CancelVisas = true },
        };
        var cancelled = CreateVisa(Today.AddDays(20), Today.AddMonths(8), cancelled: true);

        Assert.False(ApplicationProfileInstancePersonValidItems.CanAutoLink(
            application,
            ApplicationProfileInstancePersonLinkKind.Visa,
            cancelled));
    }

    [Fact]
    public void CanAutoLink_WithoutCancelVisas_RejectsNotYetStartedVisa()
    {
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile { CancelVisas = false },
        };
        var next = CreateVisa(Today.AddDays(20), Today.AddMonths(8));

        Assert.False(ApplicationProfileInstancePersonValidItems.CanAutoLink(
            application,
            ApplicationProfileInstancePersonLinkKind.Visa,
            next));
    }

    private static Visa CreateVisa(
        DateTime start,
        DateTime expiration,
        bool cancelled = false,
        bool changed = false)
    {
        var visa = new Visa
        {
            IssueDate = start,
            StartDate = start,
            ExpirationDate = expiration,
        };
        if (cancelled)
            AttachCompletedFamily(visa, ApplicationProfileActionFamily.Cancellation);
        else if (changed)
            AttachCompletedFamily(visa, ApplicationProfileActionFamily.Change);
        return visa;
    }

    private static void AttachCompletedFamily(Visa visa, ApplicationProfileActionFamily family)
    {
        visa.ApplicationProfileInstances.Add(new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile { ActionFamily = family },
            LatestPrimaryStateCode = ApplicationProfileInstanceProgressStateCodes.ProcessIssued,
        });
    }
}
