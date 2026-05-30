using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.Persistent.BaseImpl.EF;

namespace Visa2026.Module.BusinessObjects
{
    /// <summary>
    /// Save-time maintenance of sibling <see cref="ICurrentPersonItem.IsActive"/> flags.
    /// </summary>
    public static class CurrentPersonItemSync
    {
        public static void OnCreated(ICurrentPersonItem item) => item.IsActive = true;

        public static void ApplySoftDeleteDeactivate(object item)
        {
            if (item is ISoftDelete softDeletable && softDeletable.IsDeleted && item is ICurrentPersonItem current && current.IsActive)
                current.IsActive = false;
        }

        public static void ApplyOnSaving<TItem>(
            TItem item,
            Func<TItem, Person> getPerson,
            Func<Person, IList<TItem>> getSiblings,
            Func<TItem, DateTime?> getSortDate = null,
            Action<TItem> beforeSync = null)
            where TItem : class, ICurrentPersonItem
        {
            ApplySoftDeleteDeactivate(item);
            beforeSync?.Invoke(item);

            var person = getPerson(item);
            if (person == null)
                return;

            if (item.IsActive)
            {
                var siblings = getSiblings(person);
                if (siblings != null)
                {
                    foreach (var sibling in siblings.ToList())
                    {
                        if (ReferenceEquals(sibling, item))
                            continue;
                        if (sibling is ISoftDelete softDeleted && softDeleted.IsDeleted)
                            continue;
                        if (sibling.IsActive)
                            sibling.IsActive = false;
                    }
                }

                return;
            }

            var hasOtherActive = getSiblings(person)?
                .Where(s => !ReferenceEquals(s, item))
                .Any(s => s is not ISoftDelete sd || !sd.IsDeleted && s.IsActive) == true;
            if (hasOtherActive)
                return;

            var nextActive = getSiblings(person)?
                .Where(s => !ReferenceEquals(s, item))
                .Where(s => s is not ISoftDelete sd || !sd.IsDeleted)
                .Select(s => new { Item = s, SortDate = getSortDate?.Invoke(s) })
                .Where(x => x.SortDate.HasValue)
                .OrderByDescending(x => x.SortDate)
                .Select(x => x.Item)
                .FirstOrDefault();

            if (nextActive != null)
                nextActive.IsActive = true;
        }

        /// <summary>
        /// Date-effective visa selection (person + per-passport) and sibling <see cref="Visa.IsActive"/> flags.
        /// </summary>
        public static void UpdateVisaCurrentState(Visa visa)
        {
            ApplySoftDeleteDeactivate(visa);

            var parent = visa.Passport?.Person;
            if (parent == null)
                return;

            var asOf = DateTime.Today;
            var siblings = parent.Passports?.SelectMany(p => p.Visas).ToList() ?? new List<Visa>();
            var selectable = siblings.Where(VisaIsSelectable).ToList();

            var currentForPerson = selectable
                .Where(v => VisaIsEffectiveOn(v, asOf))
                .OrderByDescending(v => v.StartDate.Date)
                .ThenByDescending(v => v.IssueDate.Date)
                .FirstOrDefault();

            foreach (var v in selectable)
                v.IsActive = ReferenceEquals(v, currentForPerson);

            if (parent.Passports != null)
            {
                foreach (var passport in parent.Passports.Where(p => p != null && !p.IsDeleted))
                {
                    var pSelectable = passport.Visas?
                        .Where(VisaIsSelectable)
                        .ToList() ?? new List<Visa>();

                    var currentForPassport = pSelectable
                        .Where(v => VisaIsEffectiveOn(v, asOf))
                        .OrderByDescending(v => v.StartDate.Date)
                        .ThenByDescending(v => v.IssueDate.Date)
                        .FirstOrDefault();

                    passport.CurrentVisa = currentForPassport;
                }
            }
        }

        private static bool VisaIsSelectable(Visa v) =>
            v != null && !v.IsDeleted && !v.IsCancelled && v.StartDate != default;

        private static bool VisaIsEffectiveOn(Visa v, DateTime asOfDate) =>
            VisaIsSelectable(v) && v.StartDate.Date <= asOfDate.Date;
    }
}
