namespace Visa2026.Module.BusinessObjects;

/// Non-persistent UI flag for detail views that hide direct properties without
/// <see cref="DevExpress.Persistent.Validation.RuleRequiredFieldAttribute"/>.
/// List/collection members are not part of this scope.
public interface IOptionalDetailFields
{
    bool ShowOptionalFields { get; set; }
}
