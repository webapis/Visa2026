using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Visa2026.DataImporter;

public class PersonImporter
{
    private readonly ApiClient _api;
    private const string Entity = "Person";

    public PersonImporter(ApiClient api)
    {
        _api = api;
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
        Console.WriteLine($"=== Bulk import {Entity}s (Batch) ===");

        var operations = new List<BatchOperation>();
        foreach (var record in records)
        {
            var payload = BuildPayload(record);
            operations.Add(new BatchOperation(HttpMethod.Post, $"api/odata/{Entity}", payload));
        }

        try
        {
            await _api.ExecuteBatchAsync(operations);
            Console.WriteLine($"  ✓ Successfully sent batch of {operations.Count} records.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ Batch request failed: {ex.Message}");
        }
        Console.WriteLine($"  Done.\n");
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
            Gender = p.Gender != null ? new { ID = p.Gender.Id } : null,
            Nationality = p.Nationality != null ? new { ID = p.Nationality.Id } : null,
            CountryOfBirth = p.CountryOfBirth != null ? new { ID = p.CountryOfBirth.Id } : null,
            MaritalStatus = p.MaritalStatus != null ? new { ID = p.MaritalStatus.Id } : null,
            ForeignAddress = p.ForeignAddress,
            ForeignAddressCountry = p.ForeignAddressCountry != null ? new { ID = p.ForeignAddressCountry.Id } : null,
            Email = p.Email,
            
            // Employment details
            IsEmployee = p.IsEmployee,
            IsSubcontractorEmployee = p.IsSubcontractorEmployee,
            HireDate = p.HireDate,
            ProjectContract = p.ProjectContract != null ? new { ID = p.ProjectContract.Id } : null,
            Company = p.Company != null ? new { ID = p.Company.Id } : null,
            Subcontractor = p.Subcontractor != null ? new { ID = p.Subcontractor.Id } : null,

            // Family relationships
            SponsoringEmployee = p.SponsoringEmployee != null ? new { ID = p.SponsoringEmployee.Id } : null,
            Relationship = p.Relationship != null ? new { ID = p.Relationship.Id } : null
        };
    }
}