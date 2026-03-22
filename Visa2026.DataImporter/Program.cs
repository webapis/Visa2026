using Visa2026.DataImporter;

// -----------------------------------------------------------------------
// Configuration
// -----------------------------------------------------------------------
const string ApiBaseUrl = "https://localhost:5001";
const string UserName   = "Admin";
const string Password   = "";   // empty for default XAF Admin user

// -----------------------------------------------------------------------
// Set up client, wait for server, then authenticate
// -----------------------------------------------------------------------
var api = new ApiClient(ApiBaseUrl, UserName, Password);

try
{
    // Wait up to 120 seconds for Blazor Server to finish starting up,
    // polling every 3 seconds. Remove this if running the importer
    // manually after the server is already running.
    await api.WaitForServerAsync(maxWaitSeconds: 120, pollIntervalSeconds: 3);

    await api.LoginAsync();
}
catch (TimeoutException ex)
{
    Console.WriteLine($"\nERROR: {ex.Message}");
    Console.WriteLine("Make sure Visa2026.Blazor.Server is running and try again.");
    Console.ReadKey();
    return;
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"\nERROR: Could not connect to {ApiBaseUrl}");
    Console.WriteLine($"Details: {ex.Message}");
    Console.WriteLine("\nMake sure Visa2026.Blazor.Server is running and try again.");
    Console.ReadKey();
    return;
}

// -----------------------------------------------------------------------
// Run CRUD operations
// -----------------------------------------------------------------------
var importer = new VisaTypeImporter(api);

try
{
    Console.WriteLine("=== Starting Full Data Import Orchestration ===\n");

    // Clear the lookup cache before running the import
    BaseImporter<object>.ClearLookupCache();

    #region 1. Initialize Importers
    Console.WriteLine("--- Phase 1: Initializing all data importers ---");
    var companyImporter = new CompanyImporter(api);
    var localEmployeeImporter = new LocalEmployeeImporter(api);
    var companyHeadImporter = new CompanyHeadImporter(api);
    var representativeImporter = new RepresentativeImporter(api);
    var projectContractImporter = new ProjectContractImporter(api);
    var personImporter = new PersonImporter(api);
    var passportImporter = new PassportImporter(api);
    var educationImporter = new EducationImporter(api);
    var historyImporter = new EmployeePositionHistoryImporter(api);
    var contractImporter = new EmployeeContractImporter(api);
    var medicalRecordImporter = new MedicalRecordImporter(api);
    var applicationImporter = new ApplicationImporter(api);
    var appItemImporter = new ApplicationItemImporter(api);
    var appProgressImporter = new ApplicationProgressImporter(api);
    var invitationImporter = new InvitationImporter(api);
    var invitationItemImporter = new InvitationItemImporter(api);
    var workPermitImporter = new WorkPermitImporter(api);
    var workPermitItemImporter = new WorkPermitItemImporter(api);
    var visaImporter = new VisaImporter(api);
    var registrationImporter = new RegistrationImporter(api);
    var rejectionImporter = new RejectionImporter(api);
    var rejectionItemImporter = new RejectionItemImporter(api);
    var travelHistoryImporter = new TravelHistoryImporter(api);
    var lodgingImporter = new LodgingImporter(api);
    var addressImporter = new AddressOfResidenceImporter(api);
    var cityImporter = new CityImporter(api);
        var businessTripImporter = new BusinessTripImporter(api);
    Console.WriteLine("All importers are ready.\n");
    #endregion

    #region 2. Fetch Prerequisite Lookup Data
    Console.WriteLine("--- Phase 2: Fetching prerequisite lookup data ---");

    static async Task<TLookup?> SafeQuery<TLookup>(ApiClient client, string entity) where TLookup : class
    {
        try
        {
            var results = await client.QueryAsync<TLookup>(entity, "$top=1");
            var item = results.FirstOrDefault();
            if (item == null)
                Console.WriteLine($"  \u26a0 WARNING: No records found for \'{entity}\'. Seed this lookup table first.");
            else
                Console.WriteLine($"  \u2713 {entity}");
            return item;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  \u2717 FAILED querying \'{entity}\': {ex.Message}");
            return null;
        }
    }

    // Replaces api.LookupAsync<T> calls: looks up a single record by Code or Name filter.
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
            Console.WriteLine($"  \u2717 FAILED lookup for {entity} where {field}=\'{value}\': {ex.Message}");
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

    // Abort only if critical lookups failed — non-critical ones (gender, passportType etc.) are nullable downstream
    bool lookupsFailed =
        country == null || position == null || department == null ||
        duration == null || appType == null || region == null;

    if (lookupsFailed)
    {
        Console.WriteLine("\nCRITICAL ERROR: One or more core lookup tables are empty or unreachable (see above).");
        Console.WriteLine("Please seed the database and try again.");
        return;
    }
    Console.WriteLine("Successfully fetched required lookup data.\n");
    #endregion

    #region 3. Create Company and Staffing Structure
    Console.WriteLine("--- Phase 3: Creating Company and Staffing Structure ---");
    var company = await companyImporter.CreateOneAsync("Global Exports Ltd.", "123 International Dr.", "555-1234", "contact@globalexports.com", "TAX123", "GE", 5, true);
    if (company == null) return;

    var projectContract = await projectContractImporter.CreateOneAsync("Main Project", "MP-01", "Main project contract", company.Id, true);
    if (projectContract == null) return;

    var localEmployee = await localEmployeeImporter.CreateOneAsync("Mergen", "Atayev", company.Id);
    if (localEmployee == null) return;

    var companyHead = await companyHeadImporter.CreateOneAsync(company.Id, position.Id, true, localEmployeeId: localEmployee.Id);
    if (companyHead == null) return;

    var representative = await representativeImporter.CreateOneAsync(company.Id, true, localEmployeeId: localEmployee.Id);
    if (representative == null) return;
    Console.WriteLine("Company structure created.\n");
    #endregion

    #region 4. Onboard a New Employee
    Console.WriteLine("--- Phase 4: Onboarding a new Employee ---");
    Person? person = null;

    var excelImporter = new ExcelImporter(api);

    // STRATEGY A: multi-sheet Excel file — ExcelImporter handles all mapped sheets
    // (Persons, Passports, TravelHistory, etc.) using header-based column mapping.
    // Sheet names and column titles must match those defined in ExcelMappings.cs.
    if (File.Exists("data.xlsx"))
    {
        Console.WriteLine("Found data.xlsx — importing all mapped sheets...");
        await excelImporter.ImportFileAsync("data.xlsx");

        // Re-query the last imported person to use in subsequent phases.
        // Adjust the filter to match however you identify your target person.
        var importedPersons = await api.GetAllAsync<Person>("Person");
        person = importedPersons.LastOrDefault();
    }
    // STRATEGY B: persons-only Excel (legacy single-sheet format)
    else if (File.Exists("employees.xlsx"))
    {
        Console.WriteLine("Found employees.xlsx — importing Persons sheet only...");
        await excelImporter.ImportSheetAsync("employees.xlsx", "Persons");

        var importedPersons = await api.GetAllAsync<Person>("Person");
        person = importedPersons.LastOrDefault();
    }
    // STRATEGY C: CSV fallback — still uses the old index-based CsvParser
    else if (File.Exists("employees.csv"))
    {
        Console.WriteLine("Found employees.csv, starting bulk import...");
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

        if (csvPersons.Any())
        {
            await personImporter.BulkImportAsync(csvPersons);
            var lastEmail = csvPersons.Last().Email;
            person = (await api.QueryAsync<Person>("Person", $"$filter=Email eq '{lastEmail}'")).FirstOrDefault();
        }
    }

    // Fallback: create a single demo person manually if no file was found or import failed
    if (person == null)
    {
        Console.WriteLine("No import file found or no persons imported — creating demo person...");
        person = await personImporter.CreateOneAsync(new Person
        {
            FirstName = "John", LastName = "Smith", DateOfBirth = new DateTime(1985, 1, 1),
            BirthPlace = "London", Gender = gender, Nationality = country, CountryOfBirth = country,
            MaritalStatus = maritalStatus, ForeignAddress = "456 Oak Avenue", ForeignAddressCountry = country,
            ProjectContract = projectContract, IsEmployee = true, Company = company,
            Email = $"j.smith.{Guid.NewGuid().ToString()[..4]}@example.com"
        });
    }
    if (person == null) return;

    var passport = await passportImporter.CreateOneAsync("P123456", "S98765", "UKPA", DateTime.Today.AddYears(-5), DateTime.Today.AddYears(5), person.Id, passportType.Id, country.Id);
    if (passport == null) return;

    var education = await educationImporter.CreateOneAsync(person.Id, eduLevel.Id, eduInstitution.Id, country.Id, specialty.Id, 2007);
    if (education == null) return;

    var history = await historyImporter.CreateOneAsync(person.Id, position.Id, department.Id, DateTime.Today.AddMonths(-1));
    if (history == null) return;

    var contract = await contractImporter.CreateOneAsync(person.Id, history.Id, duration.Id, DateTime.Today, 6000m);
    if (contract == null) return;

    var medicalRecord = await medicalRecordImporter.CreateOneAsync(person.Id, "MED998877", DateTime.Today, duration.Id);
    if (medicalRecord == null) return;
    Console.WriteLine("Employee onboarding complete.\n");
    #endregion

    #region 5. Create and Process an Application
    Console.WriteLine("--- Phase 5: Creating and Processing an Application ---");
    var application = await applicationImporter.CreateOneAsync(DateTime.Today, ApplicationTypeCategory.Employee, company.Id, companyHead.Id, representative.Id, appType.Id, appType.Id);
    if (application == null) return;

    var appItem = await appItemImporter.CreateOneAsync(application.Id, currentPositionHistoryId: history.Id, currentEmployeeContractId: contract.Id);
    if (appItem == null) return;

    await appProgressImporter.CreateOneAsync(application.Id, appState.Id, appLocation.Id, DateTime.Now, "Application submitted.");
    Console.WriteLine("Application created and initial progress logged.\n");
    #endregion

    #region 6. Create Application-Related Documents
    Console.WriteLine("--- Phase 6: Creating Application-related Documents ---");
    // Invitation
    var invitation = await invitationImporter.CreateOneAsync("INV-001", DateTime.Today, application.Id, duration.Id);
    if (invitation != null)
    {
        await invitationItemImporter.CreateOneAsync(invitation.Id, person.Id, passport.Id);
    }

    // Work Permit
    var workPermit = await workPermitImporter.CreateOneAsync("WP-001", DateTime.Today, application.Id);
    if (workPermit != null)
    {
        await workPermitItemImporter.CreateOneAsync(workPermit.Id, person.Id, passport.Id, history.Id, "WPI-001", DateTime.Today, DateTime.Today.AddYears(1));
    }

    // Visa
    var visa = await visaImporter.CreateOneAsync("V-98765", visaType.Id, visaCategory.Id, visaIssuedPlace.Id, DateTime.Today, DateTime.Today, DateTime.Today.AddYears(1), passport.Id, application.Id, invitation?.Id);

    // Registration
    var registration = await registrationImporter.CreateOneAsync(person.Id, DateTime.Today, "REG-123", DateTime.Today.AddYears(1), application.Id);

    // Rejection (example of a failed process)
    var rejection = await rejectionImporter.CreateOneAsync(application.Id, "REJ-001", "Insufficient documents", DateTime.Today);
    if (rejection != null)
    {
        await rejectionItemImporter.CreateOneAsync(rejection.Id, person.Id, "Missing proof of funds.");
    }
    Console.WriteLine("Application documents created.\n");
    #endregion

    #region 7. Create Miscellaneous Person-Related Records
    Console.WriteLine("--- Phase 7: Creating Miscellaneous Person-Related Records ---");
    // City (as a generic lookup example)
    var city = await cityImporter.CreateOneAsync("Ashgabat", "Aşgabat", "ASB", region.Id, true);
    if (city == null) return;

    // Lodging and Address
    var lodging = await lodgingImporter.CreateOneAsync("Company Guesthouse", "100 Main Street", company.Id);
    if (lodging != null)
    {
        await addressImporter.CreateOneAsync(person.Id, ResidenceType.Lodging, lodging.FullAddress, region.Id, city.Id, DateTime.Today, DateTime.Today.AddYears(1), lodging.Id);
    }

    // Travel History
    await travelHistoryImporter.CreateOneAsync(person.Id, DateTime.Today.AddDays(-10), TravelType.External, MovementType.Entry);

    // Business Trip
    await businessTripImporter.CreateOneAsync(person.Id, "Client Meeting", country.Id, DateTime.Today.AddDays(30), DateTime.Today.AddDays(37));
    Console.WriteLine("Miscellaneous records created.\n");
    #endregion
    
}
catch (Exception ex)
{
    Console.WriteLine($"\nCRITICAL ERROR: {ex.Message}");
}

Console.WriteLine("Import complete. Press any key to exit.");
Console.ReadKey();