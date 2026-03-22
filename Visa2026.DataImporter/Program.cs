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
try
{
    Console.WriteLine("=== Starting Master Import Workflow ===\n");

    // 1. Initialize Importers
    // -------------------------------------------------------
    // Note: We assume PersonImporter exists (from previous steps).
    // If not, you can substitute direct API calls.
    var personImporter = new PersonImporter(api);
    var historyImporter = new EmployeePositionHistoryImporter(api);
    var contractImporter = new EmployeeContractImporter(api);

    // 2. Fetch Prerequisites (Lookup Data)
    // -------------------------------------------------------
    Console.WriteLine("Fetching reference data...");

    // We need these IDs to create valid records.
    // Using $top=1 to just grab the first available record for this demo.
    var countries = await api.QueryAsync<Country>("Country", "$top=1");
    var genders = await api.QueryAsync<Gender>("Gender", "$top=1");
    var positions = await api.QueryAsync<Position>("Position", "$top=1");
    var departments = await api.QueryAsync<Department>("Department", "$top=1");
    var durations = await api.QueryAsync<ValidityDuration>("ValidityDuration", "$filter=NumberOfDays gt 30&$top=1");
    var projectContracts = await api.QueryAsync<ProjectContract>("ProjectContract", "$top=1");
    var maritalStatuses = await api.QueryAsync<MaritalStatus>("MaritalStatus", "$top=1");

    if (!countries.Any() || !genders.Any() || !positions.Any() || !departments.Any() || !projectContracts.Any())
    {
        Console.WriteLine("ERROR: Missing required reference data (Country, Gender, Position, Department, or ProjectContract).");
        Console.WriteLine("Please ensure the database is seeded.");
        return;
    }

    var country = countries.First();
    var gender = genders.First();
    var position = positions.First();
    var dept = departments.First();
    var duration = durations.FirstOrDefault(); // might be null
    var projectContract = projectContracts.First();
    var maritalStatus = maritalStatuses.First();

    // 3. Step 1: Import Person
    // -------------------------------------------------------
    Console.WriteLine("\n--- Step 1: Importing Person ---");
    
    // Define a new employee
    var newPerson = new Person
    {
        FirstName = "Dovran",
        LastName = "Amanov",
        DateOfBirth = new DateTime(1990, 5, 15),
        BirthPlace = "Ashgabat",
        Gender = gender,
        Nationality = country,
        CountryOfBirth = country,
        MaritalStatus = maritalStatus,
        ForeignAddress = "123 Test St",
        ForeignAddressCountry = country,
        ProjectContract = projectContract,
        IsEmployee = true,
        Email = $"d.amanov.{Guid.NewGuid().ToString()[..4]}@example.com" // Ensure uniqueness
    };

    // Use BulkImport (as normally Importers handle lists)
    await personImporter.BulkImportAsync(new[] { newPerson });

    // Retrieve the Person ID (since BulkImport might not return it directly, we query by Email)
    // In a real scenario, CreateOneAsync is preferred for sequential dependencies.
    var importedPeople = await api.QueryAsync<Person>("Person", $"$filter=Email eq '{newPerson.Email}'");
    var person = importedPeople.FirstOrDefault();

    if (person == null)
    {
        Console.WriteLine("Failed to retrieve imported person. Aborting.");
        return;
    }
    Console.WriteLine($"  -> Person ID resolved: {person.Id}");

    // 4. Step 2: Create Position History
    // -------------------------------------------------------
    Console.WriteLine("\n--- Step 2: Creating Position History ---");
    
    var history = await historyImporter.CreateOneAsync(
        personId: person.Id,
        positionId: position.Id,
        departmentId: dept.Id,
        startDate: DateTime.Today.AddMonths(-1)
    );

    if (history == null)
    {
        Console.WriteLine("Failed to create Position History. Aborting.");
        return;
    }

    // 5. Step 3: Create Employee Contract
    // -------------------------------------------------------
    Console.WriteLine("\n--- Step 3: Creating Employee Contract ---");

    if (duration != null)
    {
        var contract = await contractImporter.CreateOneAsync(
            personId: person.Id,
            positionHistoryId: history.Id,
            validityDurationId: duration.Id,
            startDate: DateTime.Today,
            salary: 5000m
        );

        Console.WriteLine(contract != null 
            ? "  -> Contract created successfully." 
            : "  -> Failed to create contract.");
    }
    else
    {
        Console.WriteLine("  -> Skipping contract (no ValidityDuration found).");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"\nCRITICAL ERROR: {ex.Message}");
}

Console.WriteLine("Import complete. Press any key to exit.");
Console.ReadKey();