using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

 namespace Visa2026.DataImporter;
 public class VisaImporter : BaseImporter<Visa>
{
    private const string Entity = "Visa";

    public VisaImporter(ApiClient api) : base(api, "Visa")
    {
    }

    // ------------------------------------------------------------------
    // READ — list all
    // ------------------------------------------------------------------
    public async Task ListAllAsync()
    {
        Console.WriteLine($"=== GET all {Entity}s ===");
        var items = await Api.GetAllAsync<Visa>(Entity);
        if (items.Count == 0)
        {
            Console.WriteLine("  (no records found)");
        }
        foreach (var item in items)
        {
            var type = item.VisaType?.Code ?? "UNK";
            var cat = item.VisaCategory?.Code ?? "UNK";
            var person = item.Passport?.Person?.FullName ?? "Unknown";
            Console.WriteLine($"  [{item.Id}] {item.VisaNumber} ({type}/{cat}) - {person}");
        }
        Console.WriteLine();
    }

    // ------------------------------------------------------------------
    // CREATE — single record
    // ------------------------------------------------------------------
    public async Task<Visa?> CreateOneAsync(
        string visaNumber,
        Guid visaTypeId,
        Guid visaCategoryId,
        Guid visaIssuedPlaceId,
        DateTime issueDate,
        DateTime startDate,
        DateTime expirationDate,
        Guid passportId,
        Guid? issuingApplicationItemId = null,
        Guid? invitationItemId = null,
        string? borderZoneLocation = null,
        string notes = "")
    {
        Console.WriteLine($"=== POST {Entity}: {visaNumber} ===");

        var payload = new
        {
            VisaNumber = visaNumber,
            IssueDate = issueDate,
            StartDate = startDate,
            ExpirationDate = expirationDate,
            Notes = notes,

            // Required Relationships
            VisaType = new { ID = visaTypeId },
            VisaCategory = new { ID = visaCategoryId },
            VisaIssuedPlace = new { ID = visaIssuedPlaceId },
            Passport = new { ID = passportId },

            // Optional Relationships
            IssuingApplicationItem = issuingApplicationItemId.HasValue ? new { ID = issuingApplicationItemId.Value } : null,
            InvitationItem = invitationItemId.HasValue ? new { ID = invitationItemId.Value } : null,

            BorderZoneLocation = string.IsNullOrWhiteSpace(borderZoneLocation) ? null : borderZoneLocation.Trim()
        };

        try
        {
            var created = await Api.CreateAsync<Visa>(Entity, payload);
            Console.WriteLine($"  Created Visa ID: {created?.Id}");
            return created;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Error creating Visa: {ex.Message}");
            return null;
        }
    }

    // ------------------------------------------------------------------
    // CREATE — bulk import from a list
    // ------------------------------------------------------------------
    public async Task BulkImportAsync(IEnumerable<Visa> records)
    {
        Console.WriteLine($"=== Bulk import {Entity}s ===");
        int success = 0, fail = 0;

        foreach (var record in records)
        {
            try
            {
            var payload = new
                {
                    VisaNumber = record.VisaNumber,
                    IssueDate = record.IssueDate,
                    StartDate = record.StartDate,
                    ExpirationDate = record.ExpirationDate,
                    Notes = record.Notes,
                    BorderZoneLocation = string.IsNullOrWhiteSpace(record.BorderZoneLocation) ? null : record.BorderZoneLocation.Trim(),

                    VisaType = record.VisaType != null ? new { ID = record.VisaType.Id } : null,
                    VisaCategory = record.VisaCategory != null ? new { ID = record.VisaCategory.Id } : null,
                    VisaIssuedPlace = record.VisaIssuedPlace != null ? new { ID = record.VisaIssuedPlace.Id } : null,
                    Passport = record.Passport != null ? new { ID = record.Passport.Id } : null,
                    IssuingApplicationItem = record.IssuingApplicationItem != null ? new { ID = record.IssuingApplicationItem.Id } : null,
                    InvitationItem = record.InvitationItem != null ? new { ID = record.InvitationItem.Id } : null
                };

                await Api.CreateAsync<Visa>(Entity, payload);
                Console.WriteLine($"  ✓ Imported Visa: {record.VisaNumber}");
                success++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Failed '{record.VisaNumber}': {ex.Message}");
                fail++;
            }
        }

        Console.WriteLine($"  Done. Success={success}, Failed={fail}\n");
    }
}