using System;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Early gates for roster detach before a Person master delete (required-field bypass path).
/// </summary>
public class ApplicationProfileInstancePersonDetachForDeleteGateTests
{
    [Fact]
    public void DetachForPersonDelete_NullObjectSpaceOrPerson_DoesNotThrow()
    {
        ApplicationProfileInstancePersonService.DetachForPersonDelete(null!, null!);
        ApplicationProfileInstancePersonService.DetachForPersonDelete(null!, new Person());
    }

    [Fact]
    public void DetachForPersonDelete_EmptyPersonId_DoesNotQueryObjectSpace()
    {
        var person = new Person { ID = Guid.Empty };
        // Would throw if the empty-id gate were removed and GetObjectsQuery ran on null OS.
        ApplicationProfileInstancePersonService.DetachForPersonDelete(null!, person);
    }
}
