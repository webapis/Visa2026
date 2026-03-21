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

// 1. List existing records
await importer.ListAllAsync();

// 2. Bulk import sample data — replace with your real data source
var sampleData = new List<VisaType>
{
    new() { Name = "Tourist Visa",   NameTm = "Syýahatçy Wizasy", Code = "TV" },
    new() { Name = "Business Visa",  NameTm = "Iş Wizasy",        Code = "BV" },
    new() { Name = "Transit Visa",   NameTm = "Tranzit Wizasy",    Code = "TR" },
    new() { Name = "Student Visa",   NameTm = "Talyp Wizasy",      Code = "SV" },
};
await importer.BulkImportAsync(sampleData);

// 3. List again to confirm
await importer.ListAllAsync();

// 4. Create one and update/delete it to demo full CRUD
var created = await importer.CreateOneAsync("Diplomatic Visa", "Diplomatik Wiza", "DV");
if (created is not null)
{
    await importer.UpdateAsync(created.Id, "Diplomatic Visa (Updated)");

    var updated = await api.GetByIdAsync<VisaType>("VisaType", created.Id);
    Console.WriteLine($"Verified: {updated}\n");

    await importer.DeleteAsync(created.Id);
}

await importer.ListAllAsync();

Console.WriteLine("Import complete. Press any key to exit.");
Console.ReadKey();