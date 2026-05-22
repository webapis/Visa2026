namespace Visa2026.Module.Services;

/// <summary>Rename request from the comma-separated multi-select catalog popup.</summary>
public readonly record struct CatalogRenameRequest(string OldName, string NewName);
