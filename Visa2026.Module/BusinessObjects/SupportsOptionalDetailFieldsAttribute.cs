using System;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Marks a detail view type whose direct scalar/reference members without
/// <see cref="DevExpress.Persistent.Validation.RuleRequiredFieldAttribute"/> are hidden until
/// <see cref="IOptionalDetailFields.ShowOptionalFields"/> is toggled. List/collection properties are excluded.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class SupportsOptionalDetailFieldsAttribute : Attribute;
