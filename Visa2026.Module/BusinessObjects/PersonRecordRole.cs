namespace Visa2026.Module.BusinessObjects;

/// <summary>Discriminator for <see cref="Person"/> records (single table, separate navigation per role).</summary>
public enum PersonRecordRole
{
    Employee = 0,
    FamilyMember = 1,
    TemporaryVisitor = 2,
}
