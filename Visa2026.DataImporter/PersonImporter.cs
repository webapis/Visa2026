using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Visa2026.DataImporter;

public class PersonImporter : BaseImporter<Person>
{
    private const string Entity = "Person";

    public PersonImporter(ApiClient api) : base(api, Entity)
    {
    }

    // ------------------------------------------------------------------
    // READ — list all
    // ------------------------------------------------------------------
    public async Task ListAllAsync()
    {
        Console.WriteLine($"=== GET all {Entity}s ===");
        var items = await Api.GetAllAsync<Person>(Entity);
        if (items.Count == 0)
        {
            Console.WriteLine("  (no records found)");
        }
       foreach (var item in items)
        {
            Console.WriteLine($"  [{item.Id}] {item.FullName} ({item.Email})");
        }
        Console.WriteLine();
    }

    // ------------------------------------------------------------------
    // CREATE — single record
    // ------------------------------------------------------------------
public async Task<Person?> CreateOneAsync(Person person)
{
    Console.WriteLine($"=== POST {Entity}: {person.FirstName} {person.LastName} ===");

    object payload = BuildPayload(person);

    // TEMP DEBUG
    Console.WriteLine("  Payload: " + System.Text.Json.JsonSerializer.Serialize(payload,
        new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

    try
    {
        var created = await Api.CreateAsync<Person>(Entity, payload);
        Console.WriteLine($"  Created: {created?.Id}");
        return created;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  Error creating person: {ex.Message}");
        return null;
    }
}

    // ------------------------------------------------------------------
    // CREATE — bulk import from a list
    // ------------------------------------------------------------------
    public async Task BulkImportAsync(IEnumerable<Person> records)
    {
        await BulkImportLoopAsync(records, BuildPayload, record => record.FullName);
    }

    // ------------------------------------------------------------------
    // DELETE
    // ------------------------------------------------------------------
    public async Task DeleteAsync(Guid id)
    {
        Console.WriteLine($"=== DELETE {Entity} {id} ===");
        await Api.DeleteAsync(Entity, id);
        Console.WriteLine($"  Deleted.\n");
    }

    // ------------------------------------------------------------------
    // HELPER: Construct JSON Payload
    // ------------------------------------------------------------------
    private static object BuildPayload(Person p)
    {
        var payload = new Dictionary<string, object?>();

        // Required scalars
        payload["FirstName"]  = p.FirstName;
        payload["LastName"]   = p.LastName;
        payload["DateOfBirth"] = DateTime.SpecifyKind(p.DateOfBirth, DateTimeKind.Utc);
        payload["IsEmployee"] = p.IsEmployee;
        payload["IsSubcontractorEmployee"] = p.IsSubcontractorEmployee;

        // Optional scalars — only include when they have a value
        if (!string.IsNullOrWhiteSpace(p.MiddleName))   payload["MiddleName"]   = p.MiddleName;
        if (!string.IsNullOrWhiteSpace(p.BirthPlace))   payload["BirthPlace"]   = p.BirthPlace;
        if (!string.IsNullOrWhiteSpace(p.ForeignAddress)) payload["ForeignAddress"] = p.ForeignAddress;
        if (!string.IsNullOrWhiteSpace(p.Email))        payload["Email"]        = p.Email;
        if (p.HireDate.HasValue)                         payload["HireDate"]     = p.HireDate.Value;

        // Lookups — only include when the related object exists
        if (p.Gender              != null) payload["Gender"]               = new { ID = p.Gender.Id };
        if (p.Nationality         != null) payload["Nationality"]          = new { ID = p.Nationality.Id };
        if (p.CountryOfBirth      != null) payload["CountryOfBirth"]       = new { ID = p.CountryOfBirth.Id };
        if (p.MaritalStatus       != null) payload["MaritalStatus"]        = new { ID = p.MaritalStatus.Id };
        if (p.ForeignAddressCountry != null) payload["ForeignAddressCountry"] = new { ID = p.ForeignAddressCountry.Id };
        if (p.ProjectContract     != null) payload["ProjectContract"]      = new { ID = p.ProjectContract.Id };
        if (p.Subcontractor       != null) payload["Subcontractor"]        = new { ID = p.Subcontractor.Id };
        if (p.SponsoringEmployee  != null) payload["SponsoringEmployee"]   = new { ID = p.SponsoringEmployee.Id };
        if (p.Relationship        != null) payload["Relationship"]         = new { ID = p.Relationship.Id };

        return payload;
    }
}