#nullable enable

using System;
using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationRosterMergeLineCancelBlockTests
{
    [Fact]
    public void Cancel_visa_and_work_permit_blocks_prefix_each_line_with_order()
    {
        var line = new ApplicationRosterMergeLine
        {
            CurrentVisa = new Visa { VisaNumber = "A1634366" },
            NextVisa = new Visa { VisaNumber = "A17327411" },
            CurrentWorkPermitItem = new WorkPermitItem
            {
                WorkPermitNumber = "1022/18",
                ASNumber = "COO1797500",
                WorkPermittedLocations = "Turkmenbashy",
            },
            PreviousWorkPermitItem = new WorkPermitItem
            {
                WorkPermitNumber = "110/21",
                ASNumber = "COO200711",
                WorkPermittedLocations = "Ashgabat",
            },
        };

        var nl = Environment.NewLine;
        Assert.Equal($"1) A1634366{nl}2) A17327411", line.CancelVisa_NumberBlock);
        Assert.Equal($"1) COO1797500{nl}2) COO200711", line.CancelWorkPermit_ASNumberBlock);
        Assert.Equal($"1) 1022/18{nl}2) 110/21", line.CancelWorkPermit_NumberBlock);
        Assert.Equal($"Turkmenbashy{nl}Ashgabat", line.CancelWorkPermit_LocationsBlock);
    }

    [Fact]
    public void Cancel_block_with_one_work_permit_has_no_order_prefix()
    {
        var line = new ApplicationRosterMergeLine
        {
            CurrentVisa = new Visa { VisaNumber = "A1634366" },
            CurrentWorkPermitItem = new WorkPermitItem
            {
                WorkPermitNumber = "1249/12",
                ASNumber = "COO200711",
                WorkPermittedLocations = "Ashgabat",
            },
        };

        Assert.Equal("A1634366", line.CancelVisa_NumberBlock);
        Assert.Equal("1249/12", line.CancelWorkPermit_NumberBlock);
        Assert.Equal("COO200711", line.CancelWorkPermit_ASNumberBlock);
        Assert.Equal("Ashgabat", line.CancelWorkPermit_LocationsBlock);
    }

    [Fact]
    public void Cancel_invitation_blocks_prefix_each_line_when_two_headers()
    {
        var line = new ApplicationRosterMergeLine
        {
            CurrentInvitationItem = new InvitationItem
            {
                Invitation = new Invitation
                {
                    ID = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    InvitationNumber = "COO02058777",
                    IssuedDate = new DateTime(2025, 10, 12),
                    ExpirationDate = new DateTime(2026, 4, 11),
                },
            },
            PreviousInvitationItem = new InvitationItem
            {
                Invitation = new Invitation
                {
                    ID = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                    InvitationNumber = "COO02058801",
                    IssuedDate = new DateTime(2026, 1, 11),
                    ExpirationDate = new DateTime(2026, 7, 11),
                },
            },
        };

        var nl = Environment.NewLine;
        Assert.Equal($"1) COO02058777{nl}2) COO02058801", line.CancelInvitation_NumberBlock);
        Assert.Equal($"1) 12.10.2025{nl}2) 11.01.2026", line.CancelInvitation_IssuedDateBlock);
        Assert.Equal($"1) 11.04.2026{nl}2) 11.07.2026", line.CancelInvitation_ExpirationDateBlock);
    }

    [Fact]
    public void Cancel_invitation_block_with_one_header_has_no_order_prefix()
    {
        var line = new ApplicationRosterMergeLine
        {
            CurrentInvitationItem = new InvitationItem
            {
                Invitation = new Invitation
                {
                    ID = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    InvitationNumber = "COO02058777",
                    IssuedDate = new DateTime(2025, 10, 12),
                    ExpirationDate = new DateTime(2026, 4, 11),
                },
            },
        };

        Assert.Equal("COO02058777", line.CancelInvitation_NumberBlock);
        Assert.Equal("12.10.2025", line.CancelInvitation_IssuedDateBlock);
        Assert.Equal("11.04.2026", line.CancelInvitation_ExpirationDateBlock);
    }
}