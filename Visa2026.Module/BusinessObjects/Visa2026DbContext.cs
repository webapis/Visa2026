﻿﻿﻿﻿﻿using DevExpress.ExpressApp.Design;
using DevExpress.ExpressApp.EFCore.DesignTime;
using DevExpress.ExpressApp.EFCore.Updating;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.BaseImpl.EF.PermissionPolicy;
using DevExpress.Persistent.BaseImpl.EF.StateMachine;
using DevExpress.Persistent.BaseImpl.EFCore.AuditTrail;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Visa2026.Module.BusinessObjects
{
    [TypesInfoInitializer(typeof(DbContextTypesInfoInitializer<Visa2026EFCoreDbContext>))]
    public class Visa2026EFCoreDbContext : DbContext
    {
        public Visa2026EFCoreDbContext(DbContextOptions<Visa2026EFCoreDbContext> options) : base(options)
        {
        }
        //public DbSet<ModuleInfo> ModulesInfo { get; set; }
        public DbSet<ModelDifference> ModelDifferences { get; set; }
        public DbSet<ModelDifferenceAspect> ModelDifferenceAspects { get; set; }
        public DbSet<PermissionPolicyRole> Roles { get; set; }
        public DbSet<Visa2026.Module.BusinessObjects.ApplicationUser> Users { get; set; }
        public DbSet<Visa2026.Module.BusinessObjects.ApplicationUserLoginInfo> UserLoginsInfo { get; set; }
        public DbSet<FileData> FileData { get; set; }
        public DbSet<ReportDataV2> ReportDataV2 { get; set; }
        public DbSet<StateMachine> StateMachines { get; set; }
        public DbSet<StateMachineState> StateMachineStates { get; set; }
        public DbSet<StateMachineTransition> StateMachineTransitions { get; set; }
        public DbSet<StateMachineAppearance> StateMachineAppearances { get; set; }
        public DbSet<DashboardData> DashboardData { get; set; }
        public DbSet<AuditDataItemPersistent> AuditData { get; set; }
        public DbSet<AuditEFCoreWeakReference> AuditEFCoreWeakReferences { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<HCategory> HCategories { get; set; }
        public DbSet<RichTextMailMergeData> RichTextMailMergeData { get; set; }

        public DbSet<Country> Countries { get; set; }
        public DbSet<Region> Regions { get; set; }
        public DbSet<City> Cities { get; set; }
        public DbSet<WorkPermitLocation> WorkPermitLocations { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Position> Positions { get; set; }
        public DbSet<Gender> Genders { get; set; }
        public DbSet<MaritalStatus> MaritalStatuses { get; set; }
        public DbSet<Relationship> Relationships { get; set; }
        public DbSet<Person> People { get; set; }
        public DbSet<Visa> Visas { get; set; }
        public DbSet<VisaImage> VisaImages { get; set; }
        public DbSet<VisaType> VisaTypes { get; set; }
        public DbSet<WorkPermitItem> WorkPermitItems { get; set; }
        public DbSet<FamilyMemberImage> FamilyMemberImages { get; set; }
        public DbSet<EmployeePositionHistory> EmployeePositionHistories { get; set; }
        public DbSet<Education> Educations { get; set; }
        public DbSet<MedicalRecord> MedicalRecords { get; set; }
        public DbSet<EducationLevel> EducationLevels { get; set; }
        public DbSet<EducationInstitution> EducationInstitutions { get; set; }
        public DbSet<Specialty> Specialties { get; set; }
        public DbSet<AddressOfResidence> AddressesOfResidence { get; set; }
        public DbSet<AddressOfResidenceImage> AddressOfResidenceImages { get; set; }
        public DbSet<AddressOfResidenceDocument> AddressOfResidenceDocuments { get; set; }
        public DbSet<Passport> Passports { get; set; }
        public DbSet<PassportImage> PassportImages { get; set; }
        public DbSet<PassportType> PassportTypes { get; set; }
        public DbSet<PersonDocument> PersonDocuments { get; set; }
        public DbSet<Lodging> Lodgings { get; set; }
        public DbSet<LodgingDocument> LodgingDocuments { get; set; }
        public DbSet<LodgingImage> LodgingImages { get; set; }
        public DbSet<EducationImage> EducationImages { get; set; }
        public DbSet<InvitationImage> InvitationImages { get; set; }
        public DbSet<InvitationDocument> InvitationDocuments { get; set; }
        public DbSet<MedicalRecordDocument> MedicalRecordDocuments { get; set; }
        public DbSet<ApplicationItem> ApplicationItems { get; set; }
        public DbSet<Ministry> Ministries { get; set; }
        public DbSet<ProjectContract> ProjectContracts { get; set; }
        public DbSet<ProjectContractImage> ProjectContractImages { get; set; }
        public DbSet<ProjectContractDocument> ProjectContractDocuments { get; set; }
        public DbSet<VisaPeriod> VisaPeriods { get; set; }
        public DbSet<VisaCategory> VisaCategories { get; set; }
        public DbSet<Invitation> Invitations { get; set; }
        public DbSet<InvitationItem> InvitationItems { get; set; }
        public DbSet<Rejection> Rejections { get; set; }
        public DbSet<RejectionItem> RejectionItems { get; set; }
        public DbSet<RejectionImage> RejectionImages { get; set; }
        public DbSet<RejectionDocument> RejectionDocuments { get; set; }
        public DbSet<BorderZone> BorderZones { get; set; }
        public DbSet<CheckPoint> CheckPoints { get; set; }
        public DbSet<VisaIssuedPlace> VisaIssuedPlaces { get; set; }
        public DbSet<PurposeOfTravel> PurposeOfTravels { get; set; }
        public DbSet<WorkPermit> WorkPermits { get; set; }
        public DbSet<WorkPermitDocument> WorkPermitDocuments { get; set; }
        public DbSet<WorkPermitImage> WorkPermitImages { get; set; }
        public DbSet<Urgency> Urgencies { get; set; }
        public DbSet<Subcontractor> Subcontractors { get; set; }
        public DbSet<Application> Applications { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<CompanyImage> CompanyImages { get; set; }
        public DbSet<CompanyDocument> CompanyDocuments { get; set; }
        public DbSet<CompanyHead> CompanyHeads { get; set; }
        public DbSet<CompanyHeadImage> CompanyHeadImages { get; set; }
        public DbSet<CompanyHeadDocument> CompanyHeadDocuments { get; set; }
        public DbSet<Representative> Representatives { get; set; }
        public DbSet<RepresentativeImage> RepresentativeImages { get; set; }
        public DbSet<RepresentativeDocument> RepresentativeDocuments { get; set; }
        public DbSet<LocalEmployee> LocalEmployees { get; set; }
        public DbSet<MigrationService> MigrationServices { get; set; }
        public DbSet<EmployeeContract> EmployeeContracts { get; set; }
        public DbSet<EmployeeContractImage> EmployeeContractImages { get; set; }
        public DbSet<EmployeeContractDocument> EmployeeContractDocuments { get; set; }
        public DbSet<ContractTemplate> ContractTemplates { get; set; }
        public DbSet<ApplicationType> ApplicationTypes { get; set; }
        public DbSet<OrganizationType> OrganizationTypes { get; set; }
        public DbSet<ApplicationState> ApplicationStates { get; set; }
        public DbSet<ApplicationProgress> ApplicationProgresses { get; set; }
        public DbSet<ApplicationLocation> ApplicationLocations { get; set; }
        public DbSet<ValidityDuration> ValidityDurations { get; set; }
        public DbSet<TravelHistory> TravelHistories { get; set; }
        public DbSet<SystemSettings> SystemSettings { get; set; }
        public DbSet<SyncRule> SyncRules { get; set; }
        public DbSet<SyncRuleLog> SyncRuleLogs { get; set; }
        public DbSet<PdfFormMapping> PdfFormMapping { get; set; }
        public DbSet<ReportVisibility> ReportVisibilities { get; set; }
       public DbSet<MailMergeVisibility> MailMergeVisibility { get; set; }
        public DbSet<StateChangeRule> StateChangeRules { get; set; }
        public DbSet<StateChangeLog> StateChangeLogs { get; set; }
        public DbSet<VisaExtensionTracking> VisaExtensionTrackings { get; set; }
        public DbSet<VisaExtensionStatus> VisaExtensionStatuses { get; set; }
        public DbSet<WorkPermitExtensionTracking> WorkPermitExtensionTrackings { get; set; }
        public DbSet<WorkPermitExtensionStatus> WorkPermitExtensionStatuses { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.UseDeferredDeletion(this);
            modelBuilder.UseOptimisticLock();
            modelBuilder.SetOneToManyAssociationDeleteBehavior(DeleteBehavior.SetNull, DeleteBehavior.Cascade);
            modelBuilder.HasChangeTrackingStrategy(ChangeTrackingStrategy.ChangingAndChangedNotificationsWithOriginalValues);
            modelBuilder.Entity<Application>(b => {
                b.HasIndex(a => new { a.AppNumberPrefix, a.ApplicationNumber, a.Year }).IsUnique();
            });
            modelBuilder.Entity<Visa2026.Module.BusinessObjects.ApplicationUserLoginInfo>(b =>
            {
                b.HasIndex(nameof(DevExpress.ExpressApp.Security.ISecurityUserLoginInfo.LoginProviderName), nameof(DevExpress.ExpressApp.Security.ISecurityUserLoginInfo.ProviderUserKey)).IsUnique();
            });
            modelBuilder.Entity<AuditEFCoreWeakReference>()
                .HasMany(p => p.AuditItems)
                .WithOne(p => p.AuditedObject);
            modelBuilder.Entity<AuditEFCoreWeakReference>()
                .HasMany(p => p.OldItems)
                .WithOne(p => p.OldObject);
            modelBuilder.Entity<AuditEFCoreWeakReference>()
                .HasMany(p => p.NewItems)
                .WithOne(p => p.NewObject);
            modelBuilder.Entity<AuditEFCoreWeakReference>()
                .HasMany(p => p.UserItems)
                .WithOne(p => p.UserObject);
            modelBuilder.Entity<StateMachine>()
                .HasMany(t => t.States)
                .WithOne(t => t.StateMachine)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<ModelDifference>()
                .HasMany(t => t.Aspects)
                .WithOne(t => t.Owner)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<VisaExtensionTracking>()
                .ToView("View_VisaExtensionTracking")
                .HasKey(t => new { t.ApplicationItemID, t.ApplicationProgressID });

            modelBuilder.Entity<VisaExtensionStatus>()
                .ToView("View_VisaExtensionStatus")
                .HasKey(t => t.ID);

            modelBuilder.Entity<WorkPermitExtensionTracking>()
                .ToView("View_WorkPermitExtensionTracking")
                .HasKey(t => new { t.ApplicationItemID, t.ApplicationProgressID });

            modelBuilder.Entity<WorkPermitExtensionStatus>()
                .ToView("View_WorkPermitExtensionStatus")
                .HasKey(t => t.ID);
        }
    }

    public class Visa2026AuditingDbContext : DbContext
    {
        public Visa2026AuditingDbContext(DbContextOptions<Visa2026AuditingDbContext> options) : base(options)
        {
        }
        public DbSet<AuditDataItemPersistent> AuditData { get; set; }
        public DbSet<AuditEFCoreWeakReference> AuditEFCoreWeakReferences { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.UseDeferredDeletion(this);
            modelBuilder.HasChangeTrackingStrategy(ChangeTrackingStrategy.ChangingAndChangedNotificationsWithOriginalValues);
            modelBuilder.Entity<AuditEFCoreWeakReference>()
                .HasMany(p => p.AuditItems)
                .WithOne(p => p.AuditedObject);
            modelBuilder.Entity<AuditEFCoreWeakReference>()
                .HasMany(p => p.OldItems)
                .WithOne(p => p.OldObject);
            modelBuilder.Entity<AuditEFCoreWeakReference>()
                .HasMany(p => p.NewItems)
                .WithOne(p => p.NewObject);
            modelBuilder.Entity<AuditEFCoreWeakReference>()
                .HasMany(p => p.UserItems)
                .WithOne(p => p.UserObject);
        }
    }
}
