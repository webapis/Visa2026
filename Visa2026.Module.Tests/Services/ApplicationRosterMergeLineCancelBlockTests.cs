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
    public void Cancel_block_with_one_value_still_gets_1_prefix()
    {
        var line = new ApplicationRosterMergeLine
        {
            CurrentVisa = new Visa { VisaNumber = "A1634366" },
            CurrentWorkPermitItem = new WorkPermitItem
            {
                WorkPermitNumber = "1249/12",
                WorkPermittedLocations = "Ashgabat",
            },
            PreviousWorkPermitItem = new WorkPermitItem
            {
                WorkPermitNumber = "110/21",
                WorkPermittedLocations = "Ashgabat",
            },
        };

        Assert.Equal("1) A1634366", line.CancelVisa_NumberBlock);
        Assert.Equal($"1) 1249/12{Environment.NewLine}2) 110/21", line.CancelWorkPermit_NumberBlock);
        Assert.Equal("Ashgabat", line.CancelWorkPermit_LocationsBlock);
    }
}