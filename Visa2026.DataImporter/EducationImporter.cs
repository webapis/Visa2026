using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Visa2026.DataImporter;

public class EducationImporter
{
    private readonly ApiClient _api;
    private const string Entity = "Education";

    public EducationImporter(ApiClient api)
    {
        _api = api;
    }

    // ------------------------------------------------------------------
    // READ — list all
    // ------------------------------------------------------------------
    public async Task ListAllAsync()
    {
        Console.WriteLine($"=== GET all {Entity}s ===");
        var items = await _api.GetAllAsync<Education>(Entity);
        if (items.Count == 0)
        {
            Console.WriteLine("  (no records found)");
        }
        foreach (var item in items)
        {
            var personName = item.Person?.FullName ?? "Unknown Person";
            var degree = item.EducationLevel?.Name ?? "Unknown Level";
            var school = item.EducationInstitution?.Name ?? "Unknown Institution";
            Console.WriteLine($"  [{item.Id}] {personName}: {degree} at {school} ({item.GraduationYear})");
        }
        Console.WriteLine();
    }

    // ------------------------------------------------------------------
    // CREATE — single record
    // ------------------------------------------------------------------
    public async Task<Education?> CreateOneAsync(
        Guid personId,
        Guid educationLevelId,
        Guid institutionId,
        Guid countryId,
        Guid specialtyId,
        int graduationYear)
    {
        Console.WriteLine($"=== POST {Entity} for Person ID: {personId} ===");

        var payload = new
        {
            Person = new { ID = personId },
            EducationLevel = new { ID = educationLevelId },
            EducationInstitution = new { ID = institutionId },
            EducationCountry = new { ID = countryId },
            Specialty = new { ID = specialtyId },
            GraduationYear = graduationYear
        };

        try
        {
            var created = await _api.CreateAsync<Education>(Entity, payload);
            Console.WriteLine($"  Created Education ID: {created?.Id}");
            return created;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Error creating Education: {ex.Message}");
            return null;
        }
    }

    // ------------------------------------------------------------------
    // CREATE — bulk import from a list
    // ------------------------------------------------------------------
    public async Task BulkImportAsync(IEnumerable<Education> records)
    {
        Console.WriteLine($"=== Bulk import {Entity}s ===");
        int success = 0, fail = 0;

        foreach (var record in records)
        {
            try
            {
                var payload = new
                {
                    Person = record.Person != null ? new { ID = record.Person.Id } : null,
                    EducationLevel = record.EducationLevel != null ? new { ID = record.EducationLevel.Id } : null,
                    EducationInstitution = record.EducationInstitution != null ? new { ID = record.EducationInstitution.Id } : null,
                    EducationCountry = record.EducationCountry != null ? new { ID = record.EducationCountry.Id } : null,
                    Specialty = record.Specialty != null ? new { ID = record.Specialty.Id } : null,
                    GraduationYear = record.GraduationYear
                };

                await _api.CreateAsync<Education>(Entity, payload);
                var school = record.EducationInstitution?.Name ?? "Unknown School";
                Console.WriteLine($"  ✓ Imported Education: {school} ({record.GraduationYear})");
                success++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Failed import: {ex.Message}");
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
        await _api.DeleteAsync(Entity, id);
        Console.WriteLine($"  Deleted Education {id}\n");
    }
}