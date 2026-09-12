#nullable enable

using System;
using Visa2026.Module.DatabaseUpdate;
using Visa2026.Module.Services.ApplicationPersonRoster;
using Xunit;

namespace Visa2026.Module.Tests.Services.ApplicationPerson;

public class EntitySaveExceptionFormatterTests
{
    [Fact]
    public void ToOfficerMessage_uses_innermost_exception()
    {
        var inner = new InvalidOperationException("duplicate key value violates unique constraint");
        var wrapped = new Exception("An error occurred while saving entity changes. See the inner exception for details.", inner);

        Assert.Equal(inner.Message, EntitySaveExceptionFormatter.ToOfficerMessage(wrapped));
    }

    [Fact]
    public void Heal_resolved_link_index_ignores_soft_deleted_rows()
    {
        Assert.Contains(
            "WHERE \"GCRecord\" IS NULL",
            ApplicationWorkspaceSchemaSql.HealResolvedLinksUniqueIndexPostgres,
            StringComparison.Ordinal);
        Assert.Contains(
            "DROP INDEX IF EXISTS \"IX_ApplicationProfileInstancePersonResolvedLinks_Instance_Person_Kind_Object\"",
            ApplicationWorkspaceSchemaSql.HealResolvedLinksUniqueIndexPostgres,
            StringComparison.Ordinal);
    }
}
