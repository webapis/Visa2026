using System;
using System.Collections.Generic;
using System.Linq;
using Visa2026.Module.Services.StateNotifications;

namespace Visa2026.Module.BusinessObjects.StateNotifications;

/// <summary>
/// Sample notifications for inbox UI/UX review (replace with live state evaluation later).
/// </summary>
public static class BoStateNotificationPrototypeData
{
    public static IList<BoStateNotificationItem> CreateSampleNotifications()
    {
        var today = DateTime.Today;
        return new List<BoStateNotificationItem>
        {
            new()
            {
                Severity = BoStateNotificationSeverity.Critical,
                Status = BoStateNotificationStatus.Open,
                BoType = "Passport",
                StateCode = "Expired",
                StateLabel = "Expired",
                DisplayKey = "TM 12 3456789",
                PersonName = "Ivan Petrov",
                Message = "Passport has expired. Request renewal from the employee before starting visa or work-permit applications.",
                EventDate = today.AddDays(-12),
                DaysRemaining = -12,
                DetectedAt = today.AddDays(-10).AddHours(8),
                TargetBoId = Guid.Parse("11111111-1111-1111-1111-111111111101"),
                TargetBoTypeName = "Passport",
            },
            new()
            {
                Severity = BoStateNotificationSeverity.Warning,
                Status = BoStateNotificationStatus.Open,
                BoType = "Passport",
                StateCode = "ExpiringSoon",
                StateLabel = "Expiring soon",
                DisplayKey = "P 98 7654321",
                PersonName = "Maria Schmidt",
                Message = "Passport validity is in the warning window. Ask the owner to begin renewal.",
                EventDate = today.AddDays(38),
                DaysRemaining = 38,
                DetectedAt = today.AddDays(-1).AddHours(14),
                TargetBoId = Guid.Parse("11111111-1111-1111-1111-111111111102"),
                TargetBoTypeName = "Passport",
            },
            new()
            {
                Severity = BoStateNotificationSeverity.Warning,
                Status = BoStateNotificationStatus.Open,
                BoType = "Visa",
                StateCode = "ExpiringSoon",
                StateLabel = "Expiring soon",
                DisplayKey = "V-2024-00891",
                PersonName = "Ahmet Yilmaz",
                Message = "Visa expires within the renewal window. Consider opening or continuing an extension application.",
                EventDate = today.AddDays(24),
                DaysRemaining = 24,
                DetectedAt = today.AddHours(-3),
                TargetBoId = Guid.Parse("22222222-2222-2222-2222-222222222201"),
                TargetBoTypeName = "Visa",
            },
            new()
            {
                Severity = BoStateNotificationSeverity.Critical,
                Status = BoStateNotificationStatus.Open,
                BoType = "WorkPermitItem",
                StateCode = "ExtensionApplicationRequired",
                StateLabel = "Extension required",
                DisplayKey = "WP-2023-0442",
                PersonName = "Chen Wei",
                Message = "Work permit is inside the 90-day extension window and no active extension application was found.",
                EventDate = today.AddDays(52),
                DaysRemaining = 52,
                DetectedAt = today.AddDays(-2),
                TargetBoId = Guid.Parse("33333333-3333-3333-3333-333333333301"),
                TargetBoTypeName = "WorkPermitItem",
            },
            new()
            {
                Severity = BoStateNotificationSeverity.Info,
                Status = BoStateNotificationStatus.Open,
                BoType = "MedicalRecord",
                StateCode = "ExpiringSoon",
                StateLabel = "Expiring soon",
                DisplayKey = "MR-2025-017",
                PersonName = "Elena Kozlova",
                Message = "Medical record will enter the warning window soon. Schedule renewal if required for the next application.",
                EventDate = today.AddDays(95),
                DaysRemaining = 95,
                DetectedAt = today.AddDays(-5),
                TargetBoId = Guid.Parse("44444444-4444-4444-4444-444444444401"),
                TargetBoTypeName = "MedicalRecord",
            },
            new()
            {
                Category = BoStateNotificationCategory.DataCompleteness,
                Severity = BoStateNotificationSeverity.Critical,
                Status = BoStateNotificationStatus.Open,
                BoType = "Person",
                StateCode = "MissingPassport",
                StateLabel = "Passport missing",
                DisplayKey = "Employee profile",
                PersonName = "Dmitri Volkov",
                MissingItemLabel = "Passport",
                Message = "No active passport on file for this employee. Add passport data before starting invitation, visa, or work-permit applications.",
                DetectedAt = today.AddDays(-3).AddHours(10),
                TargetBoId = Guid.Parse("66666666-6666-6666-6666-666666666601"),
                TargetBoTypeName = "Person",
            },
            new()
            {
                Category = BoStateNotificationCategory.DataCompleteness,
                Severity = BoStateNotificationSeverity.Warning,
                Status = BoStateNotificationStatus.Open,
                BoType = "Person",
                StateCode = "MissingEducation",
                StateLabel = "Education missing",
                DisplayKey = "Employee profile",
                PersonName = "Olena Kowalski",
                MissingItemLabel = "Education",
                Message = "No education record is linked to this person. Invitation and work-permit packages typically require education details.",
                DetectedAt = today.AddDays(-4),
                TargetBoId = Guid.Parse("66666666-6666-6666-6666-666666666602"),
                TargetBoTypeName = "Person",
            },
            new()
            {
                Category = BoStateNotificationCategory.DataCompleteness,
                Severity = BoStateNotificationSeverity.Critical,
                Status = BoStateNotificationStatus.Open,
                BoType = "Person",
                StateCode = "MissingMedicalRecord",
                StateLabel = "Medical record missing",
                DisplayKey = "Employee profile",
                PersonName = "Carlos Mendez",
                MissingItemLabel = "Medical record",
                Message = "No medical record on file. A valid medical record is required before invitation or visa processing can proceed.",
                DetectedAt = today.AddDays(-1).AddHours(16),
                TargetBoId = Guid.Parse("66666666-6666-6666-6666-666666666603"),
                TargetBoTypeName = "Person",
            },
            new()
            {
                Category = BoStateNotificationCategory.DataCompleteness,
                Severity = BoStateNotificationSeverity.Warning,
                Status = BoStateNotificationStatus.Open,
                BoType = "Education",
                StateCode = "MissingDiplomaCopies",
                StateLabel = "Diploma copies missing",
                DisplayKey = "Bachelor · MSU · 2018",
                PersonName = "Fatima Al-Rashid",
                MissingItemLabel = "Diploma copies",
                Message = "Education record exists but no diploma file is attached under Documents. Upload scanned diploma copies for application PDF packages.",
                DetectedAt = today.AddDays(-2).AddHours(9),
                TargetBoId = Guid.Parse("77777777-7777-7777-7777-777777777701"),
                TargetBoTypeName = "Education",
            },
            new()
            {
                Severity = BoStateNotificationSeverity.Critical,
                Status = BoStateNotificationStatus.Open,
                BoType = "Registration",
                StateCode = "RegistrationOverdue",
                StateLabel = "Registration overdue",
                DisplayKey = "REG pending",
                PersonName = "James Okafor",
                Message = "Person arrived but registration is overdue relative to policy. Open registration or check-out workflow.",
                EventDate = today.AddDays(-7),
                DaysRemaining = -7,
                DetectedAt = today.AddDays(-6),
                TargetBoId = Guid.Parse("55555555-5555-5555-5555-555555555501"),
                TargetBoTypeName = "Registration",
            },
            new()
            {
                Severity = BoStateNotificationSeverity.Warning,
                Status = BoStateNotificationStatus.Snoozed,
                BoType = "Passport",
                StateCode = "ExpiringSoon",
                StateLabel = "Expiring soon",
                DisplayKey = "KZ 11 2233445",
                PersonName = "Nurlan Aitov",
                Message = "Snoozed until next week — embassy appointment scheduled.",
                EventDate = today.AddDays(61),
                DaysRemaining = 61,
                DetectedAt = today.AddDays(-8),
                HandledAt = today.AddHours(-2),
                HandledBy = "demo.officer",
                TargetBoId = Guid.Parse("11111111-1111-1111-1111-111111111103"),
                TargetBoTypeName = "Passport",
            },
            new()
            {
                Severity = BoStateNotificationSeverity.Warning,
                Status = BoStateNotificationStatus.Done,
                BoType = "Visa",
                StateCode = "ExpiringSoon",
                StateLabel = "Expiring soon",
                DisplayKey = "V-2023-12004",
                PersonName = "Sofia Andersson",
                Message = "Auto-resolved after state sync — visa extension application APP-2026-0412 is in progress.",
                EventDate = today.AddDays(45),
                DaysRemaining = 45,
                DetectedAt = today.AddDays(-12),
                HandledAt = today.AddDays(-1),
                HandledBy = "State sync",
                TargetBoId = Guid.Parse("22222222-2222-2222-2222-222222222202"),
                TargetBoTypeName = "Visa",
            },
        };
    }

    public static BoStateNotificationSummary GetSummary()
    {
        var open = CreateSampleNotifications()
            .Where(n => n.Status == BoStateNotificationStatus.Open)
            .ToList();

        return new BoStateNotificationSummary
        {
            OpenCriticalCount = open.Count(n => n.Severity == BoStateNotificationSeverity.Critical),
            OpenWarningCount = open.Count(n => n.Severity == BoStateNotificationSeverity.Warning),
            OpenInfoCount = open.Count(n => n.Severity == BoStateNotificationSeverity.Info),
            OpenTotalCount = open.Count,
            OpenMissingDataCount = open.Count(n => n.Category == BoStateNotificationCategory.DataCompleteness),
        };
    }
}
