using System;

namespace Visa2026.Module.BusinessObjects;

/// <summary>Marks a lookup BO as a global catalog row type for <see cref="LookupLocalization"/>.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class GlobalLookupCatalogAttribute : Attribute
{
    public GlobalLookupCatalogAttribute(GlobalLookupCatalogKind kind) => Kind = kind;

    public GlobalLookupCatalogKind Kind { get; }
}
