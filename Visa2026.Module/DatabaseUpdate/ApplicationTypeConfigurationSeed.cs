using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Code-first <see cref="BusinessObjects.ApplicationType"/> configuration.
/// Source of truth: <c>Visa2026.Module/DatabaseUpdate/LookupCatalogs/ApplicationTypeConfigurationCatalog.json</c> (embedded resource).
/// </summary>
internal static class ApplicationTypeConfigurationSeed
{
    private static readonly Lazy<ApplicationTypeConfigurationRow[]> RowsLazy =
        new(CreateRows, isThreadSafe: true);

    private static readonly Lazy<Dictionary<string, ApplicationTypeConfigurationRow>> ByNameLazy =
        new(() => Rows.ToDictionary(r => r.Name, StringComparer.OrdinalIgnoreCase), isThreadSafe: true);

    public static IReadOnlyList<ApplicationTypeConfigurationRow> Rows => RowsLazy.Value;

    public static bool TryGetByName(string? name, out ApplicationTypeConfigurationRow row) =>
        ByNameLazy.Value.TryGetValue(name ?? "", out row!);

    private static ApplicationTypeConfigurationRow[] CreateRows()
    {
        var json = ReadEmbeddedCatalogJson();
        var catalog = JsonSerializer.Deserialize<ApplicationTypeConfigurationCatalogDto>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        if (catalog?.Rows == null || catalog.Rows.Count == 0)
            throw new InvalidOperationException("ApplicationType configuration catalog contains no rows.");

        var rows = new List<ApplicationTypeConfigurationRow>(catalog.Rows.Count);
        foreach (var r in catalog.Rows)
        {
            if (string.IsNullOrWhiteSpace(r.Name))
                continue;

            var flags = r.Flags ?? new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

            rows.Add(new ApplicationTypeConfigurationRow
            {
                Name = r.Name,
                NameTm = r.NameTm ?? "",
                Code = r.Code ?? "",
                PdfForm_Code = r.PdfForm_Code,
                LifecycleStage = ParseEnum<ApplicationLifecycleStage>(r.LifecycleStage, fallback: ApplicationLifecycleStage.Stay),
                Category = ParseEnum<ApplicationTypeCategory>(r.Category, fallback: ApplicationTypeCategory.Both),
                DurationInDays = r.DurationInDays,

                ShowProjectContract = GetFlag(flags, nameof(ApplicationTypeConfigurationRow.ShowProjectContract)),
                ShowVisaPeriod = GetFlag(flags, nameof(ApplicationTypeConfigurationRow.ShowVisaPeriod)),
                ShowVisaCategory = GetFlag(flags, nameof(ApplicationTypeConfigurationRow.ShowVisaCategory)),
                ShowVisaType = GetFlag(flags, nameof(ApplicationTypeConfigurationRow.ShowVisaType)),
                ShowUrgency = GetFlag(flags, nameof(ApplicationTypeConfigurationRow.ShowUrgency)),
                ShowInvitations = GetFlag(flags, nameof(ApplicationTypeConfigurationRow.ShowInvitations)),
                ShowRejections = GetFlag(flags, nameof(ApplicationTypeConfigurationRow.ShowRejections)),
                ShowWorkPermits = GetFlag(flags, nameof(ApplicationTypeConfigurationRow.ShowWorkPermits)),
                ShowRegistrations = GetFlag(flags, nameof(ApplicationTypeConfigurationRow.ShowRegistrations)),
                ShowVisas = GetFlag(flags, nameof(ApplicationTypeConfigurationRow.ShowVisas)),
                ShowApplicationItems = GetFlag(flags, nameof(ApplicationTypeConfigurationRow.ShowApplicationItems)),
                ShowApplicationReason = GetFlag(flags, nameof(ApplicationTypeConfigurationRow.ShowApplicationReason)),
                ShowMigrationService = GetFlag(flags, nameof(ApplicationTypeConfigurationRow.ShowMigrationService)),
                ShowBusinessTrips = GetFlag(flags, nameof(ApplicationTypeConfigurationRow.ShowBusinessTrips)),
                ShowFromCity = GetFlag(flags, nameof(ApplicationTypeConfigurationRow.ShowFromCity)),
                ShowToCity = GetFlag(flags, nameof(ApplicationTypeConfigurationRow.ShowToCity)),
                ShowMovementPermitLocation = GetFlag(flags, nameof(ApplicationTypeConfigurationRow.ShowMovementPermitLocation)),
                ShowBorderZoneLocation = GetFlag(flags, nameof(ApplicationTypeConfigurationRow.ShowBorderZoneLocation)),
                ShowPreviousPassport = GetFlag(flags, nameof(ApplicationTypeConfigurationRow.ShowPreviousPassport)),
                ShowCurrentVisa = GetFlag(flags, nameof(ApplicationTypeConfigurationRow.ShowCurrentVisa)),
                ShowNextVisa = GetFlag(flags, nameof(ApplicationTypeConfigurationRow.ShowNextVisa)),
                ShowCurrentWorkPermitItem = GetFlag(flags, nameof(ApplicationTypeConfigurationRow.ShowCurrentWorkPermitItem)),
                ShowPreviousWorkPermitItem = GetFlag(flags, nameof(ApplicationTypeConfigurationRow.ShowPreviousWorkPermitItem)),
                ShowCurrentInvitationItem = GetFlag(flags, nameof(ApplicationTypeConfigurationRow.ShowCurrentInvitationItem)),
                ShowPreviousInvitationItem = GetFlag(flags, nameof(ApplicationTypeConfigurationRow.ShowPreviousInvitationItem)),
                ShowCurrentAddressOfResidence = GetFlag(flags, nameof(ApplicationTypeConfigurationRow.ShowCurrentAddressOfResidence)),
                ShowCurrentEmployeeContract = GetFlag(flags, nameof(ApplicationTypeConfigurationRow.ShowCurrentEmployeeContract)),
                ShowCurrentWorkDuty = GetFlag(flags, nameof(ApplicationTypeConfigurationRow.ShowCurrentWorkDuty)),
                ShowCurrentMedicalRecord = GetFlag(flags, nameof(ApplicationTypeConfigurationRow.ShowCurrentMedicalRecord)),
                ShowCurrentEducation = GetFlag(flags, nameof(ApplicationTypeConfigurationRow.ShowCurrentEducation)),
                ShowInvitationItemIsIssued = GetFlag(flags, nameof(ApplicationTypeConfigurationRow.ShowInvitationItemIsIssued)),
                ShowWorkPermitItemIsIssued = GetFlag(flags, nameof(ApplicationTypeConfigurationRow.ShowWorkPermitItemIsIssued)),
                ShowRejectionIssued = GetFlag(flags, nameof(ApplicationTypeConfigurationRow.ShowRejectionIssued)),
                ShowVisaIssued = GetFlag(flags, nameof(ApplicationTypeConfigurationRow.ShowVisaIssued)),
                ShowVisaIsCancelled = GetFlag(flags, nameof(ApplicationTypeConfigurationRow.ShowVisaIsCancelled)),
                ShowVisaIsChanged = GetFlag(flags, nameof(ApplicationTypeConfigurationRow.ShowVisaIsChanged)),
                ShowInvitationItemIsCancelled = GetFlag(flags, nameof(ApplicationTypeConfigurationRow.ShowInvitationItemIsCancelled)),
                ShowWorkPermitItemIsCancelled = GetFlag(flags, nameof(ApplicationTypeConfigurationRow.ShowWorkPermitItemIsCancelled)),
                ShowInvitationItemIsChanged = GetFlag(flags, nameof(ApplicationTypeConfigurationRow.ShowInvitationItemIsChanged)),
                ShowWorkPermitItemIsChanged = GetFlag(flags, nameof(ApplicationTypeConfigurationRow.ShowWorkPermitItemIsChanged)),
            });
        }

        return rows.ToArray();
    }

    private static string ReadEmbeddedCatalogJson()
    {
        var asm = typeof(ApplicationTypeConfigurationSeed).Assembly;
        var suffix = ".DatabaseUpdate.LookupCatalogs.ApplicationTypeConfigurationCatalog.json";
        var resourceName = asm.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrWhiteSpace(resourceName))
            throw new InvalidOperationException($"Embedded resource not found: *{suffix}");

        using var stream = asm.GetManifestResourceStream(resourceName);
        if (stream == null)
            throw new InvalidOperationException($"Could not open embedded resource stream: {resourceName}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static bool GetFlag(IReadOnlyDictionary<string, bool> flags, string propertyName)
        => flags.TryGetValue(propertyName, out var v) && v;

    private static TEnum ParseEnum<TEnum>(string? value, TEnum fallback) where TEnum : struct
        => Enum.TryParse<TEnum>(value ?? "", ignoreCase: true, out var parsed) ? parsed : fallback;

    private sealed class ApplicationTypeConfigurationCatalogDto
    {
        public List<ApplicationTypeConfigurationRowDto> Rows { get; set; } = new();
    }

    private sealed class ApplicationTypeConfigurationRowDto
    {
        public string? Name { get; set; }
        public string? NameTm { get; set; }
        public string? Code { get; set; }
        public int PdfForm_Code { get; set; }
        public string? LifecycleStage { get; set; }
        public string? Category { get; set; }
        public int DurationInDays { get; set; }
        public Dictionary<string, bool>? Flags { get; set; }
    }
}
