using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Visa2026.DataImporter;

public class VisaImporter
{
    private readonly ApiClient _api;
    private const string Entity = "Visa";

    public VisaImporter(ApiClient api)
    {
        _api = api;
    }

    // ------------------------------------------------------------------
    // READ — list all
    // ------------------------------------------------------------------
    public async Task ListAllAsync()
    {
        Console.WriteLine($"=== GET all {Entity}s ===");
        var items = await _api.GetAllAsync<Visa>(Entity);
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
        Guid? applicationId = null,
        Guid? invitationId = null,
        List<Guid>? borderZoneCityIds = null,
        string notes = "")
    {
        Console.WriteLine($"=== POST {Entity}: {visaNumber} ===");

        var hasBorderZones = borderZoneCityIds != null && borderZoneCityIds.Count > 0;
        var hasInvitation = invitationId.HasValue;

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
            Application = applicationId.HasValue ? new { ID = applicationId.Value } : null,
            
            // Conditional Logic for Invitation
            HasInvitation = hasInvitation,
            Invitation = hasInvitation ? new { ID = invitationId!.Value } : null,

            // Conditional Logic for Border Zones
            HasBorderZonePermit = hasBorderZones,
            BorderZoneLocations = hasBorderZones 
                ? borderZoneCityIds!.Select(id => new { ID = id }).ToList() 
                : null
        };

        try
        {
            var created = await _api.CreateAsync<Visa>(Entity, payload);
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
                    HasInvitation = record.HasInvitation,
                    HasBorderZonePermit = record.HasBorderZonePermit,

                    VisaType = record.VisaType != null ? new { ID = record.VisaType.Id } : null,
                    VisaCategory = record.VisaCategory != null ? new { ID = record.VisaCategory.Id } : null,
                    VisaIssuedPlace = record.VisaIssuedPlace != null ? new { ID = record.VisaIssuedPlace.Id } : null,
                    Passport = record.Passport != null ? new { ID = record.Passport.Id } : null,
                    Application = record.Application != null ? new { ID = record.Application.Id } : null,
                    Invitation = record.Invitation != null ? new { ID = record.Invitation.Id } : null
                };

                await _api.CreateAsync<Visa>(Entity, payload);
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