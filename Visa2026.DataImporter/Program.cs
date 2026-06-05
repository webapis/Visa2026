using Visa2026.DataImporter;

// -----------------------------------------------------------------------
// Logging — writes timestamped entries to console AND a rolling log file.
// -----------------------------------------------------------------------
// -----------------------------------------------------------------------
// Configuration
// -----------------------------------------------------------------------
string ApiBaseUrl = Environment.GetEnvironmentVariable("ApiOptions__BaseUrl")
                 ?? Environment.GetEnvironmentVariable("API_BASE_URL")
                 ?? "https://localhost:5001";
const string UserName   = "Admin";
const string Password   = "";   // empty for default XAF Admin user

static bool HasArg(IEnumerable<string> args, string flag) =>
    args.Any(a => string.Equals(a, flag, StringComparison.OrdinalIgnoreCase));

static string? GetOptionValue(IReadOnlyList<string> args, string optionName)
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

static string? ResolveInputFile(string fileNameOrPath)
{
    var fromCatalog = YamlSeedCatalog.ResolveExistingSeedPath(fileNameOrPath, AppContext.BaseDirectory);
    if (fromCatalog != null)
        return fromCatalog;

    if (Path.IsPathRooted(fileNameOrPath))
        return File.Exists(fileNameOrPath) || Directory.Exists(fileNameOrPath) ? fileNameOrPath : null;

    var outputDirPath = Path.Combine(AppContext.BaseDirectory, fileNameOrPath);
    if (File.Exists(outputDirPath) || Directory.Exists(outputDirPath))
        return outputDirPath;

    return File.Exists(fileNameOrPath) || Directory.Exists(fileNameOrPath) ? fileNameOrPath : null;
}

static string? FindDataImporterContentRoot()
{
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir != null)
    {
        string candidate = Path.Combine(dir.FullName, "Visa2026.DataImporter");
        if (Directory.Exists(candidate))
            return candidate;

        if (File.Exists(Path.Combine(dir.FullName, "Visa2026.DataImporter.csproj")))
            return dir.FullName;

        dir = dir.Parent;
    }

    return null;
}

static ScenarioRunOptions? BuildScenarioRunOptions(IReadOnlyList<string> args)
{
    var options = new ScenarioRunOptions();

    for (int i = 0; i < args.Count; i++)
    {
        if (string.Equals(args[i], "--sync-scenario", StringComparison.OrdinalIgnoreCase))
        {
            if (i + 1 >= args.Count || args[i + 1].StartsWith('-'))
            {
                Log.Error("--sync-scenario requires a scenario name (e.g. InvitationEmployee).");
                return null;
            }
            options.SyncScenarioNames.Add(args[++i]);
            continue;
        }

        if (string.Equals(args[i], "--clear-scenario", StringComparison.OrdinalIgnoreCase))
        {
            if (i + 1 >= args.Count || args[i + 1].StartsWith('-'))
            {
                Log.Error("--clear-scenario requires a scenario name (e.g. InvitationEmployee).");
                return null;
            }
            options.ClearScenarioNames.Add(args[++i]);
        }
    }

    if (HasArg(args, "--sync"))
        options.SyncAllFlaggedInYaml = true;

    return options.HasTargetedRun ? options : null;
}

/// <summary>Seed path: DATA_YAML_PATH env, --import-yaml-only [path], first positional arg, or seed/scenarios.index.yaml.</summary>
static string ResolveYamlFileSpec(IReadOnlyList<string> args)
{
    var fromEnv = Environment.GetEnvironmentVariable("DATA_YAML_PATH");
    if (!string.IsNullOrWhiteSpace(fromEnv))
        return fromEnv.Trim();

    var fromFlag = GetOptionValue(args, "--import-yaml-only");
    if (!string.IsNullOrWhiteSpace(fromFlag))
        return fromFlag;

    // Skip values consumed by flags (e.g. --sync-scenario InvitationEmployee).
    var flagsWithValue = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "--import-yaml-only",
        "--sync-scenario",
        "--clear-scenario",
    };

    for (int i = 0; i < args.Count; i++)
    {
        var token = args[i];
        if (token.StartsWith('-'))
        {
            if (flagsWithValue.Contains(token) &&
                i + 1 < args.Count &&
                !args[i + 1].StartsWith('-'))
            {
                i++;
            }
            continue;
        }

        return token;
    }

    return YamlSeedCatalog.DefaultIndexRelativePath;
}

static IReadOnlyList<string> GetUnknownFlags(IReadOnlyList<string> args)
{
    var knownFlags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "--import-yaml-only",
        "--full",
        "--sync",
        "--sync-scenario",
        "--clear-scenario",
        "--dump-lookups",
        "--export-lookup-catalogs",
        "--export-seed",
        "--validate-seed",
        "--prune-seed",
        "--skip-visibility-preflight",
        "--verbose",
        "-v",
        "--help",
        "-h"
    };

    var unknown = new List<string>();
    for (int i = 0; i < args.Count; i++)
    {
        var token = args[i];
        if (!token.StartsWith('-'))
            continue;

        if (knownFlags.Contains(token))
        {
            // --import-yaml-only optionally accepts a value immediately after it.
            if (string.Equals(token, "--import-yaml-only", StringComparison.OrdinalIgnoreCase) &&
                i + 1 < args.Count &&
                !args[i + 1].StartsWith('-'))
            {
                i++;
            }
            else if ((string.Equals(token, "--sync-scenario", StringComparison.OrdinalIgnoreCase) ||
                      string.Equals(token, "--clear-scenario", StringComparison.OrdinalIgnoreCase)) &&
                     i + 1 < args.Count &&
                     !args[i + 1].StartsWith('-'))
            {
                i++;
            }
            continue;
        }

        unknown.Add(token);
    }

    return unknown;
}

static void PrintHelp()
{
    Console.WriteLine("Visa2026.DataImporter");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  dotnet run --project Visa2026.DataImporter");
    Console.WriteLine("  dotnet Visa2026.DataImporter.dll [path-to.yaml]");
    Console.WriteLine();
    Console.WriteLine("Default (no flags): import seed/scenarios (index + fragments, bundled next to the executable).");
    Console.WriteLine("  Optional: pass seed/scenarios.index.yaml, a scenario file, legacy data.yaml, or set DATA_YAML_PATH.");
    Console.WriteLine("  Legacy alias: --import-yaml-only [path]");
    Console.WriteLine();
    Console.WriteLine("Scenario refresh (no full DB wipe):");
    Console.WriteLine("  --clear-scenario <Name>  Delete scenario rows from yaml, then POST fresh (recommended).");
    Console.WriteLine("  --sync-scenario <Name>   PATCH existing rows by natural keys (small edits only).");
    Console.WriteLine("  --sync                   PATCH every scenario with sync: true in seed YAML.");
    Console.WriteLine();
    Console.WriteLine("Modes:");
    Console.WriteLine("  --full");
    Console.WriteLine("      Legacy: seed index, else data.xlsx / employees.* / demo fallback.");
    Console.WriteLine();
    Console.WriteLine("Dev tools (no server):");
    Console.WriteLine("  --dump-lookups              Legacy: generate a markdown dump from lookup.xlsm.");
    Console.WriteLine("  --export-lookup-catalogs    Export lookup.xlsm → Module/LookupCatalogs/*.json");
    Console.WriteLine("  --export-seed               Split legacy data.yaml → seed/scenarios/ (one-time migration).");
    Console.WriteLine("  --validate-seed [path]      Report obsolete/hidden columns vs ApplicationType Show* flags.");
    Console.WriteLine("  --prune-seed [path]         Same as --validate-seed and rewrite scenario yaml on disk.");
    Console.WriteLine();
    Console.WriteLine("Safety checks:");
    Console.WriteLine("  --skip-visibility-preflight Skip seed visibility check against live ApplicationType rows.");
    Console.WriteLine();
    Console.WriteLine("Other options:");
    Console.WriteLine("  --verbose, -v       Enable verbose payload logging.");
    Console.WriteLine("  --help, -h          Show this help message.");
    Console.WriteLine();
    Console.WriteLine("Recommended flow:");
    Console.WriteLine("  1) Start Visa2026.Blazor.Server (lookup catalogs sync via ModuleUpdater on startup).");
    Console.WriteLine("  2) dotnet run --project Visa2026.DataImporter");
    Console.WriteLine();
    Console.WriteLine("Notes:");
    Console.WriteLine("  - Start Visa2026.Blazor.Server before import runs.");
}

Log.Init();
Log.Phase("Visa2026 Data Importer starting");
Log.Info($"Working directory: {Directory.GetCurrentDirectory()}");

if (HasArg(args, "--validate-seed") || HasArg(args, "--prune-seed"))
{
    bool persist = HasArg(args, "--prune-seed");
    string? seedSpec = GetOptionValue(args, "--prune-seed") ?? GetOptionValue(args, "--validate-seed");
    string seedPath = YamlSeedCatalog.ResolveExistingSeedPath(seedSpec)
                      ?? ResolveInputFile(YamlSeedCatalog.DefaultIndexRelativePath)
                      ?? YamlSeedCatalog.DefaultIndexRelativePath;

    Log.Phase(persist ? "Prune seed scenarios" : "Validate seed scenarios");
    int errors = SeedScenarioValidator.ValidateAll(seedPath, persistPruned: persist, quiet: false);
    Log.Close();
    Environment.ExitCode = errors > 0 ? 1 : 0;
    return;
}

if (HasArg(args, "--export-seed"))
{
    Log.Phase("Export seed layout — server not required");
    string? monolithic = null;
    if (FindDataImporterContentRoot() is string projectDir)
    {
        string inProject = Path.Combine(projectDir, YamlSeedCatalog.LegacyMonolithicFileName);
        if (File.Exists(inProject))
            monolithic = inProject;
    }

    monolithic ??= ResolveInputFile(YamlSeedCatalog.LegacyMonolithicFileName);

    if (monolithic == null)
    {
        Log.Error($"Legacy {YamlSeedCatalog.LegacyMonolithicFileName} not found — nothing to export.");
        Log.Close();
        return;
    }

    string seedRoot = FindDataImporterContentRoot() is string dir
        ? Path.Combine(dir, "seed")
        : Path.Combine(AppContext.BaseDirectory, "seed");

    Log.Info($"Source: {Path.GetFullPath(monolithic)}");
    Log.Info($"Output: {Path.GetFullPath(seedRoot)}");
    YamlSeedCatalog.ExportMonolithicToSeedLayout(monolithic, seedRoot);
    Log.Ok($"Exported scenarios to {Path.GetFullPath(seedRoot)}.");
    Log.Close();
    return;
}

if (HasArg(args, "--export-lookup-catalogs"))
{
    Log.Phase("Export lookup catalogs — server not required");
    var lookupPath = ResolveInputFile("lookup.xlsm");
    if (lookupPath == null)
    {
        Log.Error("lookup.xlsm not found.");
        Log.Close();
        return;
    }

    var solutionRoot = LookupDumper.FindSolutionRoot(AppContext.BaseDirectory);
    var outDir = solutionRoot != null
        ? Path.Combine(solutionRoot, "Visa2026.Module", "DatabaseUpdate", "LookupCatalogs")
        : Path.Combine("Visa2026.Module", "DatabaseUpdate", "LookupCatalogs");

    Log.Info($"Source: {Path.GetFullPath(lookupPath)}");
    Log.Info($"Output: {Path.GetFullPath(outDir)}");
    LookupCatalogExporter.Export(lookupPath, outDir);
    Log.Ok("Lookup catalog JSON export complete.");
    Log.Close();
    return;
}

Log.Info($"Target: {ApiBaseUrl}  User: {UserName}");

try
{
    // -----------------------------------------------------------------------
    // Set up client, wait for server, then authenticate
    // -----------------------------------------------------------------------
    var api = new ApiClient(ApiBaseUrl, UserName, Password);
    bool isVerbose = HasArg(args, "--verbose") || HasArg(args, "-v");
    api.Verbose = isVerbose;
    if(isVerbose) Log.Info("Verbose logging is enabled.");

    bool dumpLookupsMode = HasArg(args, "--dump-lookups");
    bool showHelp = HasArg(args, "--help") || HasArg(args, "-h");
    bool fullModeRequested = HasArg(args, "--full");
    bool legacyYamlFlag = HasArg(args, "--import-yaml-only");
    string yamlFileSpec = ResolveYamlFileSpec(args);
    var unknownFlags = GetUnknownFlags(args);

    var removedFlags = args
        .Where(a => a.StartsWith('-') &&
            (string.Equals(a, "--seed-lookups-only", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(a, "--sync-positions", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(a, "--delete-missing", StringComparison.OrdinalIgnoreCase)))
        .ToList();
    if (removedFlags.Count > 0)
    {
        Log.Error($"Removed option(s): {string.Join(", ", removedFlags)}.");
        Log.Error("Lookup catalogs sync on Visa2026.Blazor.Server startup (LookupCatalogSyncUpdater). See docs/LOOKUP_SEEDING.md.");
        return;
    }

    if (showHelp)
    {
        PrintHelp();
        return;
    }

    if (unknownFlags.Count > 0)
    {
        Log.Error($"Unknown argument(s): {string.Join(", ", unknownFlags)}");
        Log.Error("Use --help to see available options.");
        Console.WriteLine();
        PrintHelp();
        return;
    }

    var runOptions = BuildScenarioRunOptions(args);
    if (runOptions == null && (args.Any(a => string.Equals(a, "--sync-scenario", StringComparison.OrdinalIgnoreCase))
        || args.Any(a => string.Equals(a, "--clear-scenario", StringComparison.OrdinalIgnoreCase))))
        return;

    if (legacyYamlFlag && fullModeRequested)
    {
        Log.Error("Conflicting mode flags: --import-yaml-only cannot be combined with --full.");
        return;
    }

    var runMode = fullModeRequested ? "full" : "import-yaml";
    bool yamlImportMode = !fullModeRequested;
    Log.Info($"Run mode: {runMode}");
    if (yamlImportMode)
        Log.Info($"YAML file: {yamlFileSpec}");
    if (runOptions != null)
    {
        if (runOptions.ClearScenarioNames.Count > 0)
            Log.Info($"Clear + re-import: {string.Join(", ", runOptions.ClearScenarioNames)}");
        if (runOptions.SyncAllFlaggedInYaml)
            Log.Info("Sync: all scenarios marked sync: true in yaml.");
        if (runOptions.SyncScenarioNames.Count > 0)
            Log.Info($"Sync scenarios: {string.Join(", ", runOptions.SyncScenarioNames)}");
    }

    // -----------------------------------------------------------------------
    // --dump-lookups: read lookup.xlsm and write a markdown dump (no server needed)
    // -----------------------------------------------------------------------
    if (dumpLookupsMode)
    {
        Log.Phase("Dump Lookups mode — server connection not required");
        const string lookupFile = "lookup.xlsm";

        // lookup.xlsm is copied to the bin output dir by the build.
        // When running via 'dotnet run', working dir is the solution root, so check bin dir first.
        var lookupPath = File.Exists(Path.Combine(AppContext.BaseDirectory, lookupFile))
            ? Path.Combine(AppContext.BaseDirectory, lookupFile)
            : File.Exists(lookupFile) ? lookupFile : null;

        if (lookupPath == null)
        {
            Log.Error($"'{lookupFile}' not found in output directory or working directory.");
            Log.Error($"Output dir checked: {AppContext.BaseDirectory}");
        }
        else
        {
            Log.Info($"Using lookup file: {Path.GetFullPath(lookupPath)}");
            var solutionRoot = LookupDumper.FindSolutionRoot(AppContext.BaseDirectory);
            var outputFile = solutionRoot != null
                ? Path.Combine(solutionRoot, "lookup-dump.md")
                : "lookup-dump.md";

            if (solutionRoot != null) Log.Info($"Solution root found: {solutionRoot}");
            else Log.Warn("Solution root not found — writing lookup-dump.md to working directory.");

            await LookupDumper.DumpAsync(lookupPath, outputFile);
            Log.Ok($"Done. '{outputFile}' is ready for reference.");
        }
        Log.Close();
        return;
    }

    try
    {
        Log.Step("Waiting for server to become ready (max 300s, poll every 2s)...");
        await api.WaitForServerAsync(maxWaitSeconds: 300, pollIntervalSeconds: 2);
        Log.Ok("Server is ready.");

        Log.Step($"Authenticating as '{UserName}'...");
        await api.LoginAsync();
        Log.Ok("Authentication successful.");
    }
    catch (TimeoutException ex)
    {
        Log.Error($"Server did not become ready in time: {ex.Message}");
        Log.Error("Make sure Visa2026.Blazor.Server is running and try again.");
        return;
    }
    catch (HttpRequestException ex)
    {
        Log.Error($"Could not connect to {ApiBaseUrl}");
        Log.Error($"Details: {ex.Message}");
        Log.Error("Make sure Visa2026.Blazor.Server is running and try again.");
        return;
    }

    // -----------------------------------------------------------------------
    // Run import orchestration
    // -----------------------------------------------------------------------
    var sw = System.Diagnostics.Stopwatch.StartNew();

    try
    {
        Log.Phase($"Starting '{runMode}' import orchestration");

        BaseImporter<object>.ClearLookupCache();
        Log.Info("Lookup cache cleared.");

        // ===================================================================
        #region Phase 1 — Initialize Importers
        // ===================================================================
        Log.Phase("Phase 1: Initializing importers");
        var organizationImporter     = new OrganizationSingletonImporter(api);
        var projectContractImporter  = new ProjectContractImporter(api);
        var personImporter           = new PersonImporter(api);
        var passportImporter         = new PassportImporter(api);
        var educationImporter        = new EducationImporter(api);
        var historyImporter          = new EmployeePositionHistoryImporter(api);
        var medicalRecordImporter    = new MedicalRecordImporter(api);
        var applicationImporter      = new ApplicationImporter(api);
        var appItemImporter          = new ApplicationItemImporter(api);
        var appProgressImporter      = new ApplicationProgressImporter(api);
        var invitationImporter       = new InvitationImporter(api);
        var invitationItemImporter   = new InvitationItemImporter(api);
        var workPermitImporter       = new WorkPermitImporter(api);
        var workPermitItemImporter   = new WorkPermitItemImporter(api);
        var visaImporter             = new VisaImporter(api);
        var rejectionImporter        = new RejectionImporter(api);
        var rejectionItemImporter    = new RejectionItemImporter(api);
        var travelHistoryImporter    = new TravelHistoryImporter(api);
        var lodgingImporter          = new LodgingImporter(api);
        var addressImporter          = new AddressOfResidenceImporter(api);
        var cityImporter             = new CityImporter(api);
        Log.Ok("Phase 1 complete — all importers ready.");
        #endregion

        // ===================================================================
        #region Phase 2 — Fetch Prerequisite Lookup Data
        // ===================================================================
        Log.Phase("Phase 2: Fetching prerequisite lookup data");

        static async Task<TLookup?> SafeQuery<TLookup>(ApiClient client, string entity) where TLookup : class
        {
            try
            {
                var results = await client.QueryAsync<TLookup>(entity, "$top=1");
                var item = results.FirstOrDefault();
                if (item == null)
                    Log.Warn($"  No records in '{entity}' — seed this table first.");
                else
                    Log.Ok($"  {entity}");
                return item;
            }
            catch (Exception ex)
            {
                Log.Error($"  FAILED querying '{entity}': {ex.Message}");
                return null;
            }
        }

        static async Task<TLookup?> SafeLookup<TLookup>(ApiClient client, string entity, string value, bool byCode = true) where TLookup : class
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            string field = byCode ? "Code" : "Name";
            try
            {
                var results = await client.QueryAsync<TLookup>(entity, $"$filter={field} eq '{value}'&$top=1");
                return results.FirstOrDefault();
            }
            catch (Exception ex)
            {
                Log.Error($"  FAILED lookup {entity} where {field}='{value}': {ex.Message}");
                return null;
            }
        }

        var country        = await SafeQuery<Country>(api, "Country");
        var gender         = await SafeQuery<Gender>(api, "Gender");
        var position       = await SafeQuery<Position>(api, "Position");
        var department     = await SafeQuery<Department>(api, "Department");
        var duration       = await SafeQuery<ValidityDuration>(api, "ValidityDuration");
        var maritalStatus  = await SafeQuery<MaritalStatus>(api, "MaritalStatus");
        var passportType   = await SafeQuery<PassportType>(api, "PassportType");
        var eduLevel       = await SafeQuery<EducationLevel>(api, "EducationLevel");
        var eduInstitution = await SafeQuery<EducationInstitution>(api, "EducationInstitution");
        var specialty      = await SafeQuery<Specialty>(api, "Specialty");
        var appType        = await SafeQuery<ApplicationType>(api, "ApplicationType");
        var appState       = await SafeQuery<ApplicationState>(api, "ApplicationState");
        var appLocation    = await SafeQuery<ApplicationLocation>(api, "ApplicationLocation");
        var visaType       = await SafeQuery<VisaType>(api, "VisaType");
        var visaCategory   = await SafeQuery<VisaCategory>(api, "VisaCategory");
        var visaIssuedPlace = await SafeQuery<VisaIssuedPlace>(api, "VisaIssuedPlace");
        var region         = await SafeQuery<Region>(api, "Region");

        // appType and eduLevel are not in the seed file — warn but don't abort
        if (appType == null)
            Log.Warn("ApplicationType has no data — Phases 5+ may fail. Seed ApplicationType manually via the Blazor UI.");
        if (eduLevel == null)
            Log.Warn("EducationLevel has no data — education records will be skipped.");

        bool lookupsFailed =
            country == null || position == null || department == null ||
            duration == null || region == null || appType == null;

        if (lookupsFailed)
        {
            Log.Error("One or more CRITICAL lookups are empty — cannot import data.yaml.");
            Log.Error("country="    + (country    == null ? "NULL" : "OK"));
            Log.Error("position="   + (position   == null ? "NULL" : "OK"));
            Log.Error("department=" + (department == null ? "NULL" : "OK"));
            Log.Error("duration="   + (duration   == null ? "NULL" : "OK"));
            Log.Error("region="     + (region     == null ? "NULL" : "OK"));
            Log.Error("appType="    + (appType    == null ? "NULL" : "OK"));
            Log.Error("");
            Log.Error("Lookup rows are synced by Visa2026.Blazor.Server (LookupCatalogSyncUpdater), not by this tool.");
            if (department == null)
                Log.Error("Department is empty — tenant/department.json may have no rows, or catalog sync did not run after the file was updated.");
            Log.Error("If ApplicationType exists but Country/Region/etc. do not, Module updaters did not run on this database.");
            Log.Error("Fix: restart the Blazor app once with updaters forced, then run the importer again:");
            Log.Error("  - Visual Studio: stop app, set user env FORCE_XAF_DB_UPDATE=true, F5, wait for startup, unset flag.");
            Log.Error("  - Or: .\\scripts\\local\\Set-ForceXafDbUpdate.ps1 -Enable -EnvFile .env.dev  (Docker dev stack)");
            Log.Error("In app console/logs, confirm lines like: LookupCatalogSyncUpdater: country ... created=");
            Log.Error("See docs/LOOKUP_SEEDING.md and docs/ENVIRONMENTS.md (FORCE_XAF_DB_UPDATE).");
            return;
        }
        Log.Ok("Phase 2 complete — all required lookup data fetched.");
        #endregion

        // ===================================================================
        #region Phase 3 — Organization singleton and project contract
        // ===================================================================
        Log.Phase("Phase 3: Loading organization profile and project contract");

        Log.Step("Loading company profile (singleton)...");
        var companyProfile = (await api.QueryAsync<CompanyProfileDto>("CompanyProfile", "$top=1")).FirstOrDefault();
        if (companyProfile == null)
        {
            Log.Error("No CompanyProfile found. Start the Blazor app once so OrganizationSingletonSeedUpdater can seed singletons.");
            return;
        }
        Log.Ok($"CompanyProfile loaded: {companyProfile.Name} ({companyProfile.Id})");

        // Load the default project contract
        Log.Step("Loading project contract from database...");
        var contracts = await api.QueryAsync<ProjectContract>("ProjectContract", "$filter=IsDefault eq true&$top=1");
        var projectContract = contracts.FirstOrDefault();
        if (projectContract == null)
        {
            var allContracts = await api.GetAllAsync<ProjectContract>("ProjectContract");
            projectContract = allContracts.FirstOrDefault();
        }
        if (projectContract == null) { Log.Error("No ProjectContract found. Start the app once (tenant lookup catalogs) or import data.yaml Shared scenario."); return; }
        Log.Ok($"ProjectContract loaded: {projectContract.NameTm} ({projectContract.Id})");

        _ = organizationImporter;

        Log.Ok("Phase 3 complete.");
        #endregion

        // ===================================================================
        #region Phase 4 — Onboard Employee
        // ===================================================================
        Log.Phase("Phase 4: Onboarding Employee");
        Person? person = null;
        var excelImporter = new ExcelImporter(api, runOptions);

        string? dataYaml = null;
        string? dataXlsx = null;

        if (yamlImportMode)
        {
            dataYaml = ResolveInputFile(yamlFileSpec);
            if (dataYaml == null)
            {
                Log.Error($"YAML file not found: '{yamlFileSpec}'.");
                Log.Error("Place seed/scenarios.index.yaml in the output directory, pass a path, or set DATA_YAML_PATH.");
                return;
            }
            if (!HasArg(args, "--skip-visibility-preflight"))
            {
                Log.Step("Preflight: validating seed visibility vs server ApplicationType...");
                var seedVisibility = ApplicationTypeVisibilityCatalog.Load();
                await ApplicationTypeVisibilityPreflight.EnsureSeedVisibilityMatchesServerAsync(api, seedVisibility);
                Log.Ok("Seed visibility matches server.");
            }
            Log.Info($"Importing scenarios from '{Path.GetFullPath(dataYaml)}'...");
            await excelImporter.ImportByScenariosFromYamlAsync(dataYaml);
        }
        else
        {
            dataYaml = YamlSeedCatalog.ResolveExistingSeedPath(null, AppContext.BaseDirectory)
                         ?? ResolveInputFile(YamlSeedCatalog.LegacyMonolithicFileName);
            dataXlsx = ResolveInputFile("data.xlsx");

            if (dataYaml != null)
            {
                if (!HasArg(args, "--skip-visibility-preflight"))
                {
                    Log.Step("Preflight: validating seed visibility vs server ApplicationType...");
                    var seedVisibility = ApplicationTypeVisibilityCatalog.Load();
                    await ApplicationTypeVisibilityPreflight.EnsureSeedVisibilityMatchesServerAsync(api, seedVisibility);
                    Log.Ok("Seed visibility matches server.");
                }
                Log.Info($"Found seed data at '{dataYaml}' — importing scenarios from YAML...");
                await excelImporter.ImportByScenariosFromYamlAsync(dataYaml);
                if (dataXlsx != null)
                {
                    var importedPersons = await api.GetAllAsync<Person>("Person");
                    person = importedPersons.LastOrDefault();
                    if (person != null) Log.Info($"Selected Person from YAML: {person.FullName} ({person.Id})");
                }
            }
            else if (dataXlsx != null)
            {
                Log.Info("Found data.xlsx — importing by scenarios (falls back to full import if no Scenarios sheet)...");
                await excelImporter.ImportByScenariosAsync(dataXlsx);
                var importedPersons = await api.GetAllAsync<Person>("Person");
                person = importedPersons.LastOrDefault();
                if (person != null) Log.Info($"Selected Person from Excel: {person.FullName} ({person.Id})");
            }
            else if (File.Exists("employees.xlsx"))
            {
                Log.Info("Found employees.xlsx — importing Persons sheet only...");
                await excelImporter.ImportSheetAsync("employees.xlsx", "Persons");
                var importedPersons = await api.GetAllAsync<Person>("Person");
                person = importedPersons.LastOrDefault();
                Log.Info($"Person after Excel import: {(person == null ? "NULL — will use demo fallback" : person.FullName)}");
            }
            else if (File.Exists("employees.csv"))
            {
                Log.Info("Found employees.csv — bulk importing persons...");
                var csvPersons = CsvParser.Parse("employees.csv", row =>
                {
                    string Val(int index) => (index < row.Count) ? row[index].Trim() : "";
                    return new Person
                    {
                        FirstName = Val(0), LastName = Val(1), Email = Val(2),
                        DateOfBirth = DateTime.TryParse(Val(3), out var dob) ? dob : DateTime.MinValue,
                        BirthPlace = Val(4), ForeignAddress = Val(5), IsEmployee = true,
                        ProjectContract = projectContract
                    };
                }, hasHeader: true).ToList();

                Log.Info($"Parsed {csvPersons.Count} rows from CSV.");
                if (csvPersons.Any())
                {
                    await personImporter.BulkImportAsync(csvPersons);
                    var lastEmail = csvPersons.Last().Email;
                    person = (await api.QueryAsync<Person>("Person", $"$filter=Email eq '{lastEmail}'")).FirstOrDefault();
                    Log.Info($"Re-queried person by email '{lastEmail}': {(person == null ? "NOT FOUND" : person.FullName)}");
                }
            }
            else
            {
                Log.Warn("No import file found (seed/scenarios.index.yaml / data.xlsx / employees.xlsx / employees.csv).");
            }
        }

        Log.Ok("Phase 4 complete.");
        #endregion

        // ===================================================================
        // Phases 4 (programmatic), 5, and 7 only run when data.xlsx is NOT
        // present. When data.xlsx is used, all records are seeded by the
        // scenario-based Excel import above.
        // ===================================================================
        if (dataYaml == null && dataXlsx == null)
        {
        // ===================================================================
        #region Phase 4 (programmatic) — Demo / CSV / employees.xlsx fallback
        // ===================================================================
        if (person == null)
        {
            Log.Info("Creating demo person as fallback...");
            person = await personImporter.CreateOneAsync(new Person
            {
                FirstName = "John", LastName = "Smith", DateOfBirth = new DateTime(1985, 1, 1),
                BirthPlace = "London", Gender = gender, Nationality = country, CountryOfBirth = country,
                MaritalStatus = maritalStatus, ForeignAddress = "456 Oak Avenue", ForeignAddressCountry = country,
                ProjectContract = projectContract, IsEmployee = true,
                Email = $"j.smith.{Guid.NewGuid().ToString()[..4]}@example.com"
            });
        }
        if (person == null) { Log.Error("Person creation/import failed — aborting."); return; }
        Log.Ok($"Targeting Person for Application: {person.FullName}");

        Log.Step("Creating passport...");
        var passport = await passportImporter.CreateOneAsync("P123456", "S98765", "UKPA", DateTime.Today.AddYears(-5), DateTime.Today.AddYears(5), person.Id, passportType!.Id, country.Id);
        if (passport == null) { Log.Error("Passport creation failed — aborting."); return; }
        Log.Ok($"Passport: {passport.Id}");

        Log.Step("Creating education...");
        var education = await educationImporter.CreateOneAsync(person.Id, eduLevel!.Id, eduInstitution!.Id, country.Id, specialty!.Id, "2007");
        if (education == null) { Log.Error("Education creation failed — aborting."); return; }
        Log.Ok($"Education: {education.Id}");

        Log.Step("Creating position history...");
        var history = await historyImporter.CreateOneAsync(person.Id, position.Id, department.Id, DateTime.Today.AddMonths(-1));
        if (history == null) { Log.Error("PositionHistory creation failed — aborting."); return; }
        Log.Ok($"PositionHistory: {history.Id}");

        Log.Step("Creating medical record...");
        var medicalRecord = await medicalRecordImporter.CreateOneAsync(person.Id, "MED998877", DateTime.Today, duration.Id);
        if (medicalRecord == null) { Log.Error("MedicalRecord creation failed — aborting."); return; }
        Log.Ok($"MedicalRecord: {medicalRecord.Id}");

        Log.Ok("Phase 4 (programmatic) complete.");
        #endregion

        // ===================================================================
        #region Phase 5 — Application
        // ===================================================================
        Log.Phase("Phase 5: Creating Application");

        Log.Step("Creating application...");
        var application = await applicationImporter.CreateOneAsync(DateTime.Today, ApplicationTypeCategory.Employee, appType.Id);
        if (application == null) { Log.Error("Application creation failed — aborting."); return; }
        Log.Ok($"Application: {application.Id}");

        Log.Step("Creating application item...");
        var appItem = await appItemImporter.CreateOneAsync(
            application.Id,
            person.Id,
            passport.Id,
            currentPositionHistoryId: history.Id);
        if (appItem == null) { Log.Error("ApplicationItem creation failed — aborting."); return; }
        Log.Ok($"ApplicationItem: {appItem.Id}");

        Log.Step("Logging initial application progress...");
        await appProgressImporter.CreateOneAsync(application.Id, appState!.Id, appLocation!.Id, DateTime.Now, "Application submitted.");
        Log.Ok("Phase 5 complete.");
        #endregion

        // ===================================================================
        #region Phase 7 — Miscellaneous Records
        // ===================================================================
        Log.Phase("Phase 7: Creating Miscellaneous Records");

        Log.Step("Creating lodging...");
        var lodging = await lodgingImporter.CreateOneAsync("Company Guesthouse", "100 Main Street");
        if (lodging != null)
        {
            Log.Ok($"Lodging: {lodging.Id}");
            Log.Step("Creating address of residence...");
            var cities = await api.QueryAsync<City>("City", "$filter=Name eq 'Ashgabat'&$top=1");
            var city = cities.FirstOrDefault();
            if (city == null)
            {
                Log.Warn("City 'Ashgabat' not found in lookup catalogs — address skipped.");
            }
            else
            {
                await addressImporter.CreateOneAsync(person.Id, ResidenceType.Lodging, lodging.FullAddress, DateTime.Today.AddYears(1), lodging.Id);
            }
            Log.Ok("AddressOfResidence created.");
        }
        else Log.Warn("Lodging creation failed — address skipped.");

        Log.Step("Creating travel history...");
        // Note: TravelHistory is abstract. We must create a specific concrete type.
        var arrivalPayload = new Dictionary<string, object?> {
            ["Person"] = new { ID = person.Id },
            ["TravelDate"] = DateTime.Today.AddDays(-10),
            ["CheckPoint"] = new { ID = (await api.QueryAsync<CheckPoint>("CheckPoint", "$top=1")).First().Id },
            ["@odata.type"] = "#Visa2026.Module.BusinessObjects.ExternalArrival"
        };
        await api.CreateAsync<object>("TravelHistory", arrivalPayload);
        Log.Ok("TravelHistory created.");

        // BusinessTrip creation removed — fields moved to Application (BusinessTripStartDate, BusinessTripEndDate, BusinessTripPurpose).

        Log.Ok("Phase 7 complete.");
        #endregion

        } // end: dataYaml == null && dataXlsx == null

        sw.Stop();
        Log.Phase($"Import complete. Total time: {sw.Elapsed:mm\\:ss\\.fff}");
    }
    catch (Exception ex)
    {
        sw.Stop();
        Log.Error($"UNHANDLED EXCEPTION after {sw.Elapsed:mm\\:ss\\.fff}");
        Log.Error($"Type:    {ex.GetType().Name}");
        Log.Error($"Message: {ex.Message}");
        if (ex.InnerException != null)
            Log.Error($"Inner:   {ex.InnerException.Message}");
        Log.Error($"Stack trace:");
        foreach (var line in (ex.StackTrace ?? "").Split('\n'))
            Log.Error($"  {line.Trim()}");
    }
}
finally
{
    Log.Close();
    if (!Console.IsInputRedirected)
    {
        Console.WriteLine("\nPress any key to exit.");
        Console.ReadKey();
    }
}

// -----------------------------------------------------------------------
// Logging helper
// -----------------------------------------------------------------------
static class Log
{
    private static StreamWriter? _file;
    private static readonly object _lock = new();

    public static void Init()
    {
        var path = Path.Combine(
            AppContext.BaseDirectory,
            $"import_{DateTime.Now:yyyyMMdd_HHmmss}.log");
        _file = new StreamWriter(path, append: false, System.Text.Encoding.UTF8) { AutoFlush = true };
        Info($"Log file: {path}");
    }

    public static void Info (string msg) => Write("INF", msg, ConsoleColor.Gray);
    public static void Ok   (string msg) => Write(" OK", msg, ConsoleColor.Green);
    public static void Warn (string msg) => Write("WRN", msg, ConsoleColor.Yellow);
    public static void Error(string msg) => Write("ERR", msg, ConsoleColor.Red);
    public static void Phase(string msg) => Write("===", msg, ConsoleColor.Cyan);
    public static void Step (string msg) => Write("---", msg, ConsoleColor.White);

    private static void Write(string level, string msg, ConsoleColor color)
    {
        var line = $"{DateTime.Now:HH:mm:ss.fff} [{level}] {msg}";
        lock (_lock)
        {
            var prev = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(line);
            Console.ForegroundColor = prev;
            _file?.WriteLine(line);
        }
    }

    public static void Close() => _file?.Close();
}