using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Core;
using Visa2026.Blazor.Server.Services.Migration;
using Bo = Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.MigrationImport;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014ApplicationItemPersonCurrentCorrectionResult
{
    public int ItemsInScope { get; init; }
    public int EducationUpdated { get; init; }
    public int SalaryUpdated { get; init; }
    public int WorkPermitUpdated { get; init; }
    public int Unchanged { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}

/// <summary>
/// Backfills <see cref="Bo.ApplicationItem.CurrentEducation"/>, <see cref="Bo.ApplicationItem.CurrentSalary"/>,
/// and <see cref="Bo.ApplicationItem.CurrentWorkPermitItem"/> from imported person child rows using the same rules
/// as <see cref="Bo.PersonCurrentItems"/>.
/// </summary>
internal static class Visa2014ApplicationItemPersonCurrentCorrection
{
    public static Task<int> RunCommandAsync(IReadOnlyList<string> args, bool verbose)
    {
        var dataImporterRoot = Visa2014ContentRoot.FindDataImporterRoot();
        if (dataImporterRoot == null)
        {
            Console.Error.WriteLine("ERR Could not locate Visa2026.DataImporter content root.");
            return Task.FromResult(1);
        }

        var dryRun = HasArg(args, "--dry-run");
        var targetConnection = GetOptionValue(args, "--target-connection")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Server=(localdb)\\mssqllocaldb;Database=Visa2026;Trusted_Connection=True;";

        Console.WriteLine("=== VISA2014 ApplicationItem person-current correction");
        Console.WriteLine($"INF Target SQL: {MaskConnectionString(targetConnection)}");
        if (dryRun) Console.WriteLine("INF Mode: dry-run (no writes)");

        HeadlessMigrationHost? host = null;
        IDisposable? importScope = null;
        try
        {
            host = HeadlessMigrationHost.Start(targetConnection);
            importScope = MigrationImportContext.BeginDataImportScope();

            var result = Run(host.ObjectSpaceFactory, dryRun, verbose);

            Console.WriteLine($"INF Items in scope: {result.ItemsInScope}");
            Console.WriteLine($"INF CurrentEducation updated: {result.EducationUpdated}");
            Console.WriteLine($"INF CurrentSalary updated: {result.SalaryUpdated}");
            Console.WriteLine($"INF CurrentWorkPermitItem updated: {result.WorkPermitUpdated}");
            Console.WriteLine($"INF Unchanged: {result.Unchanged}");
            foreach (var error in result.Errors.Take(20))
                Console.Error.WriteLine($"ERR {error}");

            return Task.FromResult(result.Errors.Count > 0 ? 1 : 0);
        }
        finally
        {
            importScope?.Dispose();
            host?.Dispose();
        }
    }

    private static Visa2014ApplicationItemPersonCurrentCorrectionResult Run(
        INonSecuredObjectSpaceFactory objectSpaceFactory,
        bool dryRun,
        bool verbose)
    {
        var errors = new List<string>();
        int inScope = 0;
        int educationUpdated = 0;
        int salaryUpdated = 0;
        int workPermitUpdated = 0;
        int unchanged = 0;

        using var objectSpace = objectSpaceFactory.CreateNonSecuredObjectSpace(typeof(Bo.ApplicationItem));
        MigrationImportContext.ApplyImportObjectSpaceHooks(objectSpace);

        var items = objectSpace.GetObjectsQuery<Bo.ApplicationItem>()
            .Where(i => i.Person != null && i.Application != null && i.Application.ApplicationType != null)
            .ToList();

        foreach (var item in items)
        {
            var person = item.Person;
            if (person == null || !person.IsEmployee)
                continue;

            var appType = item.Application!.ApplicationType!;
            var showEducation = appType.ShowCurrentEducation && !appType.ShowRegistrations;
            var showSalary = appType.ShowCurrentSalary;
            var showWorkPermit = appType.ShowCurrentWorkPermitItem;
            if (!showEducation && !showSalary && !showWorkPermit)
                continue;

            inScope++;
            var personInSpace = objectSpace.GetObject(person);
            var changed = false;

            try
            {
                if (showEducation && item.CurrentEducation == null)
                {
                    var education = Bo.PersonCurrentItems.GetCurrentEducation(personInSpace);
                    if (education != null)
                    {
                        item.CurrentEducation = objectSpace.GetObject(education);
                        educationUpdated++;
                        changed = true;
                    }
                }

                if (showSalary && item.CurrentSalary == null)
                {
                    var salary = Bo.PersonCurrentItems.GetCurrentSalary(personInSpace);
                    if (salary != null)
                    {
                        item.CurrentSalary = objectSpace.GetObject(salary);
                        salaryUpdated++;
                        changed = true;
                    }
                }

                if (showWorkPermit && item.CurrentWorkPermitItem == null)
                {
                    var workPermitItem = Bo.PersonCurrentItems.GetCurrentWorkPermitItem(personInSpace);
                    if (workPermitItem != null)
                    {
                        item.CurrentWorkPermitItem = objectSpace.GetObject(workPermitItem);
                        item.WorkPermittedLocations = workPermitItem.WorkPermittedLocations ?? string.Empty;
                        workPermitUpdated++;
                        changed = true;
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add($"{item.ID}: {ex.Message}");
                continue;
            }

            if (!changed)
                unchanged++;
            else if (verbose)
                Console.WriteLine($"  PATCH ApplicationItem {item.ID}");
        }

        if (!dryRun && (educationUpdated > 0 || salaryUpdated > 0 || workPermitUpdated > 0))
            objectSpace.CommitChanges();

        return new Visa2014ApplicationItemPersonCurrentCorrectionResult
        {
            ItemsInScope = inScope,
            EducationUpdated = educationUpdated,
            SalaryUpdated = salaryUpdated,
            WorkPermitUpdated = workPermitUpdated,
            Unchanged = unchanged,
            Errors = errors,
        };
    }

    private static string MaskConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return "(empty)";
        return System.Text.RegularExpressions.Regex.Replace(
            connectionString,
            @"(Password|Pwd)\s*=\s*[^;]+",
            "$1=***",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    private static bool HasArg(IReadOnlyList<string> args, string flag) =>
        args.Any(a => string.Equals(a, flag, StringComparison.OrdinalIgnoreCase));

    private static string? GetOptionValue(IReadOnlyList<string> args, string optionName)
    {
        for (int i = 0; i < args.Count - 1; i++)
        {
            if (string.Equals(args[i], optionName, StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        }

        return null;
    }
}