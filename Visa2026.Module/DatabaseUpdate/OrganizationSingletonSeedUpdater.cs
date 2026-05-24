using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;
using Microsoft.EntityFrameworkCore;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.DatabaseUpdate
{
    /// <summary>
    /// Seeds organization singletons and application-number fields on <see cref="SystemSettings"/>
    /// from the legacy default <see cref="Company"/> row (Phase 1 of organization refactor).
    /// </summary>
    public class OrganizationSingletonSeedUpdater : ModuleUpdater
    {
        public OrganizationSingletonSeedUpdater(IObjectSpace objectSpace, Version currentDBVersion)
            : base(objectSpace, currentDBVersion)
        {
        }

        public override void UpdateDatabaseAfterUpdateSchema()
        {
            base.UpdateDatabaseAfterUpdateSchema();

            var company = ObjectSpace.GetObjectsQuery<Company>()
                .FirstOrDefault(c => c.IsDefault)
                ?? ObjectSpace.GetObjectsQuery<Company>().FirstOrDefault();

            SeedCompanyProfile(company);
            SeedAuthorizedSignatory(company);
            SeedAuthorizedRepresentative(company);
            SeedApplicationNumbering(company);

            ObjectSpace.CommitChanges();
        }

        private void SeedCompanyProfile(Company? company)
        {
            var profile = CompanyProfile.TryGetInstance(ObjectSpace);
            if (profile == null)
                profile = ObjectSpace.CreateObject<CompanyProfile>();

            if (company == null || !string.IsNullOrWhiteSpace(profile.Name))
                return;

            profile.Name = company.Name;
            profile.Code = company.Code;
            profile.Address = company.Address;
            profile.PhoneNumber = company.PhoneNumber;
            profile.Email = company.Email;
            profile.TaxInformation = company.TaxInformation;
        }

        private void SeedAuthorizedSignatory(Company? company)
        {
            var signatory = AuthorizedSignatory.TryGetInstance(ObjectSpace);
            if (signatory == null)
                signatory = ObjectSpace.CreateObject<AuthorizedSignatory>();

            if (!string.IsNullOrWhiteSpace(signatory.FullName))
                return;

            var head = ResolveCompanyHead(company);
            if (head == null)
                return;

            signatory.FullName = head.FullName;
            signatory.PositionTitleTm = head.Position?.NameTm ?? string.Empty;
            CopyPassport(signatory, ResolvePassport(head));
        }

        private void SeedAuthorizedRepresentative(Company? company)
        {
            var representative = AuthorizedRepresentative.TryGetInstance(ObjectSpace);
            if (representative == null)
                representative = ObjectSpace.CreateObject<AuthorizedRepresentative>();

            if (!string.IsNullOrWhiteSpace(representative.FullName))
                return;

            var rep = ResolveRepresentative(company);
            if (rep == null)
                return;

            representative.FullName = rep.FullName;
            representative.PositionTitleTm = rep.LocalEmployee?.Position?.NameTm
                ?? rep.Employee?.CurrentPositionHistory?.Position?.NameTm
                ?? string.Empty;
            CopyPassport(representative, ResolvePassport(rep));
        }

        private void SeedApplicationNumbering(Company? company)
        {
            var settings = SystemSettings.GetOrCreateInstance(ObjectSpace);
            if (company == null)
                return;

            if (string.IsNullOrWhiteSpace(settings.AppNumberPrefix))
                settings.AppNumberPrefix = company.AppNumberPrefix;
            if (string.IsNullOrWhiteSpace(settings.AppNumberFormat))
                settings.AppNumberFormat = company.AppNumberFormat;
            if (settings.ApplicationNumberSeed == 0 && company.ApplicationNumberSeed != 0)
                settings.ApplicationNumberSeed = company.ApplicationNumberSeed;
            if (settings.ApplicationNumberPadding <= 0)
                settings.ApplicationNumberPadding = company.ApplicationNumberPadding > 0
                    ? company.ApplicationNumberPadding
                    : SystemSettings.DefaultApplicationNumberPadding;
        }

        private CompanyHead? ResolveCompanyHead(Company? company)
        {
            if (company?.CurrentAuthorizedSignatory != null)
                return LoadCompanyHead(company.CurrentAuthorizedSignatory.ID);

            if (company == null)
                return null;

            var headId = ObjectSpace.GetObjectsQuery<CompanyHead>()
                .Where(h => h.Company != null && h.Company.ID == company.ID && h.IsActive)
                .OrderByDescending(h => h.ID)
                .Select(h => h.ID)
                .FirstOrDefault();

            return headId == default ? null : LoadCompanyHead(headId);
        }

        private Representative? ResolveRepresentative(Company? company)
        {
            if (company?.CurrentRepresentative != null)
                return LoadRepresentative(company.CurrentRepresentative.ID);

            if (company == null)
                return null;

            var repId = ObjectSpace.GetObjectsQuery<Representative>()
                .Where(r => r.Company != null && r.Company.ID == company.ID && r.IsActive)
                .OrderByDescending(r => r.ID)
                .Select(r => r.ID)
                .FirstOrDefault();

            return repId == default ? null : LoadRepresentative(repId);
        }

        private CompanyHead? LoadCompanyHead(Guid id) =>
            ObjectSpace.GetObjectsQuery<CompanyHead>()
                .Include(h => h.Position)
                .Include(h => h.LocalEmployee).ThenInclude(e => e!.Position)
                .Include(h => h.Employee).ThenInclude(e => e!.CurrentPassport)
                .FirstOrDefault(h => h.ID == id);

        private Representative? LoadRepresentative(Guid id) =>
            ObjectSpace.GetObjectsQuery<Representative>()
                .Include(r => r.LocalEmployee).ThenInclude(e => e!.Position)
                .Include(r => r.Employee).ThenInclude(e => e!.CurrentPassport)
                .Include(r => r.Employee).ThenInclude(e => e!.CurrentPositionHistory).ThenInclude(h => h!.Position)
                .FirstOrDefault(r => r.ID == id);

        private static Passport? ResolvePassport(CompanyHead head)
        {
            if (head.IsLocalEmployee)
                return null;
            return head.Employee?.CurrentPassport;
        }

        private static Passport? ResolvePassport(Representative rep)
        {
            if (rep.IsLocalEmployee)
                return null;
            return rep.Employee?.CurrentPassport;
        }

        private static void CopyPassport(AuthorizedSignatory target, Passport? passport)
        {
            if (passport == null)
                return;
            target.PassportNumber = passport.PassportNumber;
            target.PassportAuthority = passport.Authority;
            target.PassportIssueDate = passport.IssueDate == default ? null : passport.IssueDate;
        }

        private static void CopyPassport(AuthorizedRepresentative target, Passport? passport)
        {
            if (passport == null)
                return;
            target.PassportNumber = passport.PassportNumber;
            target.PassportAuthority = passport.Authority;
            target.PassportIssueDate = passport.IssueDate == default ? null : passport.IssueDate;
        }
    }
}
