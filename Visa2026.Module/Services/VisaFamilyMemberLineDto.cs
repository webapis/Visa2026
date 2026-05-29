using System;

namespace Visa2026.Module.Services;

/// <summary>One manual family member line for <see cref="BusinessObjects.Person.VisaApplicationFamilyMembersText"/>.</summary>
public sealed class VisaFamilyMemberLineDto
{
    public Guid RowId { get; init; } = Guid.NewGuid();

    public string FullName { get; set; } = string.Empty;

    public DateTime? BirthDate { get; set; }

    /// <summary>Serialized <see cref="BusinessObjects.Relationship.NameTm"/> (or legacy free text).</summary>
    public string RelationshipNameTm { get; set; } = string.Empty;

    /// <summary>UI-only; not stored in the text field.</summary>
    public Guid? RelationshipOid { get; set; }

    public bool IsLegacyIncomplete { get; set; }
}
