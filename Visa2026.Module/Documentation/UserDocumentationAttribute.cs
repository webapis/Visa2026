namespace Visa2026.Module.Documentation;

/// <summary>
/// Links a business object to officer manual reference content (Layer A catalog).
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class UserDocumentationAttribute : Attribute
{
    public UserDocumentationAttribute(string slug) => Slug = slug;

    public string Slug { get; }

    public string? Category { get; init; }
}
