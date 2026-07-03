namespace Visa2026.DataImporter.Legacy.Visa2014;

internal static class Visa2014FilesImportCommand
{
    public static async Task<int> RunAsync(IReadOnlyList<string> args, bool verbose)
    {
        var entity = GetOptionValue(args, "--entity");
        if (string.IsNullOrWhiteSpace(entity))
        {
            Console.Error.WriteLine("ERR --import-visa2014-files requires --entity <Name> (e.g. Person, Passport).");
            return 1;
        }

        var property = GetOptionValue(args, "--property");
        if (string.IsNullOrWhiteSpace(property))
        {
            Console.Error.WriteLine("ERR --import-visa2014-files requires --property <Name> (e.g. Photo, PassportDocument).");
            return 1;
        }

        if (!HasArg(args, "--inprocess"))
        {
            Console.Error.WriteLine("ERR --import-visa2014-files requires --inprocess (headless XAF ObjectSpace). OData file writes are not supported.");
            return 1;
        }

        var dataImporterRoot = Visa2014ContentRoot.FindDataImporterRoot();
        if (dataImporterRoot == null)
        {
            Console.Error.WriteLine("ERR Could not locate Visa2026.DataImporter content root.");
            return 1;
        }

        var solutionRoot = Visa2014ContentRoot.FindSolutionRoot();
        Visa2014LegacySourceProfile source;
        try
        {
            source = Visa2014LegacySource.Resolve(dataImporterRoot, solutionRoot, args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERR {ex.Message}");
            return 1;
        }

        int? maxRows = null;
        var maxRowsText = GetOptionValue(args, "--max-rows");
        if (int.TryParse(maxRowsText, out var parsedMax) && parsedMax > 0)
            maxRows = parsedMax;

        bool dryRun = HasArg(args, "--dry-run");
        bool isFamilyProjectContract = string.Equals(entity, "Person", StringComparison.OrdinalIgnoreCase)
            && string.Equals(property, "FamilyMemberProjectContract", StringComparison.OrdinalIgnoreCase);

        Console.WriteLine($"=== VISA2014 file import — {entity}.{property} (headless XAF)");
        Console.WriteLine($"INF Legacy source: {source.Id} ({source.Label})");

        var targetConnection = GetTargetConnection(args);
        if (string.IsNullOrWhiteSpace(targetConnection) && !dryRun)
        {
            Console.Error.WriteLine("ERR --inprocess requires --target-connection or ConnectionStrings__DefaultConnection.");
            return 1;
        }

        Console.WriteLine($"INF Target (write): Visa2026 in-process ObjectSpace");
        if (!string.IsNullOrWhiteSpace(targetConnection))
            Console.WriteLine($"INF Target SQL: {MaskConnectionForLog(targetConnection)}");

        if (!isFamilyProjectContract)
            Console.WriteLine($"INF Legacy (read-only): {Visa2014LegacySqlGuard.DescribeLegacyConnection(source.ConnectionString, source.LegacyDatabase)}");

        if (maxRows.HasValue)
            Console.WriteLine($"INF Max rows: {maxRows.Value}");
        if (dryRun)
            Console.WriteLine("INF Mode: dry-run (no writes)");

        if (!dryRun && !isFamilyProjectContract)
        {
            try
            {
                Visa2014LegacySqlGuard.EnsureLegacyReadCredentials(source.ConnectionString);
                await Visa2014LegacySqlGuard.EnsureLegacyConnectionAsync(source.ConnectionString);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"ERR {ex.Message}");
                return 1;
            }
        }

        IVisa2014ImportTarget target;
        Visa2014HeadlessImportSession? session = null;

        try
        {
            if (dryRun)
            {
                target = new Visa2014DryRunImportTarget();
            }
            else
            {
                var batchSize = ResolveBatchSize(args);
                session = await Visa2014HeadlessImportSession.OpenAsync(targetConnection!, batchSize);
                target = session.Target;
            }

            if (string.Equals(entity, "Person", StringComparison.OrdinalIgnoreCase))
                return await RunPersonFileImportAsync(target, source, dataImporterRoot, args, property, maxRows, dryRun, verbose);

            if (string.Equals(entity, "Passport", StringComparison.OrdinalIgnoreCase))
                return await RunPassportFileImportAsync(target, source, dataImporterRoot, args, property, maxRows, dryRun, verbose);

            if (string.Equals(entity, "Visa", StringComparison.OrdinalIgnoreCase))
                return await RunVisaFileImportAsync(target, source, dataImporterRoot, args, property, maxRows, dryRun, verbose);

            if (string.Equals(entity, "Education", StringComparison.OrdinalIgnoreCase))
                return await RunEducationFileImportAsync(target, source, dataImporterRoot, args, property, maxRows, dryRun, verbose);

            if (string.Equals(entity, "MedicalRecord", StringComparison.OrdinalIgnoreCase))
            {
                if (session == null)
                {
                    Console.Error.WriteLine("ERR MedicalRecord file import requires a live headless session (not dry-run).");
                    return 1;
                }

                return await RunMedicalRecordFileImportAsync(
                    target, session.ObjectSpaceFactory, source, dataImporterRoot, args, property, maxRows, dryRun, verbose);
            }

            Console.Error.WriteLine($"ERR Entity '{entity}' is not supported yet. Supported: Person, Passport, Visa, Education, MedicalRecord.");
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERR File import failed: {ex.Message}");
            if (verbose)
                Console.Error.WriteLine(ex);
            return 1;
        }
        finally
        {
            if (session != null)
                await session.DisposeAsync();
        }
    }

    private static async Task<int> RunPersonFileImportAsync(
        IVisa2014ImportTarget target,
        Visa2014LegacySourceProfile source,
        string dataImporterRoot,
        IReadOnlyList<string> args,
        string property,
        int? maxRows,
        bool dryRun,
        bool verbose)
    {
        var isPhoto = string.Equals(property, "Photo", StringComparison.OrdinalIgnoreCase);
        var isFamilyText = string.Equals(property, "VisaApplicationFamilyMembersText", StringComparison.OrdinalIgnoreCase);
        var isFamilyProjectContract = string.Equals(property, "FamilyMemberProjectContract", StringComparison.OrdinalIgnoreCase);
        if (!isPhoto && !isFamilyText && !isFamilyProjectContract)
        {
            Console.Error.WriteLine($"ERR Property '{property}' is not supported for Person. Supported: Photo, VisaApplicationFamilyMembersText, FamilyMemberProjectContract.");
            return 1;
        }

        if (isFamilyProjectContract)
        {
            var targetCs = GetTargetConnection(args);
            var result = await Visa2014FamilyMemberProjectContractSync.RunAsync(
                target,
                targetCs,
                maxRows,
                dryRun,
                verbose);

            Console.WriteLine($"INF Family members scanned: {result.FamilyMembersScanned}");
            Console.WriteLine(
                $"INF Patched: {result.Patched}  Failed: {result.Failed}  " +
                $"Already synced: {result.SkippedAlreadySynced}  No person map: {result.SkippedNoPersonMap}  " +
                $"No sponsor map: {result.SkippedNoSponsorMap}  No sponsor contract: {result.SkippedNoSponsorContract}");

            foreach (var error in result.Errors.Take(20))
                Console.Error.WriteLine($"ERR {error}");
            if (result.Errors.Count > 20)
                Console.Error.WriteLine($"ERR ... and {result.Errors.Count - 20} more");

            return result.Failed > 0 ? 1 : 0;
        }

        var idMapPath = GetOptionValue(args, "--id-map")
            ?? GetOptionValue(args, "--id-map-output")
            ?? source.IdMapPath(dataImporterRoot, "Person");

        Console.WriteLine($"INF Id-map: {idMapPath}");

        if (isPhoto)
        {
            var result = await Visa2014PersonPhotoImporter.RunAsync(
                target,
                source.ConnectionString,
                idMapPath,
                maxRows,
                dryRun,
                verbose);

            Console.WriteLine($"INF Id-map entries: {result.IdMapEntries}");
            Console.WriteLine($"INF Processed: {result.Processed}  Patched: {result.Patched}  No blob: {result.SkippedNoBlob}  Failed: {result.Failed}");

            foreach (var error in result.Errors.Take(20))
                Console.Error.WriteLine($"ERR {error}");
            if (result.Errors.Count > 20)
                Console.Error.WriteLine($"ERR ... and {result.Errors.Count - 20} more");

            return result.Failed > 0 ? 1 : 0;
        }

        var familyResult = await Visa2014PersonVisaFamilyTextImporter.RunAsync(
            target,
            source.ConnectionString,
            idMapPath,
            maxRows,
            dryRun,
            verbose);

        Console.WriteLine($"INF Id-map entries: {familyResult.IdMapEntries}");
        Console.WriteLine(
            $"INF Processed: {familyResult.Processed}  Patched: {familyResult.Patched}  " +
            $"Single→Ýok: {familyResult.PatchedSingleNone}  " +
            $"Not employee: {familyResult.SkippedNotEmployee}  No StatusL text: {familyResult.SkippedNoText}  Failed: {familyResult.Failed}");

        foreach (var error in familyResult.Errors.Take(20))
            Console.Error.WriteLine($"ERR {error}");
        if (familyResult.Errors.Count > 20)
            Console.Error.WriteLine($"ERR ... and {familyResult.Errors.Count - 20} more");

        return familyResult.Failed > 0 ? 1 : 0;
    }

    private static async Task<int> RunPassportFileImportAsync(
        IVisa2014ImportTarget target,
        Visa2014LegacySourceProfile source,
        string dataImporterRoot,
        IReadOnlyList<string> args,
        string property,
        int? maxRows,
        bool dryRun,
        bool verbose)
    {
        var isPassportDocument = string.Equals(property, "PassportDocument", StringComparison.OrdinalIgnoreCase)
            || string.Equals(property, "PassportCopy", StringComparison.OrdinalIgnoreCase);
        if (!isPassportDocument)
        {
            Console.Error.WriteLine($"ERR Property '{property}' is not supported for Passport. Supported: PassportDocument (PassportCopy).");
            return 1;
        }

        var passportIdMapPath = GetOptionValue(args, "--passport-id-map")
            ?? GetOptionValue(args, "--id-map")
            ?? source.IdMapPath(dataImporterRoot, "Passport");
        var copyIdMapPath = GetOptionValue(args, "--copy-id-map-output")
            ?? source.IdMapPath(dataImporterRoot, "PassportCopy");

        Console.WriteLine($"INF Passport id-map: {passportIdMapPath}");
        Console.WriteLine($"INF Copy id-map: {copyIdMapPath}");

        var result = await Visa2014PassportCopyImporter.RunAsync(
            target,
            source.ConnectionString,
            passportIdMapPath,
            dryRun ? null : copyIdMapPath,
            maxRows,
            dryRun,
            verbose);

        Console.WriteLine($"INF Passport id-map entries: {result.PassportIdMapEntries}");
        Console.WriteLine($"INF Legacy copy rows: {result.LegacyCopyRows}");
        Console.WriteLine(
            $"INF Posted: {result.Posted}  Failed: {result.Failed}  " +
            $"No passport map: {result.SkippedNoPassportMap}  No blob: {result.SkippedNoBlob}  " +
            $"Oversize (>5MB): {result.SkippedOversize}  Already imported: {result.SkippedAlreadyImported}  " +
            $"Duplicate blob: {result.SkippedDuplicateBlob}");
        if (result.CopyIdMapPath != null)
            Console.WriteLine($"INF Copy id-map: {result.CopyIdMapPath}");

        foreach (var error in result.Errors.Take(20))
            Console.Error.WriteLine($"ERR {error}");
        if (result.Errors.Count > 20)
            Console.Error.WriteLine($"ERR ... and {result.Errors.Count - 20} more");

        return result.Failed > 0 ? 1 : 0;
    }

    private static async Task<int> RunVisaFileImportAsync(
        IVisa2014ImportTarget target,
        Visa2014LegacySourceProfile source,
        string dataImporterRoot,
        IReadOnlyList<string> args,
        string property,
        int? maxRows,
        bool dryRun,
        bool verbose)
    {
        var isVisaDocument = string.Equals(property, "VisaDocument", StringComparison.OrdinalIgnoreCase)
            || string.Equals(property, "GöçürmeNusga", StringComparison.OrdinalIgnoreCase);
        if (!isVisaDocument)
        {
            Console.Error.WriteLine($"ERR Property '{property}' is not supported for Visa. Supported: VisaDocument (GöçürmeNusga).");
            return 1;
        }

        var visaIdMapPath = GetOptionValue(args, "--visa-id-map")
            ?? GetOptionValue(args, "--id-map")
            ?? source.IdMapPath(dataImporterRoot, "Visa");
        var documentIdMapPath = GetOptionValue(args, "--document-id-map-output")
            ?? source.IdMapPath(dataImporterRoot, "VisaDocument");

        Console.WriteLine($"INF Visa id-map: {visaIdMapPath}");
        Console.WriteLine($"INF Document id-map: {documentIdMapPath}");

        var result = await Visa2014VisaDocumentImporter.RunAsync(
            target,
            source.ConnectionString,
            visaIdMapPath,
            dryRun ? null : documentIdMapPath,
            maxRows,
            dryRun,
            verbose);

        Console.WriteLine($"INF Visa id-map entries: {result.VisaIdMapEntries}");
        Console.WriteLine($"INF Rows with blob processed: {result.LegacyRowsWithBlob}");
        Console.WriteLine(
            $"INF Posted: {result.Posted}  Failed: {result.Failed}  " +
            $"No visa map: {result.SkippedNoVisaMap}  No blob: {result.SkippedNoBlob}  " +
            $"Oversize (>5MB): {result.SkippedOversize}  Already imported: {result.SkippedAlreadyImported}");
        if (result.DocumentIdMapPath != null)
            Console.WriteLine($"INF Document id-map: {result.DocumentIdMapPath}");

        foreach (var error in result.Errors.Take(20))
            Console.Error.WriteLine($"ERR {error}");
        if (result.Errors.Count > 20)
            Console.Error.WriteLine($"ERR ... and {result.Errors.Count - 20} more");

        return result.Failed > 0 ? 1 : 0;
    }

    private static async Task<int> RunEducationFileImportAsync(
        IVisa2014ImportTarget target,
        Visa2014LegacySourceProfile source,
        string dataImporterRoot,
        IReadOnlyList<string> args,
        string property,
        int? maxRows,
        bool dryRun,
        bool verbose)
    {
        var isEducationDocument = string.Equals(property, "EducationDocument", StringComparison.OrdinalIgnoreCase)
            || string.Equals(property, "DiplomaCopy", StringComparison.OrdinalIgnoreCase)
            || string.Equals(property, "Diploma", StringComparison.OrdinalIgnoreCase);
        if (!isEducationDocument)
        {
            Console.Error.WriteLine($"ERR Property '{property}' is not supported for Education. Supported: EducationDocument (DiplomaCopy).");
            return 1;
        }

        var educationIdMapPath = GetOptionValue(args, "--education-id-map")
            ?? GetOptionValue(args, "--id-map")
            ?? source.IdMapPath(dataImporterRoot, "Education");
        var documentIdMapPath = GetOptionValue(args, "--document-id-map-output")
            ?? source.IdMapPath(dataImporterRoot, "EducationDocument");

        Console.WriteLine($"INF Education id-map: {educationIdMapPath}");
        Console.WriteLine($"INF Document id-map: {documentIdMapPath}");

        var result = await Visa2014EducationDocumentImporter.RunAsync(
            target,
            source.ConnectionString,
            educationIdMapPath,
            dryRun ? null : documentIdMapPath,
            maxRows,
            dryRun,
            verbose);

        Console.WriteLine($"INF Education id-map entries: {result.EducationIdMapEntries}");
        Console.WriteLine($"INF Legacy diploma copy rows: {result.LegacyCopyRows}");
        Console.WriteLine(
            $"INF Posted: {result.Posted}  Failed: {result.Failed}  " +
            $"No education map: {result.SkippedNoEducationMap}  No blob: {result.SkippedNoBlob}  " +
            $"Oversize (>5MB): {result.SkippedOversize}  Already imported: {result.SkippedAlreadyImported}  " +
            $"Duplicate blob: {result.SkippedDuplicateBlob}");
        if (result.DocumentIdMapPath != null)
            Console.WriteLine($"INF Document id-map: {result.DocumentIdMapPath}");

        foreach (var error in result.Errors.Take(20))
            Console.Error.WriteLine($"ERR {error}");
        if (result.Errors.Count > 20)
            Console.Error.WriteLine($"ERR ... and {result.Errors.Count - 20} more");

        return result.Failed > 0 ? 1 : 0;
    }

    private static async Task<int> RunMedicalRecordFileImportAsync(
        IVisa2014ImportTarget target,
        DevExpress.ExpressApp.INonSecuredObjectSpaceFactory objectSpaceFactory,
        Visa2014LegacySourceProfile source,
        string dataImporterRoot,
        IReadOnlyList<string> args,
        string property,
        int? maxRows,
        bool dryRun,
        bool verbose)
    {
        var isMedicalRecordDocument = string.Equals(property, "MedicalRecordDocument", StringComparison.OrdinalIgnoreCase)
            || string.Equals(property, "SpidKepilnama", StringComparison.OrdinalIgnoreCase);
        if (!isMedicalRecordDocument)
        {
            Console.Error.WriteLine(
                $"ERR Property '{property}' is not supported for MedicalRecord. Supported: MedicalRecordDocument (SpidKepilnama).");
            return 1;
        }

        var personIdMapPath = GetOptionValue(args, "--person-id-map")
            ?? GetOptionValue(args, "--id-map")
            ?? source.IdMapPath(dataImporterRoot, "Person");
        var medicalRecordIdMapPath = GetOptionValue(args, "--medical-record-id-map-output")
            ?? source.IdMapPath(dataImporterRoot, "MedicalRecord");
        var documentIdMapPath = GetOptionValue(args, "--document-id-map-output")
            ?? source.IdMapPath(dataImporterRoot, "MedicalRecordDocument");

        Console.WriteLine($"INF Person id-map: {personIdMapPath}");
        Console.WriteLine($"INF MedicalRecord id-map: {medicalRecordIdMapPath}");
        Console.WriteLine($"INF Document id-map: {documentIdMapPath}");

        var result = await Visa2014MedicalRecordDocumentImporter.RunAsync(
            target,
            objectSpaceFactory,
            source.ConnectionString,
            personIdMapPath,
            dryRun ? null : medicalRecordIdMapPath,
            dryRun ? null : documentIdMapPath,
            maxRows,
            dryRun,
            verbose);

        Console.WriteLine($"INF Person id-map entries: {result.PersonIdMapEntries}");
        Console.WriteLine($"INF Spid link rows: {result.LegacySpidLinkRows}  Importable (Copy+FileData): {result.LegacyImportableRows}");
        Console.WriteLine(
            $"INF Posted: {result.Posted}  Failed: {result.Failed}  " +
            $"No person map: {result.SkippedNoPersonMap}  Orphan copy link: {result.SkippedOrphanCopy}  " +
            $"No blob: {result.SkippedNoBlob}  No audit: {result.SkippedNoAudit}  " +
            $"Oversize (>5MB): {result.SkippedOversize}  Already imported: {result.SkippedAlreadyImported}  " +
            $"Duplicate blob: {result.SkippedDuplicateBlob}");
        if (result.MedicalRecordIdMapPath != null)
            Console.WriteLine($"INF MedicalRecord id-map: {result.MedicalRecordIdMapPath}");
        if (result.DocumentIdMapPath != null)
            Console.WriteLine($"INF Document id-map: {result.DocumentIdMapPath}");

        foreach (var error in result.Errors.Take(20))
            Console.Error.WriteLine($"ERR {error}");
        if (result.Errors.Count > 20)
            Console.Error.WriteLine($"ERR ... and {result.Errors.Count - 20} more");

        return result.Failed > 0 ? 1 : 0;
    }

    private static int ResolveBatchSize(IReadOnlyList<string> args)
    {
        var text = GetOptionValue(args, "--batch-size");
        return int.TryParse(text, out var size) && size > 0 ? size : 50;
    }

    private static bool HasArg(IReadOnlyList<string> args, string flag) =>
        args.Any(a => string.Equals(a, flag, StringComparison.OrdinalIgnoreCase));

    private static string? GetOptionValue(IReadOnlyList<string> args, string optionName)
    {
        for (int i = 0; i < args.Count; i++)
        {
            if (!string.Equals(args[i], optionName, StringComparison.OrdinalIgnoreCase))
                continue;

            if (i + 1 < args.Count && !args[i + 1].StartsWith('-'))
                return args[i + 1];
            return null;
        }

        return null;
    }

    private static string GetTargetConnection(IReadOnlyList<string> args) =>
        GetOptionValue(args, "--target-connection")
        ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
        ?? Environment.GetEnvironmentVariable("VISA2026_SQL_CONNECTION")
        ?? "Server=(localdb)\\mssqllocaldb;Database=Visa2026;Trusted_Connection=True;TrustServerCertificate=True";

    private static string MaskConnectionForLog(string connectionString)
    {
        string? server = null;
        string? database = null;
        bool trusted = connectionString.Contains("Trusted_Connection=True", StringComparison.OrdinalIgnoreCase);

        foreach (var part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            if (part.StartsWith("Server=", StringComparison.OrdinalIgnoreCase))
                server = part["Server=".Length..].Trim();
            else if (part.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
                server = part["Data Source=".Length..].Trim();
            else if (part.StartsWith("Database=", StringComparison.OrdinalIgnoreCase))
                database = part["Database=".Length..].Trim();
            else if (part.StartsWith("Initial Catalog=", StringComparison.OrdinalIgnoreCase))
                database = part["Initial Catalog=".Length..].Trim();
        }

        return $"Server={server ?? "?"};Database={database ?? "?"};Auth={(trusted ? "Windows" : "SQL")}";
    }
}
