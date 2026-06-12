using DevExpress.ExpressApp.Design;
using DevExpress.ExpressApp.EFCore.DesignTime;
using DevExpress.ExpressApp.EFCore.Updating;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.BaseImpl.EF.PermissionPolicy;
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
        public DbSet<AuditDataItemPersistent> AuditData { get; set; }
        public DbSet<AuditEFCoreWeakReference> AuditEFCoreWeakReferences { get; set; }
        public DbSet<HCategory> HCategories { get; set; }
        // Retained for existing DB rows; not exported in XAF while MailMergeFeature.Enabled is false.
        public DbSet<RichTextMailMergeData> RichTextMailMergeData { get; set; }

        public DbSet<Country> Countries { get; set; }
        public DbSet<Region> Regions { get; set; }
        public DbSet<City> Cities { get; set; }
        public DbSet<WorkPermitLocation> WorkPermitLocations { get; set; }
        public DbSet<MovementPermitLocation> MovementPermitLocations { get; set; }
        public DbSet<BorderZoneLocation> BorderZoneLocations { get; set; }
        public DbSet<BorderZoneName> BorderZoneNames { get; set; }
        public DbSet<WorkPermittedLocationName> WorkPermittedLocationNames { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Position> Positions { get; set; }
        public DbSet<ActualPosition> ActualPositions { get; set; }
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
        public DbSet<PassportDocument> PassportDocuments { get; set; }
        public DbSet<PassportImage> PassportImages { get; set; }
        public DbSet<PassportType> PassportTypes { get; set; }
        public DbSet<PersonDocument> PersonDocuments { get; set; }
        public DbSet<PersonFamilyRelationDocument> PersonFamilyRelationDocuments { get; set; }
        public DbSet<Lodging> Lodgings { get; set; }
        public DbSet<LodgingDocument> LodgingDocuments { get; set; }
        public DbSet<LodgingImage> LodgingImages { get; set; }
        public DbSet<EducationImage> EducationImages { get; set; }
        public DbSet<InvitationImage> InvitationImages { get; set; }
        public DbSet<InvitationDocument> InvitationDocuments { get; set; }
        public DbSet<MedicalRecordDocument> MedicalRecordDocuments { get; set; }
        public DbSet<ApplicationItem> ApplicationItems { get; set; }
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
        public DbSet<MigrationService> MigrationServices { get; set; }
        public DbSet<EmployeeSalary> EmployeeSalaries { get; set; }
        public DbSet<WorkDuty> WorkDuties { get; set; }
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
        public DbSet<ExpirationAlertRule> ExpirationAlertRules { get; set; }
        public DbSet<CompanyProfile> CompanyProfiles { get; set; }
        public DbSet<ApplicationNumberingProfile> ApplicationNumberingProfiles { get; set; }
        public DbSet<AuthorizedSignatory> AuthorizedSignatories { get; set; }
        public DbSet<AuthorizedRepresentative> AuthorizedRepresentatives { get; set; }
        public DbSet<SyncRule> SyncRules { get; set; }
        public DbSet<SyncRuleLog> SyncRuleLogs { get; set; }
        public DbSet<PdfFormMapping> PdfFormMapping { get; set; }
        public DbSet<ReportVisibility> ReportVisibilities { get; set; }
        public DbSet<UserReportTemplate> UserReportTemplates { get; set; }
        public DbSet<UserReportTemplateApplicationType> UserReportTemplateApplicationTypes { get; set; }
        public DbSet<UserReportTemplateProjectContract> UserReportTemplateProjectContracts { get; set; }
        public DbSet<UserReportPlaceholder> UserReportPlaceholders { get; set; }
        public DbSet<PdfGenerationBatch> PdfGenerationBatches { get; set; }
        public DbSet<WordReportGenerationBatch> WordReportGenerationBatches { get; set; }
        public DbSet<MailMergeVisibility> MailMergeVisibility { get; set; }
        public DbSet<StateChangeRule> StateChangeRules { get; set; }
        public DbSet<StateChangeLog> StateChangeLogs { get; set; }
        public DbSet<BoStateSnapshot> BoStateSnapshots { get; set; }
        public DbSet<BusinessObjects.Feedback.UserFeedback> UserFeedbacks { get; set; }
        public DbSet<BusinessObjects.Operations.ApplicationRuntimeLog> ApplicationRuntimeLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.UseDeferredDeletion(this);
            modelBuilder.UseOptimisticLock();
            modelBuilder.SetOneToManyAssociationDeleteBehavior(DeleteBehavior.SetNull, DeleteBehavior.Cascade);
            // Match XAF template (DX 404292): notification strategy + UseChangeTrackingProxies() in Startup so BaseImpl entities
            // (e.g. FileData) get notification interfaces via proxies. Snapshot breaks Model Editor; notifications without proxies fail at runtime.
            modelBuilder.HasChangeTrackingStrategy(ChangeTrackingStrategy.ChangingAndChangedNotificationsWithOriginalValues);

            modelBuilder.Entity<TravelHistory>(b =>
            {
                b.HasDiscriminator<string>("Discriminator")
                    .HasValue<ExternalArrival>(nameof(ExternalArrival))
                    .HasValue<ExternalDeparture>(nameof(ExternalDeparture))
                    .HasValue<InternalArrival>(nameof(InternalArrival))
                    .HasValue<InternalDeparture>(nameof(InternalDeparture));

                b.HasOne(t => t.SourceApplicationItem)
                    .WithMany()
                    .HasForeignKey(t => t.SourceApplicationItemID)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.SetNull);

                b.HasIndex(t => t.SourceApplicationItemID)
                    .IsUnique()
                    .HasFilter("[SourceApplicationItemID] IS NOT NULL AND [GCRecord] IS NULL");
            });

            modelBuilder.Entity<VisaExtensionTracking>(b => {
                b.HasKey(t => t.ID);
                b.ToView("View_VisaExtensionTracking");
                b.HasOne(t => t.ApplicationItem)
                    .WithMany()
                    .HasForeignKey(t => t.ApplicationItemID)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.NoAction);
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
                b.HasOne(t => t.ApplicationItem)
                    .WithMany()
                    .HasForeignKey(t => t.ApplicationItemID)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.NoAction);
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

            modelBuilder.Entity<UserReportTemplateApplicationType>(b => {
                b.HasOne(l => l.UserReportTemplate)
                    .WithMany(t => t.ApplicableTypeLinks)
                    .HasForeignKey(l => l.UserReportTemplateId)
                    .OnDelete(DeleteBehavior.Cascade);
                b.HasOne(l => l.ApplicationType)
                    .WithMany()
                    .HasForeignKey(l => l.ApplicationTypeId)
                    .OnDelete(DeleteBehavior.NoAction);
                b.HasIndex(l => new { l.UserReportTemplateId, l.ApplicationTypeId })
                    .IsUnique()
                    .HasFilter("[GCRecord] IS NULL");
            });

            modelBuilder.Entity<UserReportTemplateProjectContract>(b => {
                b.HasOne(l => l.UserReportTemplate)
                    .WithMany(t => t.ApplicableProjectContractLinks)
                    .HasForeignKey(l => l.UserReportTemplateId)
                    .OnDelete(DeleteBehavior.Cascade);
                b.HasOne(l => l.ProjectContract)
                    .WithMany()
                    .HasForeignKey(l => l.ProjectContractId)
                    .OnDelete(DeleteBehavior.NoAction);
                b.HasIndex(l => new { l.UserReportTemplateId, l.ProjectContractId })
                    .IsUnique()
                    .HasFilter("[GCRecord] IS NULL");
            });

            modelBuilder.Entity<Application>(b => {
                b.HasIndex(a => new { a.AppNumberPrefix, a.ApplicationNumber, a.Year, a.Month })
                 .IsUnique()
                 .HasFilter("[IsManualEntry] = 0");
            });

            modelBuilder.Entity<ApplicationType>(b => {
                b.Property(t => t.SelectionCode).HasMaxLength(3);
                b.HasIndex(t => t.SelectionCode)
                    .IsUnique()
                    .HasFilter("[SelectionCode] IS NOT NULL AND [SelectionCode] <> ''");
            });

            modelBuilder.Entity<ProjectContract>(b =>
            {
                b.Ignore(c => c.Name);
                b.Ignore(c => c.Code);
            });

            modelBuilder.Entity<SystemSettings>()
                .Property(s => s.ExpirationWarningThreshold)
                .HasPrecision(5, 4);

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
                b.HasOne(ai => ai.CurrentVisa).WithMany(v => v.AssociatedApplicationItems)
                    .HasForeignKey(ai => ai.CurrentVisaId)
                    .HasConstraintName("FK_ApplicationItems_Visas_CurrentVisaId")
                    .OnDelete(DeleteBehavior.NoAction);
                b.HasOne(ai => ai.NextVisa).WithMany()
                    .HasForeignKey(ai => ai.NextVisaId)
                    .HasConstraintName("FK_ApplicationItems_Visas_NextVisaId")
                    .OnDelete(DeleteBehavior.NoAction);
                b.HasOne(ai => ai.CurrentWorkPermitItem).WithMany().OnDelete(DeleteBehavior.NoAction);
                // Keep legacy SQL column name SecondWorkPermitItemId (rename property only).
                b.HasOne(ai => ai.PreviousWorkPermitItem).WithMany()
                    .HasForeignKey("SecondWorkPermitItemId")
                    .OnDelete(DeleteBehavior.NoAction);
                b.HasOne(ai => ai.CurrentInvitationItem).WithMany().OnDelete(DeleteBehavior.NoAction);
                b.HasOne(ai => ai.PreviousInvitationItem).WithMany().OnDelete(DeleteBehavior.NoAction);
                b.HasOne(ai => ai.CurrentAddressOfResidence).WithMany().OnDelete(DeleteBehavior.NoAction);
                b.HasOne(ai => ai.CheckPoint).WithMany().OnDelete(DeleteBehavior.NoAction);
                b.HasOne(ai => ai.BusinessTripAddress).WithMany().OnDelete(DeleteBehavior.Cascade);
                b.Property(ai => ai.BorderZoneLocation).HasMaxLength(500);
                b.Property(ai => ai.WorkPermittedLocations).HasMaxLength(500);
                b.HasOne(ai => ai.CurrentWorkDuty).WithMany().OnDelete(DeleteBehavior.NoAction);
                b.HasOne(ai => ai.CurrentSalary).WithMany().OnDelete(DeleteBehavior.NoAction);
                b.HasOne(ai => ai.CurrentMedicalRecord).WithMany().OnDelete(DeleteBehavior.NoAction);
                b.HasOne(ai => ai.CurrentEducation).WithMany().OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<WorkPermitItem>(b => {
                b.HasOne(wpi => wpi.Person).WithMany(p => p.WorkPermitItems).OnDelete(DeleteBehavior.NoAction);
                b.HasOne(wpi => wpi.Passport).WithMany().OnDelete(DeleteBehavior.NoAction);
                b.Property(wpi => wpi.WorkPermittedLocations).HasMaxLength(500);
            });

            modelBuilder.Entity<Visa>(b => {
                b.HasOne(v => v.Passport).WithMany(p => p.Visas).OnDelete(DeleteBehavior.NoAction);
                b.HasOne(v => v.IssuingApplicationItem).WithMany().OnDelete(DeleteBehavior.NoAction).IsRequired(false);
                b.Metadata.UseSqlOutputClause(false);
                b.Property(v => v.ExtensionRequired).HasDefaultValue(true);
                b.Property(v => v.BorderZoneLocation).HasMaxLength(500);
            });

            modelBuilder.Entity<Passport>(b =>
            {
                b.HasOne(p => p.Person).WithMany(p => p.Passports).OnDelete(DeleteBehavior.NoAction);
                b.Property(p => p.PersonalNumber).IsRequired(false);
                b.Navigation(p => p.Documents).UsePropertyAccessMode(PropertyAccessMode.Property);
            });

            modelBuilder.Entity<Person>()
                .Property(p => p.PersonalNumber)
                .IsRequired(false);

            // Filtered unique index: SQL Server allows only one NULL in a non-filtered UNIQUE index; this
            // filter excludes NULL and empty string so many legacy NULL rows can coexist. Error 10735:
            // filtered index predicates cannot use LTRIM/RTRIM (or most string functions)—only simple comparisons.
            // Exclude literal N'0' so employees without a passport personal number can share that sentinel (DB must match trimmed "0" in app validation).
            // Whitespace-only values are still indexed; trim/dup logic also enforced on save via IsPersonalNumberUniqueAmongActive.
            modelBuilder.Entity<Person>()
                .HasIndex(p => p.PersonalNumber)
                .IsUnique()
                .HasFilter("[PersonalNumber] IS NOT NULL AND [PersonalNumber] <> N'' AND [PersonalNumber] <> N'0'");

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

            modelBuilder.Entity<WordReportGenerationBatch>(b =>
            {
                b.HasOne(x => x.Application)
                    .WithMany()
                    .HasForeignKey(x => x.ApplicationID)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<PdfGenerationBatch>(b =>
            {
                b.Property(x => x.IncludeDiplomaFiles).HasDefaultValue(true);
                b.Property(x => x.DiplomaScope).HasDefaultValue(PdfBatchDiplomaScope.AllEducations);
                b.Property(x => x.SupportingZipMergeOption).HasDefaultValue(PdfSupportingZipMergeOption.IndividualFilesAndMergedPdfs);
                b.Property(x => x.IncludeMergedDiplomaPdf).HasDefaultValue(false);
                b.Property(x => x.IncludePassportCopies).HasDefaultValue(true);
                b.Property(x => x.IncludeVisaCopies).HasDefaultValue(true);
                b.Property(x => x.IncludeMedicalRecordCopies).HasDefaultValue(true);
                b.Property(x => x.IncludeAddressOfResidenceCopies).HasDefaultValue(true);
                b.Property(x => x.IncludeWorkPermitCopies).HasDefaultValue(true);
                b.Property(x => x.IncludeInvitationCopies).HasDefaultValue(true);
                b.Property(x => x.IncludeFamilyRelationshipCopies).HasDefaultValue(true);
            });

            modelBuilder.Entity<BusinessObjects.Operations.ApplicationRuntimeLog>(b =>
            {
                b.Property(x => x.StackTrace).HasColumnType("nvarchar(max)");
                b.HasIndex(x => x.OccurredAtUtc);
                b.HasIndex(x => x.Severity);
                b.HasIndex(x => x.CorrelationId);
                b.HasIndex(x => x.ResolutionStatus);
            });

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