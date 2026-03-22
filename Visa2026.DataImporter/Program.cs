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
    // In a real scenario, you would fetch or create all required lookup data.
    // For this demo, we query the first available record for each required type.
    var country = (await api.QueryAsync<Country>("Country", "$top=1")).FirstOrDefault();
    var gender = (await api.QueryAsync<Gender>("Gender", "$top=1")).FirstOrDefault();
    var position = (await api.QueryAsync<Position>("Position", "$top=1")).FirstOrDefault();
    var department = (await api.QueryAsync<Department>("Department", "$top=1")).FirstOrDefault();
    var duration = (await api.QueryAsync<ValidityDuration>("ValidityDuration", "$top=1")).FirstOrDefault();
    var maritalStatus = (await api.QueryAsync<MaritalStatus>("MaritalStatus", "$top=1")).FirstOrDefault();
    var passportType = (await api.QueryAsync<PassportType>("PassportType", "$top=1")).FirstOrDefault();
    var eduLevel = (await api.QueryAsync<EducationLevel>("EducationLevel", "$top=1")).FirstOrDefault();
    var eduInstitution = (await api.QueryAsync<EducationInstitution>("EducationInstitution", "$top=1")).FirstOrDefault();
    var specialty = (await api.QueryAsync<Specialty>("Specialty", "$top=1")).FirstOrDefault();
    var appType = (await api.QueryAsync<ApplicationType>("ApplicationType", "$top=1")).FirstOrDefault();
    var appState = (await api.QueryAsync<ApplicationState>("ApplicationState", "$top=1")).FirstOrDefault();
    var appLocation = (await api.QueryAsync<ApplicationLocation>("ApplicationLocation", "$top=1")).FirstOrDefault();
    var visaType = (await api.QueryAsync<VisaType>("VisaType", "$top=1")).FirstOrDefault();
    var visaCategory = (await api.QueryAsync<VisaCategory>("VisaCategory", "$top=1")).FirstOrDefault();
    var visaIssuedPlace = (await api.QueryAsync<VisaIssuedPlace>("VisaIssuedPlace", "$top=1")).FirstOrDefault();
    var region = (await api.QueryAsync<Region>("Region", "$top=1")).FirstOrDefault();

    // Basic validation to ensure the script can run
    if (country == null || position == null || department == null || duration == null || appType == null || region == null)
    {
        Console.WriteLine("CRITICAL ERROR: Core lookup data is missing. Please seed the database first.");
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
    
    // Prepare dictionaries for fast lookup
    var allCountries = (await api.GetAllAsync<Country>("Country"))
        .ToDictionary(c => c.Code, StringComparer.OrdinalIgnoreCase);
    var allGenders = (await api.GetAllAsync<Gender>("Gender"))
        .ToDictionary(g => g.Name, StringComparer.OrdinalIgnoreCase);
    var allMarital = (await api.GetAllAsync<MaritalStatus>("MaritalStatus"))
        .ToDictionary(m => m.Name, StringComparer.OrdinalIgnoreCase);

    List<Person> importedPersons = new();

    // STRATEGY A: Try Excel (.xlsx) Import
    if (File.Exists("employees.xlsx"))
    {
        Console.WriteLine("Found employees.xlsx, starting bulk import...");
        importedPersons = ExcelParser.Parse("employees.xlsx", row =>
        {
            // Helper: safe cast from Excel object
            string Str(int i) => i < row.Count && row[i] != null ? row[i].ToString()!.Trim() : "";
            DateTime Date(int i)
            {
                if (i >= row.Count || row[i] == null) return DateTime.MinValue;
                if (row[i] is DateTime dt) return dt; // Excel native date
                return DateTime.TryParse(row[i].ToString(), out var parsed) ? parsed : DateTime.MinValue;
            }

            return new Person
            {
                FirstName = Str(0),
                LastName = Str(1),
                Email = Str(2),
                DateOfBirth = Date(3),
                BirthPlace = Str(4),
                ForeignAddress = Str(5),
                IsEmployee = true,
                
                Nationality = allCountries.GetValueOrDefault(Str(6)) ?? country,
                CountryOfBirth = allCountries.GetValueOrDefault(Str(7)) ?? country,
                ForeignAddressCountry = allCountries.GetValueOrDefault(Str(8)) ?? country,
                Gender = allGenders.GetValueOrDefault(Str(9)) ?? gender,
                MaritalStatus = allMarital.GetValueOrDefault(Str(10)) ?? maritalStatus,
                
                Company = company,
                ProjectContract = projectContract
            };
        }, hasHeader: true).ToList();
    }
    // Check for CSV file for bulk import
    else if (File.Exists("employees.csv"))
    {
        Console.WriteLine("Found employees.csv, starting bulk import...");

        // Parse CSV
        // Example of using index-based parsing. Assumes the following column order:
        // 0: FirstName, 1: LastName, 2: Email, 3: DateOfBirth, 4: BirthPlace, 5: ForeignAddress,
        // 6: NationalityCode, 7: BirthCountryCode, 8: AddressCountryCode, 9: Gender, 10: MaritalStatus
        importedPersons = CsvParser.Parse("employees.csv", row =>
        {
            string Val(int index) => (index < row.Count) ? row[index].Trim() : "";
            
            return new Person
            {
                FirstName = Val(0),
                LastName = Val(1),
                Email = Val(2),
                DateOfBirth = DateTime.TryParse(Val(3), out var dob) ? dob : DateTime.MinValue,
                BirthPlace = Val(4),
                ForeignAddress = Val(5),
                IsEmployee = true,
                
                // Lookups with fallback to Phase 2 defaults
                Nationality = allCountries.GetValueOrDefault(Val(6)) ?? country,
                CountryOfBirth = allCountries.GetValueOrDefault(Val(7)) ?? country,
                ForeignAddressCountry = allCountries.GetValueOrDefault(Val(8)) ?? country,
                Gender = allGenders.GetValueOrDefault(Val(9)) ?? gender,
                MaritalStatus = allMarital.GetValueOrDefault(Val(10)) ?? maritalStatus,
                
                Company = company,
                ProjectContract = projectContract
            };
        }, hasHeader: true).ToList();
    }

    // 3. Import and recover last person for demo continuity
    if (importedPersons.Any())
    {
        await personImporter.BulkImportAsync(importedPersons);
        var lastEmail = importedPersons.Last().Email;
        person = (await api.QueryAsync<Person>("Person", $"$filter=Email eq '{lastEmail}'")).FirstOrDefault();
    }

    // Fallback to manual creation if no CSV or import failed
    if (person == null) return;
    {
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