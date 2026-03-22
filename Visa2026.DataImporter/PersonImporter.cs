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
        var items = await _api.GetAllAsync<Person>(Entity);
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
        Console.WriteLine($"=== POST {Entity}: {person.FullName} ===");

        object payload = BuildPayload(person); 

        try
        {
            var created = await _api.CreateAsync<Person>(Entity, payload);
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

        foreach (var record in records)
        {
            try
            {
                var payload = BuildPayload(record);
                await _api.CreateAsync<Person>(Entity, payload);
                Console.WriteLine($"  ✓ Imported: {record.FullName}");
                success++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Failed '{record.FullName}': {ex.Message}");
                fail++;
            }
        }

        Console.WriteLine($"  Done. Success={success}, Failed={fail}\n");
    }

    // ------------------------------------------------------------------
    // DELETE
    // ------------------------------------------------------------------
    public async Task DeleteAsync(Guid id)
    {
        Console.WriteLine($"=== DELETE {Entity} {id} ===");
        await _api.DeleteAsync(Entity, id);
        Console.WriteLine($"  Deleted.\n");
    }

    // ------------------------------------------------------------------
    // HELPER: Construct JSON Payload
    // ------------------------------------------------------------------
    private static object BuildPayload(Person p)
    {
        // Map scalar properties and handle Lookups by passing { ID = guid }
        return new
        {
            FirstName = p.FirstName,
            LastName = p.LastName,
            MiddleName = p.MiddleName,
            DateOfBirth = p.DateOfBirth,
            BirthPlace = p.BirthPlace,
            Gender = p.Gender != null ? new { ID = p.Gender.Id } : null,  // use existing Id, do not call Lookup
            Nationality = p.Nationality != null ? new { ID = p.Nationality.Id } : null, // Use existing id.
            CountryOfBirth = p.CountryOfBirth != null ? new { ID = p.CountryOfBirth.Id } : null, // use existing id
            MaritalStatus = p.MaritalStatus != null ? new { ID = p.MaritalStatus.Id } : null,
            ForeignAddress = p.ForeignAddress,
ForeignAddressCountry = p.ForeignAddressCountry != null ? new { ID = p.ForeignAddressCountry.Id } : null,
            // Employment details
            IsEmployee = p.IsEmployee,
            IsSubcontractorEmployee = p.IsSubcontractorEmployee,
            HireDate = p.HireDate,
            ProjectContract = p.ProjectContract != null ? new { ID = p.ProjectContract.Id } : null,
            Company = p.Company != null ? new { ID = p.Company.Id } : null,
            Subcontractor = p.Subcontractor != null ? new { ID = p.Subcontractor.Id } : null,
           Email = p.Email,
            // Family relationships
            SponsoringEmployee = p.SponsoringEmployee != null ? new { ID = p.SponsoringEmployee.Id } : null,
            Relationship = p.Relationship != null ? new { ID = p.Relationship.Id } : null
        };
    }

}