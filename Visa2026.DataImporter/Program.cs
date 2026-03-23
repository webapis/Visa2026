using Visa2026.DataImporter;

// -----------------------------------------------------------------------
// Logging — writes timestamped entries to console AND a rolling log file.
// -----------------------------------------------------------------------
// -----------------------------------------------------------------------
// Configuration
// -----------------------------------------------------------------------
const string ApiBaseUrl = "https://localhost:5001";
const string UserName   = "Admin";
const string Password   = "";   // empty for default XAF Admin user

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
        Log.Phase("Starting Full Data Import Orchestration");

        BaseImporter<object>.ClearLookupCache();
        Log.Info("Lookup cache cleared.");

        // ===================================================================
        #region Phase 0 — Seed Lookup Tables
        // ===================================================================
        Log.Phase("Phase 0: Seeding lookup/reference tables");
        var lookupSeeder = new LookupSeeder(api);
        if (File.Exists("lookup.xlsm"))
        {
            Log.Info("Found lookup.xlsm — seeding all reference tables...");
            await lookupSeeder.SeedAllAsync("lookup.xlsm");
            Log.Ok("Phase 0 complete.");
        }
        else
        {
            Log.Warn("lookup.xlsm not found — skipping lookup seeding.");
            Log.Warn("Run will proceed but may fail at Phase 2 if tables are empty.");
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
        Log.Phase("Phase 3: Creating Company and Staffing Structure");

        Log.Step("Creating company...");
        var company = await companyImporter.CreateOneAsync("Global Exports Ltd.", "123 International Dr.", "555-1234", "contact@globalexports.com", "TAX123", "GE", 5, true);
        if (company == null) { Log.Error("Company creation failed — aborting."); return; }
        Log.Ok($"Company created: {company.Id}");

        Log.Step("Creating project contract...");
        var projectContract = await projectContractImporter.CreateOneAsync("Main Project", "MP-01", "Main project contract", company.Id, true);
        if (projectContract == null) { Log.Error("ProjectContract creation failed — aborting."); return; }
        Log.Ok($"ProjectContract created: {projectContract.Id}");

        Log.Step("Creating local employee...");
        var localEmployee = await localEmployeeImporter.CreateOneAsync("Mergen", "Atayev", company.Id);
        if (localEmployee == null) { Log.Error("LocalEmployee creation failed — aborting."); return; }
        Log.Ok($"LocalEmployee created: {localEmployee.Id}");

        Log.Step("Creating company head...");
        var companyHead = await companyHeadImporter.CreateOneAsync(company.Id, position.Id, true, localEmployeeId: localEmployee.Id);
        if (companyHead == null) { Log.Error("CompanyHead creation failed — aborting."); return; }
        Log.Ok($"CompanyHead created: {companyHead.Id}");

        Log.Step("Creating representative...");
        var representative = await representativeImporter.CreateOneAsync(company.Id, true, localEmployeeId: localEmployee.Id);
        if (representative == null) { Log.Error("Representative creation failed — aborting."); return; }
        Log.Ok($"Representative created: {representative.Id}");

        Log.Ok("Phase 3 complete.");
        #endregion

        // ===================================================================
        #region Phase 4 — Onboard Employee
        // ===================================================================
        Log.Phase("Phase 4: Onboarding Employee");
        Person? person = null;
        var excelImporter = new ExcelImporter(api);

        if (File.Exists("data.xlsx"))
        {
            Log.Info("Found data.xlsx — importing all mapped sheets...");
            await excelImporter.ImportFileAsync("data.xlsx");
            var importedPersons = await api.GetAllAsync<Person>("Person");
            person = importedPersons.LastOrDefault();
            Log.Info($"Person after Excel import: {(person == null ? "NULL — will use demo fallback" : person.FullName)}");
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
        Log.Ok($"Person: {person.FullName} ({person.Id})");

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

        Log.Ok("Phase 4 complete.");
        #endregion

        // ===================================================================
        #region Phase 5 — Application
        // ===================================================================
        Log.Phase("Phase 5: Creating Application");

        Log.Step("Creating application...");
        var application = await applicationImporter.CreateOneAsync(DateTime.Today, ApplicationTypeCategory.Employee, company.Id, companyHead.Id, representative.Id, appType.Id, appTypeFilter.Id);
        if (application == null) { Log.Error("Application creation failed — aborting."); return; }
        Log.Ok($"Application: {application.Id}");

        Log.Step("Creating application item...");
        var appItem = await appItemImporter.CreateOneAsync(application.Id, currentPositionHistoryId: history.Id, currentEmployeeContractId: contract.Id);
        if (appItem == null) { Log.Error("ApplicationItem creation failed — aborting."); return; }
        Log.Ok($"ApplicationItem: {appItem.Id}");

        Log.Step("Logging initial application progress...");
        await appProgressImporter.CreateOneAsync(application.Id, appState!.Id, appLocation!.Id, DateTime.Now, "Application submitted.");
        Log.Ok("Phase 5 complete.");
        #endregion

        // ===================================================================
        #region Phase 6 — Application Documents
        // ===================================================================
        Log.Phase("Phase 6: Creating Application Documents");

        Log.Step("Creating invitation...");
        var invitation = await invitationImporter.CreateOneAsync("INV-001", DateTime.Today, application.Id, duration.Id);
        if (invitation != null)
        {
            Log.Ok($"Invitation: {invitation.Id}");
            Log.Step("Creating invitation item...");
            await invitationItemImporter.CreateOneAsync(invitation.Id, person.Id, passport.Id);
            Log.Ok("InvitationItem created.");
        }
        else Log.Warn("Invitation creation failed — invitation item skipped.");

        Log.Step("Creating work permit...");
        var workPermit = await workPermitImporter.CreateOneAsync("WP-001", DateTime.Today, application.Id);
        if (workPermit != null)
        {
            Log.Ok($"WorkPermit: {workPermit.Id}");
            Log.Step("Creating work permit item...");
            await workPermitItemImporter.CreateOneAsync(workPermit.Id, person.Id, passport.Id, history.Id, "WPI-001", DateTime.Today, DateTime.Today.AddYears(1));
            Log.Ok("WorkPermitItem created.");
        }
        else Log.Warn("WorkPermit creation failed — work permit item skipped.");

        Log.Step("Creating visa...");
        var visa = await visaImporter.CreateOneAsync("V-98765", visaType!.Id, visaCategory!.Id, visaIssuedPlace!.Id, DateTime.Today, DateTime.Today, DateTime.Today.AddYears(1), passport.Id, application.Id, invitation?.Id);
        Log.Info($"Visa: {(visa == null ? "FAILED" : visa.Id.ToString())}");

        Log.Step("Creating registration...");
        var registration = await registrationImporter.CreateOneAsync(person.Id, DateTime.Today, "REG-123", DateTime.Today.AddYears(1), application.Id);
        Log.Info($"Registration: {(registration == null ? "FAILED" : registration.Id.ToString())}");

        Log.Step("Creating rejection...");
        var rejection = await rejectionImporter.CreateOneAsync(application.Id, "REJ-001", "Insufficient documents", DateTime.Today);
        if (rejection != null)
        {
            Log.Ok($"Rejection: {rejection.Id}");
            await rejectionItemImporter.CreateOneAsync(rejection.Id, person.Id, "Missing proof of funds.");
            Log.Ok("RejectionItem created.");
        }
        else Log.Warn("Rejection creation failed — rejection item skipped.");

        Log.Ok("Phase 6 complete.");
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
        await travelHistoryImporter.CreateOneAsync(person.Id, DateTime.Today.AddDays(-10), TravelType.External, MovementType.Entry);
        Log.Ok("TravelHistory created.");

        Log.Step("Creating business trip...");
        await businessTripImporter.CreateOneAsync(person.Id, "Client Meeting", country.Id, DateTime.Today.AddDays(30), DateTime.Today.AddDays(37));
        Log.Ok("BusinessTrip created.");

        Log.Ok("Phase 7 complete.");
        #endregion

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
    Console.WriteLine("\nPress any key to exit.");
    Console.ReadKey();
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