using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Security;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF.PermissionPolicy;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.DatabaseUpdate.LookupCatalogs;

/// <summary>Tenant-specific application users from <c>tenant/tenant-users.json</c>.</summary>
internal sealed class TenantUserCatalog
{
    public List<TenantUserRow> Rows { get; set; } = new();
}

internal sealed class TenantUserRow
{
    public string UserName { get; set; } = string.Empty;

    public List<string> Roles { get; set; } = new();
}

internal static class TenantUserCatalogLoader
{
    private const string FileName = "tenant-users.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static TenantUserCatalog? Load()
    {
        var json = LookupCatalogResourceLoader.TryReadTenantOverlayText(FileName)
            ?? LookupCatalogResourceLoader.TryReadEmbeddedLookupCatalogText("tenant/" + FileName);
        if (string.IsNullOrWhiteSpace(json))
            return null;

        return JsonSerializer.Deserialize<TenantUserCatalog>(json, JsonOptions);
    }
}

internal static class TenantUserCatalogSync
{
    public static void Sync(IObjectSpace objectSpace, UserManager userManager)
    {
        var catalog = TenantUserCatalogLoader.Load();
        if (catalog?.Rows is not { Count: > 0 })
        {
            Tracing.Tracer.LogText("TenantUserCatalogSync: no rows in tenant-users.json (embedded or disk overlay).");
            return;
        }

        foreach (var row in catalog.Rows)
        {
            if (string.IsNullOrWhiteSpace(row.UserName))
                continue;

            var userName = row.UserName.Trim();
            var user = userManager.FindUserByName<ApplicationUser>(objectSpace, userName);
            if (user == null)
            {
                _ = userManager.CreateUser<ApplicationUser>(
                    objectSpace,
                    userName,
                    string.Empty,
                    created => EnsureRoles(created, row.Roles, objectSpace));
                Tracing.Tracer.LogText($"TenantUserCatalogSync: created user '{userName}'.");
            }
            else
            {
                EnsureRoles(user, row.Roles, objectSpace);
                Tracing.Tracer.LogText($"TenantUserCatalogSync: ensured roles for '{userName}'.");
            }
        }
    }

    private static void EnsureRoles(
        ApplicationUser user,
        IReadOnlyList<string>? roleNames,
        IObjectSpace objectSpace)
    {
        if (roleNames == null || roleNames.Count == 0)
            return;

        foreach (var roleName in roleNames)
        {
            if (string.IsNullOrWhiteSpace(roleName))
                continue;

            var normalized = roleName.Trim();
            if (user.Roles.Any(r => string.Equals(r.Name, normalized, StringComparison.Ordinal)))
                continue;

            var role = objectSpace.FirstOrDefault<PermissionPolicyRole>(r => r.Name == normalized);
            if (role != null)
                user.Roles.Add(role);
        }
    }
}
