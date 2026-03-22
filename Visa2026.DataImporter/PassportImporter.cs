using System;
using System.Collections.Generic;
using System.Threading.Tasks;

 namespace Visa2026.DataImporter;

 public class PassportImporter : BaseImporter<Passport>
{

  public PassportImporter(ApiClient api) : base(api, "Passport")
    {
        _api = api;
    }

    // ------------------------------------------------------------------
    // READ — list all
    // ------------------------------------------------------------------
    public async Task ListAllAsync()
    {
        Console.WriteLine($"=== GET all {Entity}s ===");
        var items = await _api.GetAllAsync<Passport>(Entity);
        if (items.Count == 0)
        {
            Console.WriteLine("  (no records found)");
        }
        foreach (var item in items)
        {
            Console.WriteLine($"  [{item.Id}] {item.PassportNumber} ({item.Person?.FullName}) - Exp: {item.ExpirationDate:d}");
        }
        Console.WriteLine();
    }

    // ------------------------------------------------------------------
    // CREATE — single record
    // ------------------------------------------------------------------
    public async Task<Passport?> CreateOneAsync(
        string passportNumber,
        string personalNumber,
        string authority,
        DateTime issueDate,
        DateTime expirationDate,
        Guid personId,
        Guid passportTypeId,
        Guid issuedCountryId)
    {
        Console.WriteLine($"=== POST {Entity}: {passportNumber} ===");
  
        var payload = new
        {
            PassportNumber = passportNumber,
            PersonalNumber = personalNumber,
            Authority = authority,
            IssueDate = issueDate,
            ExpirationDate = expirationDate,
           // Relationships must be mapped via ID
            Person = new { ID = personId },
   PassportType = new { ID = passportTypeId },
            IssuedCountry = new { ID = issuedCountryId }
        };

        try
        {
            var created = await _api.CreateAsync<Passport>(Entity, payload);
            Console.WriteLine($"  Created Passport ID: {created?.Id}");
            Console.WriteLine();
            return created;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Error creating passport: {ex.Message}");
            return null;
        }
    }

    // ------------------------------------------------------------------
    // CREATE — bulk import from a list
    // ------------------------------------------------------------------
    public async Task BulkImportAsync(IEnumerable<Passport> records)
    {
        Console.WriteLine($"=== Bulk import {Entity}s ===");
        int success = 0, fail = 0;

        foreach (var record in records)
        {
            try
            {
                var payload = new
                {
                    PassportNumber = record.PassportNumber,
                    PersonalNumber = record.PersonalNumber,
                    Authority = record.Authority,
                    IssueDate = record.IssueDate,
                    ExpirationDate = record.ExpirationDate,
                    Person = record.Person != null ? new { ID = record.Person.Id } : null,
                    PassportType = record.PassportType != null ? new { ID = record.PassportType.Id } : null,
                    IssuedCountry = record.IssuedCountry != null ? new { ID = record.IssuedCountry.Id } : null
                };

                await _api.CreateAsync<Passport>(Entity, payload);
                Console.WriteLine($"  ✓ Imported passport: {record.PassportNumber}");
                success++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Failed '{record.PassportNumber}': {ex.Message}");
                fail++;
            }
        }

        Console.WriteLine($"  Done. Success={success}, Failed={fail}\n");
    }
}