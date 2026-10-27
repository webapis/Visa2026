using System;
using System.Linq;
using Visa2026.Module.Services.EmailDocumentCopies;
using Visa2026.Module.Services.PersonLinkedDocuments;
using Xunit;

namespace Visa2026.Module.Tests.Services.EmailDocumentCopies;

public class EmailDocumentCopiesPlannerTests
{
    private static readonly Guid PersonId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    [Fact]
    public void PlanPerson_PicksCurrentPassport_AndFlagsMissingTypes()
    {
        var snapshot = Snapshot(
            Record("Passport:old", "Old passport", size: 100, isCurrent: false),
            Record("Passport:cur", "Current passport", size: 200, isCurrent: true));

        var plan = EmailDocumentCopiesPlanner.PlanPerson(
            snapshot,
            new[] { EmailDocumentType.Passport, EmailDocumentType.Visa },
            includeAll: false);

        Assert.Equal(PersonId, plan.PersonId);
        Assert.Equal("Ada Lovelace", plan.DisplayName);
        Assert.True(plan.HasAttachment);
        Assert.Equal(200, plan.Bytes);

        var passport = Assert.Single(plan.Lines, l => l.Type == EmailDocumentType.Passport);
        Assert.Equal("Passport:cur", Assert.Single(passport.Records).RecordKey);
        Assert.False(passport.NoFile);
        Assert.False(passport.NoRecord);

        var visa = Assert.Single(plan.Lines, l => l.Type == EmailDocumentType.Visa);
        Assert.True(visa.NoRecord);
        Assert.Empty(visa.Records);
    }

    [Fact]
    public void PlanPerson_IncludeAll_TakesEveryPassportWithAScan()
    {
        var snapshot = Snapshot(
            Record("Passport:a", "A", size: 10, isCurrent: false),
            Record("Passport:b", "B", size: 20, isCurrent: true),
            Record("Passport:empty", "Empty", size: 0, isCurrent: false, hasContent: false));

        var plan = EmailDocumentCopiesPlanner.PlanPerson(
            snapshot,
            new[] { EmailDocumentType.Passport },
            includeAll: true);

        Assert.Equal(2, Assert.Single(plan.Lines).Records.Count);
        Assert.Equal(30, plan.Bytes);
    }

    [Fact]
    public void PlanPerson_FamilyProof_AlwaysTakesEveryFile()
    {
        var snapshot = Snapshot(
            Record("FamilyRelationDocument:1", "Marriage", size: 40),
            Record("FamilyRelationDocument:2", "Birth", size: 50));

        var plan = EmailDocumentCopiesPlanner.PlanPerson(
            snapshot,
            new[] { EmailDocumentType.FamilyProof },
            includeAll: false);

        Assert.Equal(2, Assert.Single(plan.Lines).Records.Count);
        Assert.Equal(90, plan.Bytes);
    }

    [Fact]
    public void PlanPerson_AddressOfResidence_IncludesNestedLodgingUnderChosenAddress()
    {
        const string addressKey = "AddressOfResidence:addr1111111111111111111111111111";
        var snapshot = Snapshot(
            Record(addressKey, "Flat 1", size: 100, isCurrent: true),
            Record(addressKey + "/Lodging:room", "Room scan", size: 25, isNested: true),
            Record("AddressOfResidence:other2222222222222222222222222222/Lodging:skip", "Other room", size: 99, isNested: true));

        var plan = EmailDocumentCopiesPlanner.PlanPerson(
            snapshot,
            new[] { EmailDocumentType.AddressOfResidence },
            includeAll: false);

        var keys = Assert.Single(plan.Lines).Records.Select(r => r.RecordKey).ToArray();
        Assert.Equal(new[] { addressKey, addressKey + "/Lodging:room" }, keys);
        Assert.Equal(125, plan.Bytes);
    }

    [Fact]
    public void PlanPerson_MarksNoFileWhenChosenRecordHasNoScan()
    {
        var snapshot = Snapshot(Record("Passport:cur", "Current passport", size: 0, isCurrent: true, hasContent: false));
        var plan = EmailDocumentCopiesPlanner.PlanPerson(
            snapshot,
            new[] { EmailDocumentType.Passport },
            includeAll: false);

        var line = Assert.Single(plan.Lines);
        Assert.False(line.NoRecord);
        Assert.True(line.NoFile);
        Assert.Empty(line.Records);
    }

    [Fact]
    public void PlanPersonFromKeys_SkipsMissingOrEmptyScans()
    {
        var snapshot = Snapshot(
            Record("Passport:ok", "OK", size: 80),
            Record("Passport:empty", "Empty", size: 0, hasContent: false));

        var plan = EmailDocumentCopiesPlanner.PlanPersonFromKeys(
            snapshot,
            new[] { "Passport:ok", "Passport:empty", "Passport:gone", "not-a-key" });

        var line = Assert.Single(plan.Lines);
        Assert.Equal(EmailDocumentType.Passport, line.Type);
        Assert.Equal("Passport:ok", Assert.Single(line.Records).RecordKey);
        Assert.False(line.NoFile);
    }

    [Fact]
    public void Split_KeepsOnePersonUnderLimit()
    {
        var person = PersonWith(
            Line(EmailDocumentType.Passport, 100),
            Line(EmailDocumentType.Visa, 50));

        var plan = EmailDocumentCopiesPlanner.Split(new[] { person }, limitBytes: 200);

        Assert.False(plan.IsSplit);
        Assert.Empty(plan.TooLarge);
        var message = Assert.Single(plan.Messages);
        var attachment = Assert.Single(message.Attachments);
        Assert.Equal(150, attachment.Bytes);
        Assert.Equal(1, attachment.PartNumber);
        Assert.Equal(1, attachment.PartCount);
    }

    [Fact]
    public void Split_BreaksOversizedPersonByTypeThenPacksMessages()
    {
        var person = PersonWith(
            Line(EmailDocumentType.Passport, 80),
            Line(EmailDocumentType.Visa, 80),
            Line(EmailDocumentType.Education, 80));

        var plan = EmailDocumentCopiesPlanner.Split(new[] { person }, limitBytes: 100);

        Assert.True(plan.IsSplit);
        Assert.Empty(plan.TooLarge);
        Assert.Equal(3, plan.Messages.Count);
        Assert.All(plan.Messages, m => Assert.Single(m.Attachments));
        Assert.Equal(3, plan.Messages[0].Attachments[0].PartCount);
        Assert.Equal(1, plan.Messages[0].Attachments[0].PartNumber);
        Assert.Equal(3, plan.Messages[2].Attachments[0].PartNumber);
    }

    [Fact]
    public void Split_MarksSingleRecordLargerThanLimitAsTooLarge()
    {
        var person = PersonWith(Line(EmailDocumentType.Passport, 500));

        var plan = EmailDocumentCopiesPlanner.Split(new[] { person }, limitBytes: 100);

        Assert.Empty(plan.Messages);
        var tooLarge = Assert.Single(plan.TooLarge);
        Assert.True(tooLarge.TooLarge);
        Assert.Equal(500, tooLarge.Bytes);
    }

    [Fact]
    public void Split_RejectsNonPositiveLimit()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            EmailDocumentCopiesPlanner.Split(Array.Empty<EmailPlanPerson>(), 0));
    }

    [Theory]
    [InlineData(25 * EmailDocumentCopiesPlanner.MegaByte, new[] { 10, 15, 20 })]
    [InlineData(12 * EmailDocumentCopiesPlanner.MegaByte, new[] { 10 })]
    [InlineData(EmailDocumentCopiesPlanner.MegaByte / 2, new[] { 1 })]
    public void AvailableSizesMb_CapsAtSenderMax(long senderMax, int[] expected)
    {
        Assert.Equal(expected, EmailDocumentCopiesPlanner.AvailableSizesMb(senderMax));
    }

    private static PersonLinkedDocumentsSnapshot Snapshot(params PersonLinkedDocumentRecord[] records) =>
        new()
        {
            PersonId = PersonId,
            PersonDisplayName = "Ada Lovelace",
            Sections = new[]
            {
                new PersonLinkedDocumentSection { SectionId = "docs", Records = records }
            }
        };

    private static PersonLinkedDocumentRecord Record(
        string key,
        string label,
        int size,
        bool isCurrent = false,
        bool isNested = false,
        bool hasContent = true) =>
        new()
        {
            RecordKey = key,
            RecordLabel = label,
            IsCurrent = isCurrent,
            IsNested = isNested,
            Files = new[]
            {
                new PersonLinkedDocumentFile
                {
                    FileDataId = hasContent ? Guid.NewGuid() : Guid.Empty,
                    FileName = label + ".jpg",
                    SizeBytes = size,
                    HasContent = hasContent
                }
            }
        };

    private static EmailPlanPerson PersonWith(params EmailPlanTypeLine[] lines) =>
        new()
        {
            PersonId = PersonId,
            DisplayName = "Ada Lovelace",
            Lines = lines
        };

    private static EmailPlanTypeLine Line(EmailDocumentType type, long bytes) =>
        new()
        {
            Type = type,
            Records = new[]
            {
                new EmailPlanRecord
                {
                    RecordKey = type + ":x",
                    Label = type.ToString(),
                    Type = type,
                    Bytes = bytes
                }
            }
        };
}
