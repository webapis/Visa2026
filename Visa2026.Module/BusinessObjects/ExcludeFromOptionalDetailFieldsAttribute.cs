using System;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Excludes a member from the optional-fields gear scope on types marked with
/// <see cref="SupportsOptionalDetailFieldsAttribute"/> (member stays visible when the gear is off).
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class ExcludeFromOptionalDetailFieldsAttribute : Attribute;
