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
    if (Path.IsPathRooted(fileNameOrPath))
        return File.Exists(fileNameOrPath) ? fileNameOrPath : null;

    var outputDirPath = Path.Combine(AppContext.BaseDirectory, fileNameOrPath);
    if (File.Exists(outputDirPath))
        return outputDirPath;

    return File.Exists(fileNameOrPath) ? fileNameOrPath : null;
}

static IReadOnlyList<string> GetUnknownFlags(IReadOnlyList<string> args)
{
    var knownFlags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "--seed-lookups-only",
        "--sync-positions",
        "--delete-missing",
        "--import-yaml-only",
        "--full",
        "--dump-lookups",
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
    Console.WriteLine("  dotnet run --project Visa2026.DataImporter -- [mode] [options]");
    Console.WriteLine("  dotnet Visa2026.DataImporter.dll [mode] [options]");
    Console.WriteLine();
    Console.WriteLine("Modes (choose one):");
    Console.WriteLine("  --seed-lookups-only");
    Console.WriteLine("      Seed lookup/reference tables from lookup.xlsm, then exit.");
    Console.WriteLine("  --sync-positions");
    Console.WriteLine("      Replace Position lookup values safely (upsert from lookup.xlsm).");
    Console.WriteLine("      Optional: add --delete-missing to delete Positions not present in Excel (best-effort).");
    Console.WriteLine("  --import-yaml-only [path]");
    Console.WriteLine("      Import YAML scenarios only (default path: data.yaml).");
    Console.WriteLine("      Lookup seeding is skipped by design.");
    Console.WriteLine("  --full");
    Console.WriteLine("      Run full orchestration (default if no mode is provided).");
    Console.WriteLine();
    Console.WriteLine("Other options:");
    Console.WriteLine("  --dump-lookups      Generate LOOKUPS.md from lookup.xlsm (no server required).");
    Console.WriteLine("  --delete-missing    With --sync-positions: delete destination Positions not present in lookup.xlsm.");
    Console.WriteLine("  --verbose, -v       Enable verbose payload logging.");
    Console.WriteLine("  --help, -h          Show this help message.");
    Console.WriteLine();
    Console.WriteLine("Recommended production-safe flow:");
    Console.WriteLine("  1) dotnet run --project Visa2026.DataImporter -- --seed-lookups-only");
    Console.WriteLine("  2) dotnet run --project Visa2026.DataImporter -- --sync-positions");
    Console.WriteLine("  2) dotnet run --project Visa2026.DataImporter -- --import-yaml-only");
    Console.WriteLine();
    Console.WriteLine("Notes:");
    Console.WriteLine("  - Only one mode flag can be used per run.");
    Console.WriteLine("  - Start Visa2026.Blazor.Server before import runs.");
}

Log.Init();
Log.Phase("Visa2026 Data Importer starting");
Log.Info($"Target: {ApiBaseUrl}  User: {UserName}");
Log.Info($"Working directory: {Directory.GetCurrentDirectory()}");

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
    bool seedLookupsOnlyMode = HasArg(args, "--seed-lookups-only");
    bool syncPositionsMode = HasArg(args, "--sync-positions");
    bool deleteMissingPositions = HasArg(args, "--delete-missing");
    bool importYamlOnlyMode = HasArg(args, "--import-yaml-only");
    bool fullModeRequested = HasArg(args, "--full");
    string? importYamlPathArg = GetOptionValue(args, "--import-yaml-only");
    var unknownFlags = GetUnknownFlags(args);

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

    int selectedModes =
        (seedLookupsOnlyMode ? 1 : 0) +
        (syncPositionsMode ? 1 : 0) +
        (importYamlOnlyMode ? 1 : 0) +
        (fullModeRequested ? 1 : 0);

    if (selectedModes > 1)
    {
        Log.Error("Conflicting mode flags. Use only one of:");
        Log.Error("  --seed-lookups-only");
        Log.Error("  --sync-positions");
        Log.Error("  --import-yaml-only [path]");
        Log.Error("  --full");
        return;
    }

    var runMode = seedLookupsOnlyMode
        ? "seed-lookups-only"
        : syncPositionsMode
            ? "sync-positions"
        : importYamlOnlyMode
            ? "import-yaml-only"
            : "full";

    bool shouldSeedLookups = runMode is "seed-lookups-only" or "full";
    bool yamlOnlyMode = runMode == "import-yaml-only";
    string yamlOnlyFileNameOrPath = string.IsNullOrWhiteSpace(importYamlPathArg) ? "data.yaml" : importYamlPathArg;
    Log.Info($"Run mode: {runMode}");

    // -----------------------------------------------------------------------
    // --dump-lookups: read lookup.xlsm and write LOOKUPS.md (no server needed)
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
                ? Path.Combine(solutionRoot, "LOOKUPS.md")
                : "LOOKUPS.md";

            if (solutionRoot != null) Log.Info($"Solution root found: {solutionRoot}");
            else Log.Warn("Solution root not found — writing LOOKUPS.md to working directory.");

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

        // -----------------------------------------------------------------------
        // Mode: --sync-positions (production-safe Position replacement)
        // -----------------------------------------------------------------------
        if (syncPositionsMode)
        {
            Log.Phase("Sync Positions mode");
            var lookupSeeder = new LookupSeeder(api);
            var lookupXlsm = ResolveInputFile("lookup.xlsm");
            if (lookupXlsm != null)
            {
                await lookupSeeder.SyncPositionsAsync(lookupXlsm, deleteMissing: deleteMissingPositions);
                Log.Ok("Positions synced.");
            }
            else
            {
                Log.Error("lookup.xlsm not found — cannot sync positions.");
            }
            Log.Close();
            return;
        }
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
        #region Phase 0 — Seed Lookup Tables
        // ===================================================================
        if (shouldSeedLookups)
        {
            Log.Phase("Phase 0: Seeding lookup/reference tables");
            var lookupSeeder = new LookupSeeder(api);
            var lookupXlsm = ResolveInputFile("lookup.xlsm");
            if (lookupXlsm != null)
            {
                Log.Info($"Found lookup.xlsm — seeding all reference tables...");
                await lookupSeeder.SeedAllAsync(lookupXlsm);

                // Auto-sync LOOKUPS.md at the solution root after every successful seed.
                try
                {
                    var solutionRoot = LookupDumper.FindSolutionRoot(AppContext.BaseDirectory);
                    var lookupsDoc = solutionRoot != null ? Path.Combine(solutionRoot, "LOOKUPS.md") : "LOOKUPS.md";
                    await LookupDumper.DumpAsync(lookupXlsm, lookupsDoc);
                    Log.Ok($"LOOKUPS.md refreshed: {lookupsDoc}");
                }
                catch (Exception ex)
                {
                    Log.Warn($"LOOKUPS.md could not be refreshed: {ex.Message}");
                }

                Log.Ok("Phase 0 complete.");
            }
            else
            {
                Log.Warn("lookup.xlsm not found — skipping lookup seeding.");
                Log.Warn("Run will proceed but may fail at Phase 2 if tables are empty.");
            }
        }
        else
        {
            Log.Phase("Phase 0: Skipped (mode does not seed lookups)");
        }

        if (runMode == "seed-lookups-only")
        {
            sw.Stop();
            Log.Phase($"Lookup seeding complete. Total time: {sw.Elapsed:mm\\:ss\\.fff}");
            return;
        }
        #endregion

        // ===================================================================
        #region Phase 1 — Initialize Importers
        // ===================================================================
        Log.Phase("Phase 1: Initializing importers");
        var companyImporter          = new CompanyImporter(api);
        var localEmployeeImporter    = new LocalEmployeeImporter(api);
        var companyHeadImporter      = new CompanyHeadImporter(api);
        var representativeImporter   = new RepresentativeImporter(api);
        var projectContractImporter  = new ProjectContractImporter(api);
        var personImporter           = new PersonImporter(api);
        var passportImporter         = new PassportImporter(api);
        var educationImporter        = new EducationImporter(api);
        var historyImporter          = new EmployeePositionHistoryImporter(api);
        var contractImporter         = new EmployeeContractImporter(api);
        var medicalRecordImporter    = new MedicalRecordImporter(api);
        var applicationImporter      = new ApplicationImporter(api);
        var applicationTypeFilterImporter = new ApplicationTypeFilterImporter(api);
        var appItemImporter          = new ApplicationItemImporter(api);
        var appProgressImporter      = new ApplicationProgressImporter(api);
        var invitationImporter       = new InvitationImporter(api);
        var invitationItemImporter   = new InvitationItemImporter(api);
        var workPermitImporter       = new WorkPermitImporter(api);
        var workPermitItemImporter   = new WorkPermitItemImporter(api);
        var visaImporter             = new VisaImporter(api);
        var registrationImporter     = new RegistrationImporter(api);
        var rejectionImporter        = new RejectionImporter(api);
        var rejectionItemImporter    = new RejectionItemImporter(api);
        var travelHistoryImporter    = new TravelHistoryImporter(api);
        var lodgingImporter          = new LodgingImporter(api);
        var addressImporter          = new AddressOfResidenceImporter(api);
        var cityImporter             = new CityImporter(api);
        var businessTripImporter     = new BusinessTripImporter(api);
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
        var appTypeFilter  = await SafeQuery<ApplicationTypeFilter>(api, "ApplicationTypeFilter");
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
        if (appTypeFilter == null)
            Log.Warn("ApplicationTypeFilter has no data — Phases 5+ may fail. Seed ApplicationTypeFilter via lookup.xlsm.");

        bool lookupsFailed =
            country == null || position == null || department == null ||
            duration == null || region == null || appType == null || appTypeFilter == null;

        if (lookupsFailed)
        {
            Log.Error("One or more CRITICAL lookups are null — cannot continue.");
            Log.Error("country="       + (country       == null ? "NULL" : "OK"));
            Log.Error("position="      + (position      == null ? "NULL" : "OK"));
            Log.Error("department="    + (department    == null ? "NULL" : "OK"));
            Log.Error("duration="      + (duration      == null ? "NULL" : "OK"));
            Log.Error("region="        + (region        == null ? "NULL" : "OK"));
            Log.Error("appType="       + (appType       == null ? "NULL" : "OK"));
            Log.Error("appTypeFilter=" + (appTypeFilter == null ? "NULL" : "OK"));
            Log.Error("Seed the database (lookup.xlsm) and restart.");
            return;
        }
        Log.Ok("Phase 2 complete — all required lookup data fetched.");
        #endregion

        // ===================================================================
        #region Phase 3 — Company and Staffing Structure
        // ===================================================================
        Log.Phase("Phase 3: Loading Company and Staffing Structure");

        // Load the default company seeded from lookup.xlsm
        Log.Step("Loading company from database...");
        var companies = await api.QueryAsync<Company>("Company", "$filter=IsDefault eq true&$top=1");
        var company = companies.FirstOrDefault();
        if (company == null)
        {
            // Fallback: take the first company if none marked as default
            var allCompanies = await api.GetAllAsync<Company>("Company");
            company = allCompanies.FirstOrDefault();
        }
        if (company == null) { Log.Error("No Company found in database. Seed Company via lookup.xlsm first."); return; }
        Log.Ok($"Company loaded: {company.Name} ({company.Id})");

        // Load the default project contract for this company
        Log.Step("Loading project contract from database...");
        var contracts = await api.QueryAsync<ProjectContract>("ProjectContract", "$filter=IsDefault eq true&$top=1");
        var projectContract = contracts.FirstOrDefault();
        if (projectContract == null)
        {
            var allContracts = await api.GetAllAsync<ProjectContract>("ProjectContract");
            projectContract = allContracts.FirstOrDefault();
        }
        if (projectContract == null) { Log.Error("No ProjectContract found in database. Seed ProjectContracts via lookup.xlsm first."); return; }
        Log.Ok($"ProjectContract loaded: {projectContract.Name} ({projectContract.Id})");

        CompanyHead? companyHead = null;
        Representative? representative = null;

        // CompanyHead and Representative will now be imported from data.xlsx in Phase 4.
        // They are declared here to be in scope for later phases.
        // Their actual instances will be retrieved after the Excel import.
        // The LocalEmployee creation is also removed as it's a dependency for the programmatic creation.

        Log.Ok("Phase 3 complete.");
        #endregion

        // ===================================================================
        #region Phase 4 — Onboard Employee
        // ===================================================================
        Log.Phase("Phase 4: Onboarding Employee");
        Person? person = null;
        var excelImporter = new ExcelImporter(api);

        string? dataYaml = null;
        string? dataXlsx = null;

        if (yamlOnlyMode)
        {
            dataYaml = ResolveInputFile(yamlOnlyFileNameOrPath);
            if (dataYaml == null)
            {
                Log.Error($"YAML import mode requested but file not found: '{yamlOnlyFileNameOrPath}'.");
                Log.Error("Provide a valid file path or place data.yaml in the output/working directory.");
                return;
            }
            Log.Info($"YAML-only mode: using '{Path.GetFullPath(dataYaml)}'.");
        }
        else
        {
            dataYaml = ResolveInputFile("data.yaml");
            dataXlsx = ResolveInputFile("data.xlsx");
        }

        if (dataYaml != null)
        {
            Log.Info($"Found data.yaml — importing by scenarios from YAML...");
            await excelImporter.ImportByScenariosFromYamlAsync(dataYaml);
            // Person/CompanyHead/Representative lookups are only needed when
            // data.xlsx is also present for the legacy post-YAML Excel steps.
            if (dataXlsx != null)
            {
                var importedPersons = await api.GetAllAsync<Person>("Person");
                person = importedPersons.LastOrDefault();
                if (person != null) Log.Info($"Selected Person from YAML: {person.FullName} ({person.Id})");

                Log.Step("Retrieving CompanyHead and Representative from database...");
                var companyHeadsYaml = await api.QueryAsync<CompanyHead>("CompanyHead",
                    $"$filter=Company/ID eq {company.Id} and IsActive eq true&$top=1");
                companyHead = companyHeadsYaml.FirstOrDefault();
                if (companyHead != null) Log.Ok($"CompanyHead retrieved: {companyHead.FullName} ({companyHead.Id})");
                else Log.Warn("No active CompanyHead found for the company after YAML import. Application creation may fail.");

                var representativesYaml = await api.QueryAsync<Representative>("Representative",
                    $"$filter=Company/ID eq {company.Id} and IsActive eq true&$top=1");
                representative = representativesYaml.FirstOrDefault();
                if (representative != null) Log.Ok($"Representative retrieved: {representative.FullName} ({representative.Id})");
                else Log.Warn("No active Representative found for the company after YAML import. Application creation may fail.");
            }
        }
        else if (dataXlsx != null)
        {
            Log.Info($"Found data.xlsx — importing by scenarios (falls back to full import if no Scenarios sheet)...");
            await excelImporter.ImportByScenariosAsync(dataXlsx);
            var importedPersons = await api.GetAllAsync<Person>("Person");
            person = importedPersons.LastOrDefault();
            if (person != null) Log.Info($"Selected Person from Excel: {person.FullName} ({person.Id})");

            // Retrieve CompanyHead and Representative after data.xlsx import
            // Assuming there's at least one active CompanyHead and Representative linked to the company.
            Log.Step("Retrieving CompanyHead and Representative from database...");
            var companyHeads = await api.QueryAsync<CompanyHead>("CompanyHead",
                $"$filter=Company/ID eq {company.Id} and IsActive eq true&$top=1");
            companyHead = companyHeads.FirstOrDefault();
            if (companyHead != null) Log.Ok($"CompanyHead retrieved: {companyHead.FullName} ({companyHead.Id})");
            else Log.Warn("No active CompanyHead found for the company after data.xlsx import. Application creation may fail.");

            var representatives = await api.QueryAsync<Representative>("Representative",
                $"$filter=Company/ID eq {company.Id} and IsActive eq true&$top=1");
            representative = representatives.FirstOrDefault();
            if (representative != null) Log.Ok($"Representative retrieved: {representative.FullName} ({representative.Id})");
            else Log.Warn("No active Representative found for the company after data.xlsx import. Application creation may fail.");
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
                    Company = company, ProjectContract = projectContract
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
            Log.Warn("No import file found (data.xlsx / employees.xlsx / employees.csv).");
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
                ProjectContract = projectContract, IsEmployee = true, Company = company,
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
        var education = await educationImporter.CreateOneAsync(person.Id, eduLevel!.Id, eduInstitution!.Id, country.Id, specialty!.Id, 2007);
        if (education == null) { Log.Error("Education creation failed — aborting."); return; }
        Log.Ok($"Education: {education.Id}");

        Log.Step("Creating position history...");
        var history = await historyImporter.CreateOneAsync(person.Id, position.Id, department.Id, DateTime.Today.AddMonths(-1));
        if (history == null) { Log.Error("PositionHistory creation failed — aborting."); return; }
        Log.Ok($"PositionHistory: {history.Id}");

        Log.Step("Creating employee contract...");
        var contract = await contractImporter.CreateOneAsync(person.Id, history.Id, duration.Id, DateTime.Today, 6000m);
        if (contract == null) { Log.Error("EmployeeContract creation failed — aborting."); return; }
        Log.Ok($"EmployeeContract: {contract.Id}");

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
        if (companyHead == null || representative == null)
        {
            Log.Warn("CompanyHead or Representative not available — Application will be created without them.");
        }
        var application = await applicationImporter.CreateOneAsync(DateTime.Today, ApplicationTypeCategory.Employee, company.Id,
            companyHead?.Id ?? Guid.Empty, representative?.Id ?? Guid.Empty, appType.Id, appTypeFilter.Id);
        if (application == null) { Log.Error("Application creation failed — aborting."); return; }
        Log.Ok($"Application: {application.Id}");

        Log.Step("Creating application item...");
        var appItem = await appItemImporter.CreateOneAsync(
            application.Id,
            person.Id,
            passport.Id,
            currentPositionHistoryId: history.Id,
            currentEmployeeContractId: contract.Id);
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

        Log.Step("Creating city...");
        var city = await cityImporter.CreateOneAsync("Ashgabat", "Aşgabat", "ASB", region.Id, true);
        if (city == null) { Log.Error("City creation failed — aborting."); return; }
        Log.Ok($"City: {city.Id}");

        Log.Step("Creating lodging...");
        var lodging = await lodgingImporter.CreateOneAsync("Company Guesthouse", "100 Main Street", company.Id);
        if (lodging != null)
        {
            Log.Ok($"Lodging: {lodging.Id}");
            Log.Step("Creating address of residence...");
            await addressImporter.CreateOneAsync(person.Id, ResidenceType.Lodging, lodging.FullAddress, region.Id, city.Id, DateTime.Today, DateTime.Today.AddYears(1), lodging.Id);
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