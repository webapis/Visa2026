using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

public sealed class Visa2014PersonIdentityDuplicateGuardTests
{
    [Fact]
    public void TryResolveFromImportRow_finds_canonical_person_by_personal_number()
    {
        var keepId = Guid.NewGuid();
        var guard = new Visa2014PersonIdentityDuplicateGuard();
        guard.RegisterFromImportRow(
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["PersonalNumber"] = "56872306030",
                ["FirstName"] = "Test",
                ["LastName"] = "User",
                ["DateOfBirth"] = new DateTime(1980, 1, 2),
            },
            keepId);

        var row = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["PersonalNumber"] = "56872306030",
            ["FirstName"] = "Other",
            ["LastName"] = "Name",
            ["DateOfBirth"] = new DateTime(1990, 3, 4),
        };

        Assert.Equal(keepId, guard.TryResolveFromImportRow(row));
    }

    [Fact]
    public void TryResolveFromImportRow_finds_canonical_person_by_identity_when_pn_is_sentinel()
    {
        var keepId = Guid.NewGuid();
        var guard = new Visa2014PersonIdentityDuplicateGuard();
        guard.RegisterFromImportRow(
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["PersonalNumber"] = "0",
                ["FirstName"] = "Asim",
                ["LastName"] = "ANUL",
                ["DateOfBirth"] = new DateTime(1958, 4, 4),
            },
            keepId);

        var row = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["PersonalNumber"] = "0",
            ["FirstName"] = "Asim",
            ["LastName"] = "ANUL",
            ["DateOfBirth"] = new DateTime(1958, 4, 4),
        };

        Assert.Equal(keepId, guard.TryResolveFromImportRow(row));
    }

    [Fact]
    public void RegisterFromImportRow_keeps_lowest_id_for_same_key()
    {
        var higherId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var lowerId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var guard = new Visa2014PersonIdentityDuplicateGuard();
        var row = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["PersonalNumber"] = "11458457544",
            ["FirstName"] = "Adnan",
            ["LastName"] = "LEBLEBICI",
            ["DateOfBirth"] = new DateTime(1962, 2, 1),
        };

        guard.RegisterFromImportRow(row, higherId);
        guard.RegisterFromImportRow(row, lowerId);

        Assert.Equal(lowerId, guard.TryResolveFromImportRow(row));
    }

    [Fact]
    public void TryResolveFromImportRow_returns_null_when_identity_incomplete()
    {
        var guard = new Visa2014PersonIdentityDuplicateGuard();
        var row = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["PersonalNumber"] = "0",
            ["FirstName"] = "OnlyFirst",
        };

        Assert.Null(guard.TryResolveFromImportRow(row));
    }
}