using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Blazor.Server.Services.StateSnapshot
{
    public interface IVisaV06StateSnapshotService
    {
        Task RecomputeAsync(DateTime today);
    }

    public static class VisaV06StateCodes
    {
        public const string ExpiredToBeCheckedOut = "Visa|ExpiredToBeCheckedOut";
        public const string ExpiredOnCheckOutProcess = "Visa|ExpiredOnCheckOutProcess";
        public const string ExpiredCheckedOut = "Visa|ExpiredCheckedOut";
        public const string ExpiredMissedTimelyCheckout = "Visa|ExpiredMissedTimelyCheckout";

        public static readonly string[] All =
        {
            ExpiredToBeCheckedOut,
            ExpiredOnCheckOutProcess,
            ExpiredCheckedOut,
            ExpiredMissedTimelyCheckout
        };
    }

    public class VisaV06StateSnapshotService : IVisaV06StateSnapshotService
    {
        private const string OwnerType = "Visa";
        private const string RuleVersion = "v06-snapshot-1";
        private readonly IDbContextFactory<Visa2026EFCoreDbContext> _dbContextFactory;

        public VisaV06StateSnapshotService(IDbContextFactory<Visa2026EFCoreDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task RecomputeAsync(DateTime today)
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync();

            var expiredVisas = await db.Visas
                .Where(v => v.IsActive && !v.IsCancelled && !v.IsDeleted
                    && v.ExpirationDate.HasValue && v.ExpirationDate.Value < today
                    && v.Passport != null && v.Passport.Person != null)
                .Select(v => new
                {
                    VisaId = v.ID,
                    PersonId = v.Passport.Person.ID,
                    ExpirationDate = v.ExpirationDate.Value
                })
                .ToListAsync();

            var registeredVisaPairSet = await BuildRegisteredVisaPairSetAsync(db);
            var latestCheckoutByVisa = await BuildLatestCheckoutByVisaAsync(db);

            var matches = new List<StateMatch>();
            foreach (var visa in expiredVisas)
            {
                if (!registeredVisaPairSet.Contains($"{visa.PersonId:N}|{visa.VisaId:N}"))
                    continue;

                var visaKey = $"{visa.PersonId:N}|{visa.VisaId:N}";
                if (latestCheckoutByVisa.TryGetValue(visaKey, out var checkoutStateCode))
                {
                    if (checkoutStateCode == "PROCESS_ISSUED")
                    {
                        matches.Add(new StateMatch(
                            visa.VisaId,
                            VisaV06StateCodes.ExpiredCheckedOut,
                            "Archived",
                            $"Rule=V06C; CheckoutLatestState={checkoutStateCode}"));
                    }
                    else
                    {
                        matches.Add(new StateMatch(
                            visa.VisaId,
                            VisaV06StateCodes.ExpiredOnCheckOutProcess,
                            "Info",
                            $"Rule=V06B; CheckoutLatestState={checkoutStateCode ?? "NULL"}"));
                    }
                    continue;
                }

                var workingDays = CountWorkingDaysAfter(visa.ExpirationDate.Date, today.Date);
                if (workingDays > 3)
                {
                    matches.Add(new StateMatch(
                        visa.VisaId,
                        VisaV06StateCodes.ExpiredMissedTimelyCheckout,
                        "Critical",
                        $"Rule=V06D; NoCheckoutApp=true; WorkingDays={workingDays}"));
                }
                else if (workingDays <= 3)
                {
                    matches.Add(new StateMatch(
                        visa.VisaId,
                        VisaV06StateCodes.ExpiredToBeCheckedOut,
                        "Warning",
                        $"Rule=V06A; NoCheckoutApp=true; WorkingDays={workingDays}"));
                }
            }

            await SyncSnapshotsAsync(db, matches);
        }

        private static async Task<HashSet<string>> BuildRegisteredVisaPairSetAsync(Visa2026EFCoreDbContext db)
        {
            var registeredVisaPairs = await db.Set<Registration>()
                .Where(r => !r.IsDeleted && r.Person != null && r.CurrentVisa != null)
                .Select(r => new
                {
                    PersonId = r.Person.ID,
                    VisaId = r.CurrentVisa.ID
                })
                .Distinct()
                .ToListAsync();

            return registeredVisaPairs
                .Select(x => $"{x.PersonId:N}|{x.VisaId:N}")
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        private static async Task<Dictionary<string, string>> BuildLatestCheckoutByVisaAsync(Visa2026EFCoreDbContext db)
        {
            var checkoutRegs = await db.Set<Registration>()
                .Where(r => !r.IsDeleted && r.Person != null && r.Application != null && !r.Application.IsDeleted
                    && r.Application.ApplicationType != null
                    && r.Application.ApplicationType.Name == "App_Reg_Check_Out"
                    && r.CurrentVisa != null)
                .Select(r => new
                {
                    PersonId = r.Person.ID,
                    VisaId = r.CurrentVisa.ID,
                    ApplicationId = r.Application.ID,
                    ApplicationDate = r.Application.ApplicationDate
                })
                .ToListAsync();

            if (checkoutRegs.Count == 0)
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var checkoutAppIds = checkoutRegs.Select(x => x.ApplicationId).Distinct().ToList();
            var latestProgressByApp = await db.ApplicationProgresses
                .Where(ap => checkoutAppIds.Contains(ap.Application.ID))
                .Select(ap => new
                {
                    ApplicationId = ap.Application.ID,
                    ap.ID,
                    ap.Date,
                    StateCode = ap.State != null ? ap.State.Code : null
                })
                .ToListAsync();

            var latestStateCodeByApp = latestProgressByApp
                .GroupBy(x => x.ApplicationId)
                .Select(g => g.OrderByDescending(x => x.Date).ThenByDescending(x => x.ID).First())
                .ToDictionary(x => x.ApplicationId, x => x.StateCode);

            return checkoutRegs
                .GroupBy(x => new { x.PersonId, x.VisaId })
                .Select(g => g.OrderByDescending(x => x.ApplicationDate).ThenByDescending(x => x.ApplicationId).First())
                .ToDictionary(
                    x => $"{x.PersonId:N}|{x.VisaId:N}",
                    x => latestStateCodeByApp.TryGetValue(x.ApplicationId, out var code) ? code : null,
                    StringComparer.OrdinalIgnoreCase);
        }

        private static async Task SyncSnapshotsAsync(Visa2026EFCoreDbContext db, IReadOnlyCollection<StateMatch> matches)
        {
            var now = DateTime.UtcNow;
            var matchMap = matches
                .GroupBy(m => new { m.OwnerId, m.StateCode })
                .Select(g => g.First())
                .ToDictionary(m => $"{m.OwnerId:N}|{m.StateCode}", StringComparer.OrdinalIgnoreCase);

            var existing = await db.BoStateSnapshots
                .Where(s => s.OwnerType == OwnerType && VisaV06StateCodes.All.Contains(s.StateCode))
                .ToListAsync();

            foreach (var row in existing)
            {
                var key = $"{row.OwnerId:N}|{row.StateCode}";
                if (matchMap.TryGetValue(key, out var match))
                {
                    row.IsActive = true;
                    row.Severity = match.Severity;
                    row.Reason = match.Reason;
                    row.EvaluatedAtUtc = now;
                    row.RuleVersion = RuleVersion;
                    row.UpdatedOnUtc = now;
                }
                else
                {
                    row.IsActive = false;
                    row.EvaluatedAtUtc = now;
                    row.UpdatedOnUtc = now;
                }
            }

            var existingKeys = existing
                .Select(x => $"{x.OwnerId:N}|{x.StateCode}")
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var match in matchMap.Values)
            {
                var key = $"{match.OwnerId:N}|{match.StateCode}";
                if (existingKeys.Contains(key))
                    continue;

                db.BoStateSnapshots.Add(new BoStateSnapshot
                {
                    OwnerType = OwnerType,
                    OwnerId = match.OwnerId,
                    StateCode = match.StateCode,
                    Severity = match.Severity,
                    IsActive = true,
                    Reason = match.Reason,
                    RuleVersion = RuleVersion,
                    EvaluatedAtUtc = now,
                    CreatedOnUtc = now,
                    UpdatedOnUtc = now
                });
            }

            await db.SaveChangesAsync();
        }

        private static int CountWorkingDaysAfter(DateTime fromDate, DateTime toDate)
        {
            if (toDate <= fromDate) return 0;
            var count = 0;
            for (var d = fromDate.AddDays(1); d <= toDate; d = d.AddDays(1))
            {
                if (d.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday)
                    count++;
            }
            return count;
        }

        private sealed record StateMatch(Guid OwnerId, string StateCode, string Severity, string Reason);
    }
}
