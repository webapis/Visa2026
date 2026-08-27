using System;
using System.Collections.ObjectModel;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class IssuedDocumentLifecycleTests
{
    [Fact]
    public void InvitationItem_IsCancelled_OnlyWhenLinkedCancellationInstanceIsIssued()
    {
        var item = new InvitationItem { ApplicationProfileInstances = new ObservableCollection<ApplicationProfileInstance>() };
        var cancelProfile = new ApplicationProfile { ActionFamily = ApplicationProfileActionFamily.Cancellation };
        var inProcess = new ApplicationProfileInstance
        {
            ApplicationProfile = cancelProfile,
            LatestPrimaryStateCode = ApplicationProfileInstanceProgressStateCodes.ProcessStarted,
        };
        item.ApplicationProfileInstances.Add(inProcess);

        Assert.False(IssuedDocumentLifecycle.IsCancelled(item));

        inProcess.LatestPrimaryStateCode = ApplicationProfileInstanceProgressStateCodes.ProcessIssued;
        Assert.True(IssuedDocumentLifecycle.IsCancelled(item));
    }

    [Fact]
    public void InvitationItem_CancelledWinsOverChanged()
    {
        var item = new InvitationItem { ApplicationProfileInstances = new ObservableCollection<ApplicationProfileInstance>() };
        item.ApplicationProfileInstances.Add(new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile { ActionFamily = ApplicationProfileActionFamily.Cancellation },
            LatestPrimaryStateCode = ApplicationProfileInstanceProgressStateCodes.ProcessIssued,
        });
        item.ApplicationProfileInstances.Add(new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile { ActionFamily = ApplicationProfileActionFamily.Change },
            LatestPrimaryStateCode = ApplicationProfileInstanceProgressStateCodes.ProcessIssued,
        });

        Assert.True(IssuedDocumentLifecycle.IsCancelled(item));
        Assert.False(IssuedDocumentLifecycle.IsChanged(item));
    }

    [Fact]
    public void InvitationItem_IsChanged_WhenChangeInstanceIssued_AndNotCancelled()
    {
        var item = new InvitationItem { ApplicationProfileInstances = new ObservableCollection<ApplicationProfileInstance>() };
        var changeProfile = new ApplicationProfile { ActionFamily = ApplicationProfileActionFamily.Change };
        item.ApplicationProfileInstances.Add(new ApplicationProfileInstance
        {
            ApplicationProfile = changeProfile,
            LatestPrimaryStateCode = ApplicationProfileInstanceProgressStateCodes.ProcessIssued,
        });

        Assert.True(IssuedDocumentLifecycle.IsChanged(item));
        Assert.False(IssuedDocumentLifecycle.IsCancelled(item));
    }

    [Fact]
    public void InvitationItem_IsUsed_WhenIssuedVisaLinked()
    {
        var item = new InvitationItem();
        Assert.False(IssuedDocumentLifecycle.IsUsed(item));

        item.IssuedVisa = new Visa();
        Assert.True(IssuedDocumentLifecycle.IsUsed(item));
    }

    [Fact]
    public void Visa_IsCancelled_FromSkipNavCancellationInstance()
    {
        var visa = new Visa { ApplicationProfileInstances = new ObservableCollection<ApplicationProfileInstance>() };
        visa.ApplicationProfileInstances.Add(new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile { ActionFamily = ApplicationProfileActionFamily.Cancellation },
            LatestPrimaryStateCode = ApplicationProfileInstanceProgressStateCodes.ProcessIssued,
        });

        Assert.True(IssuedDocumentLifecycle.IsCancelled(visa));
        Assert.False(IssuedDocumentLifecycle.IsChanged(visa));
    }
}
