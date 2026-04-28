using DevExpress.ExpressApp.Design;
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
            Database.SetCommandTimeout(180); // 3-minute timeout; the default 30s is too short for complex prefetch queries
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
        public DbSet<MovementPermitLocation> MovementPermitLocations { get; set; }
        public DbSet<BorderZoneLocation> BorderZoneLocations { get; set; }
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
        public DbSet<VisaExtensionTracking> VisaExtensionTracking { get; set; }
        public DbSet<VisaExtensionStatus> VisaExtensionStatus { get; set; }
        public DbSet<WorkPermitExtensionTracking> WorkPermitExtensionTracking { get; set; }
        public DbSet<WorkPermitExtensionStatus> WorkPermitExtensionStatus { get; set; }
        public DbSet<VisaTransferStatus> VisaTransferStatus { get; set; }
        public DbSet<VisaCancelExtStatus> VisaCancelExtStatus { get; set; }
        public DbSet<VisaCancellationStatus> VisaCancellationStatus { get; set; }
        public DbSet<TravelHistory> TravelHistories { get; set; }
        public DbSet<ExternalArrival> ExternalArrivals { get; set; }
        public DbSet<ExternalDeparture> ExternalDepartures { get; set; }
        public DbSet<InternalArrival> InternalArrivals { get; set; }
        public DbSet<InternalDeparture> InternalDepartures { get; set; }
        public DbSet<SystemSettings> SystemSettings { get; set; }
        public DbSet<SyncRule> SyncRules { get; set; }
        public DbSet<SyncRuleLog> SyncRuleLogs { get; set; }
        public DbSet<PdfFormMapping> PdfFormMapping { get; set; }
        public DbSet<ReportVisibility> ReportVisibilities { get; set; }
        public DbSet<MailMergeVisibility> MailMergeVisibility { get; set; }
        public DbSet<StateChangeRule> StateChangeRules { get; set; }
        public DbSet<StateChangeLog> StateChangeLogs { get; set; }
        public DbSet<BoStateSnapshot> BoStateSnapshots { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.UseDeferredDeletion(this);
            modelBuilder.UseOptimisticLock();
            modelBuilder.SetOneToManyAssociationDeleteBehavior(DeleteBehavior.SetNull, DeleteBehavior.Cascade);
            modelBuilder.HasChangeTrackingStrategy(ChangeTrackingStrategy.ChangingAndChangedNotificationsWithOriginalValues);

            modelBuilder.Entity<TravelHistory>()
                .HasDiscriminator<string>("Discriminator")
                .HasValue<ExternalArrival>(nameof(ExternalArrival))
                .HasValue<ExternalDeparture>(nameof(ExternalDeparture))
                .HasValue<InternalArrival>(nameof(InternalArrival))
                .HasValue<InternalDeparture>(nameof(InternalDeparture));

            modelBuilder.Entity<VisaExtensionTracking>(b => {
                b.HasKey(t => t.ID);
                b.ToView("View_VisaExtensionTracking");
            });

            modelBuilder.Entity<VisaExtensionStatus>(b => {
                b.HasKey(t => t.ID);
                b.ToView("View_VisaExtensionStatus");
                b.HasOne(t => t.IssuedVisa).WithMany().HasForeignKey(t => t.IssuedVisaID).OnDelete(DeleteBehavior.NoAction);
                b.HasOne(t => t.RejectionItem).WithMany().HasForeignKey(t => t.RejectionItemID).OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<WorkPermitExtensionTracking>(b => {
                b.HasKey(t => t.ID);
                b.ToView("View_WorkPermitExtensionTracking");
            });

            modelBuilder.Entity<WorkPermitExtensionStatus>(b => {
                b.HasKey(t => t.ID);
                b.ToView("View_WorkPermitExtensionStatus");
            });

            modelBuilder.Entity<VisaTransferStatus>(b => {
                b.HasKey(t => t.ID);
                b.ToView("View_VisaTransferStatus");
                b.HasOne(t => t.IssuedVisa).WithMany().HasForeignKey(t => t.IssuedVisaID).OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<VisaCancelExtStatus>(b => {
                b.HasKey(c => c.ID);
                b.ToView("View_VisaCancelExtStatus");
                b.HasOne(c => c.Visa).WithMany().HasForeignKey(c => c.VisaID).OnDelete(DeleteBehavior.NoAction);
                b.HasOne(c => c.ExtCurrentState).WithMany().HasForeignKey(c => c.ExtCurrentStateID).OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<VisaCancellationStatus>(b => {
                b.HasKey(c => c.ID);
                b.ToView("View_VisaCancellationStatus");
                b.HasOne(c => c.Application).WithMany().HasForeignKey(c => c.ApplicationID).OnDelete(DeleteBehavior.NoAction);
                b.HasOne(c => c.Visa).WithMany().HasForeignKey(c => c.VisaID).OnDelete(DeleteBehavior.NoAction);
                b.HasOne(c => c.Person).WithMany().HasForeignKey(c => c.PersonID).OnDelete(DeleteBehavior.NoAction);
                b.HasOne(c => c.Passport).WithMany().HasForeignKey(c => c.PassportID).OnDelete(DeleteBehavior.NoAction);
                b.HasOne(c => c.CurrentState).WithMany().HasForeignKey(c => c.CurrentStateID).OnDelete(DeleteBehavior.NoAction);
                b.HasOne(c => c.CheckOutState).WithMany().HasForeignKey(c => c.CheckOutStateID).OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<Application>(b => {
                b.HasIndex(a => new { a.AppNumberPrefix, a.ApplicationNumber, a.Year, a.Month })
                 .IsUnique()
                 .HasFilter("[IsManualEntry] = 0");
            });

            modelBuilder.Entity<BoStateSnapshot>(b => {
                b.HasIndex(s => new { s.OwnerType, s.OwnerId, s.StateCode }).IsUnique();
                b.HasIndex(s => new { s.OwnerType, s.StateCode, s.IsActive });
                b.Property(s => s.OwnerType).HasMaxLength(128);
                b.Property(s => s.StateCode).HasMaxLength(128);
                b.Property(s => s.Severity).HasMaxLength(64);
                b.Property(s => s.RuleVersion).HasMaxLength(64);
            });

            // Break multiple cascade paths and cycles for SQL Server
            modelBuilder.Entity<ApplicationItem>(b => {
                b.HasOne(ai => ai.Person).WithMany(p => p.ApplicationItems).OnDelete(DeleteBehavior.NoAction);
                b.HasOne(ai => ai.CurrentPassport).WithMany().OnDelete(DeleteBehavior.NoAction);
                b.HasOne(ai => ai.PreviousPassport).WithMany().OnDelete(DeleteBehavior.NoAction);
                b.HasOne(ai => ai.CurrentVisa).WithMany(v => v.AssociatedApplicationItems).HasForeignKey(ai => ai.CurrentVisaId).OnDelete(DeleteBehavior.NoAction);
                b.HasOne(ai => ai.CurrentWorkPermitItem).WithMany().OnDelete(DeleteBehavior.NoAction);
                b.HasOne(ai => ai.SecondWorkPermitItem).WithMany().OnDelete(DeleteBehavior.NoAction);
                b.HasOne(ai => ai.CurrentInvitationItem).WithMany().OnDelete(DeleteBehavior.NoAction);
                b.HasOne(ai => ai.CurrentAddressOfResidence).WithMany().OnDelete(DeleteBehavior.NoAction);
                b.HasOne(ai => ai.CurrentRegistration).WithMany().OnDelete(DeleteBehavior.NoAction);
                b.HasOne(ai => ai.CurrentEmployeeContract).WithMany().OnDelete(DeleteBehavior.NoAction);
                b.HasOne(ai => ai.CurrentMedicalRecord).WithMany().OnDelete(DeleteBehavior.NoAction);
                b.HasOne(ai => ai.CurrentEducation).WithMany().OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<BusinessTrip>(b => {
                b.HasOne(bt => bt.CurrentAddressOfResidence).WithMany().OnDelete(DeleteBehavior.NoAction);
                b.HasOne(bt => bt.CurrentPassport).WithMany().OnDelete(DeleteBehavior.NoAction);
                b.HasOne(bt => bt.CurrentVisa).WithMany().OnDelete(DeleteBehavior.NoAction);
                b.HasOne(bt => bt.CurrentPositionHistory).WithMany().OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<WorkPermitItem>(b => {
                b.HasOne(wpi => wpi.Person).WithMany(p => p.WorkPermitItems).OnDelete(DeleteBehavior.NoAction);
                b.HasOne(wpi => wpi.Passport).WithMany().OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<Visa>(b => {
                b.HasOne(v => v.Passport).WithMany(p => p.Visas).OnDelete(DeleteBehavior.NoAction);
                b.HasOne(v => v.IssuingApplicationItem).WithMany().OnDelete(DeleteBehavior.NoAction).IsRequired(false);
                b.Metadata.UseSqlOutputClause(false);
                b.Property(v => v.ExtensionRequired).HasDefaultValue(true);
                b.Property(v => v.HistoricalImport).HasDefaultValue(false);
            });

            modelBuilder.Entity<Passport>().HasOne(p => p.Person).WithMany(p => p.Passports).OnDelete(DeleteBehavior.NoAction);

            // FIX: Person.ApplicationItems is a virtual collection navigation whose backing field
            // cannot be discovered by the lazy-loading proxy (it is an auto-property or an inline-
            // initialised collection). Configuring PropertyAccessMode.Property tells EF Core to
            // call the getter/setter directly instead of looking for a private backing field,
            // which resolves the startup exception:
            //   "No backing field was found for property 'Person.ApplicationItems'.
            //    Lazy-loaded navigations must have backing fields."
            modelBuilder.Entity<Person>()
                .Navigation(p => p.ApplicationItems)
                .UsePropertyAccessMode(PropertyAccessMode.Property);

            modelBuilder.Entity<Person>()
                .Navigation(p => p.WorkPermitItems)
                .UsePropertyAccessMode(PropertyAccessMode.Property);

            modelBuilder.Entity<Person>()
                .Navigation(p => p.Passports)
                .UsePropertyAccessMode(PropertyAccessMode.Property);

            modelBuilder.Entity<Person>()
                .Navigation(p => p.TravelHistories)
                .UsePropertyAccessMode(PropertyAccessMode.Property);

            modelBuilder.Entity<Person>()
                .Navigation(p => p.Registrations)
                .UsePropertyAccessMode(PropertyAccessMode.Property);

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