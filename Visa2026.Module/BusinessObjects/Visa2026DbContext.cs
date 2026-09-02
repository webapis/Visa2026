using DevExpress.ExpressApp.Design;
using DevExpress.ExpressApp.EFCore.DesignTime;
using DevExpress.ExpressApp.EFCore.Updating;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.BaseImpl.EF.PermissionPolicy;
using DevExpress.Persistent.BaseImpl.EFCore.AuditTrail;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Visa2026.Module.BusinessObjects
{
    [TypesInfoInitializer(typeof(DbContextTypesInfoInitializer<Visa2026EFCoreDbContext>))]
    public class Visa2026EFCoreDbContext : DbContext
    {
        public Visa2026EFCoreDbContext(DbContextOptions<Visa2026EFCoreDbContext> options) : base(options)
        {
            Database.SetCommandTimeout(180); // 3-minute timeout; the default 30s is too short for complex prefetch queries
        }

        /// <summary>
        /// Filtered-index predicates: SQL Server uses <c>[Col]</c>; PostgreSQL uses <c>"Col"</c>.
        /// Bool filters use <c>= 0</c> on SQL Server and <c>= FALSE</c> on PostgreSQL.
        /// </summary>
        private string IndexFilter(string sqlServerPredicate)
        {
            var provider = Database.ProviderName ?? string.Empty;
            if (!provider.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
                return sqlServerPredicate;

            return sqlServerPredicate
                .Replace("[", "\"", StringComparison.Ordinal)
                .Replace("]", "\"", StringComparison.Ordinal)
                .Replace("N''", "''", StringComparison.Ordinal)
                .Replace("N'0'", "'0'", StringComparison.Ordinal)
                .Replace("= 0", "= FALSE", StringComparison.Ordinal);
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
        public DbSet<Hotel> Hotels { get; set; }
        public DbSet<Hospital> Hospitals { get; set; }
        public DbSet<OtherSite> OtherSites { get; set; }
        public DbSet<EducationImage> EducationImages { get; set; }
        public DbSet<InvitationImage> InvitationImages { get; set; }
        public DbSet<InvitationDocument> InvitationDocuments { get; set; }
        public DbSet<MedicalRecordDocument> MedicalRecordDocuments { get; set; }
        public DbSet<ProjectContract> ProjectContracts { get; set; }
        public DbSet<ProjectContractImage> ProjectContractImages { get; set; }
        public DbSet<ProjectContractDocument> ProjectContractDocuments { get; set; }
        public DbSet<ApprovingMinistry> ApprovingMinistries { get; set; }
        public DbSet<ApprovalLegProfile> ApprovalLegProfiles { get; set; }
        public DbSet<ApprovalLegProfileMinistryLeg> ApprovalLegProfileMinistryLegs { get; set; }
        public DbSet<ProjectContractApprovalLegProfile> ProjectContractApprovalLegProfiles { get; set; }
        public DbSet<ApplicationProfileInstanceApprovalLegSnapshot> ApplicationProfileInstanceApprovalLegSnapshots { get; set; }
        public DbSet<VisaPeriod> VisaPeriods { get; set; }
        public DbSet<VisaCategory> VisaCategories { get; set; }
        public DbSet<Invitation> Invitations { get; set; }
        public DbSet<InvitationItem> InvitationItems { get; set; }
        public DbSet<Rejection> Rejections { get; set; }
        public DbSet<RejectionItem> RejectionItems { get; set; }
        public DbSet<RejectionImage> RejectionImages { get; set; }
        public DbSet<RejectionDocument> RejectionDocuments { get; set; }
        public DbSet<BorderZone> BorderZones { get; set; }
        public DbSet<BorderZoneItem> BorderZoneItems { get; set; }
        public DbSet<BorderZoneDocument> BorderZoneDocuments { get; set; }
        public DbSet<CheckPoint> CheckPoints { get; set; }
        public DbSet<VisaIssuedPlace> VisaIssuedPlaces { get; set; }
        public DbSet<PurposeOfTravel> PurposeOfTravels { get; set; }
        public DbSet<BusinessTripAddress> BusinessTripAddresses { get; set; }
        public DbSet<BusinessTripPurpose> BusinessTripPurposes { get; set; }
        public DbSet<WorkPermit> WorkPermits { get; set; }
        public DbSet<WorkPermitDocument> WorkPermitDocuments { get; set; }
        public DbSet<WorkPermitImage> WorkPermitImages { get; set; }
        public DbSet<Urgency> Urgencies { get; set; }
        public DbSet<Subcontractor> Subcontractors { get; set; }
        public DbSet<ApplicationProfileInstance> ApplicationProfileInstances { get; set; }
        public DbSet<MigrationService> MigrationServices { get; set; }
        public DbSet<EmployeeSalary> EmployeeSalaries { get; set; }
        public DbSet<WorkDuty> WorkDuties { get; set; }
        public DbSet<ContractTemplate> ContractTemplates { get; set; }
        public DbSet<ApplicationType> ApplicationTypes { get; set; }
        public DbSet<ApplicationProfile> ApplicationProfiles { get; set; }
        public DbSet<ApplicationProfileApprovalLeg> ApplicationProfileApprovalLegs { get; set; }
        public DbSet<ApplicationProfileApprovalLegVersion> ApplicationProfileApprovalLegVersions { get; set; }
        public DbSet<ApplicationProfileTemplate> ApplicationProfileTemplates { get; set; }
        public DbSet<ApplicationProfileProgressStateSetting> ApplicationProfileProgressStateSettings { get; set; }
        public DbSet<ApplicationProfileInstancePersonResolvedLink> ApplicationProfileInstancePersonResolvedLinks { get; set; }
        public DbSet<ApplicationTypeGroup> ApplicationTypeGroups { get; set; }
        public DbSet<ApplicationTypeGroupMember> ApplicationTypeGroupMembers { get; set; }
        public DbSet<ApplicationState> ApplicationStates { get; set; }
        public DbSet<ApplicationProfileInstanceProgress> ApplicationProfileInstanceProgresses { get; set; }
        public DbSet<ApplicationLocation> ApplicationLocations { get; set; }
        public DbSet<ValidityDuration> ValidityDurations { get; set; }
        public DbSet<VisaExtensionTracking> VisaExtensionTracking { get; set; }
        public DbSet<VisaExtensionStatus> VisaExtensionStatus { get; set; }
        public DbSet<WorkPermitExtensionTracking> WorkPermitExtensionTracking { get; set; }
        public DbSet<WorkPermitExtensionStatus> WorkPermitExtensionStatus { get; set; }
        public DbSet<VisaTransferStatus> VisaTransferStatus { get; set; }
        public DbSet<VisaCancelExtStatus> VisaCancelExtStatus { get; set; }
        public DbSet<VisaCancellationStatus> VisaCancellationStatus { get; set; }
        public DbSet<ForeignWorkerMaglumat> ForeignWorkerMaglumat { get; set; }
        public DbSet<VwRdPassport> VwRdPassport { get; set; }
        public DbSet<VwRdWorkPermit> VwRdWorkPermit { get; set; }
        public DbSet<VwRdWorkPermitActive> VwRdWorkPermitActive { get; set; }
        public DbSet<VwRdWorkPermitAppProgress> VwRdWorkPermitAppProgress { get; set; }
        public DbSet<VwRdInvitationReady> VwRdInvitationReady { get; set; }
        public DbSet<VwRdInvitationInProcess> VwRdInvitationInProcess { get; set; }
        public DbSet<VwRdApplicationViaMinistryInvitationOnProcess> VwRdApplicationViaMinistryInvitationOnProcess { get; set; }
        public DbSet<VwRdApplicationViaMinistryInvitationOnProcessByPeriodCategoryType> VwRdApplicationViaMinistryInvitationOnProcessByPeriodCategoryType { get; set; }
        public DbSet<VwRdApplicationViaMinistryInvitationCompleted> VwRdApplicationViaMinistryInvitationCompleted { get; set; }
        public DbSet<VwRdApplicationViaMinistryInvitationCompletedByPeriodCategoryType> VwRdApplicationViaMinistryInvitationCompletedByPeriodCategoryType { get; set; }
        public DbSet<VwRdApplicationViaMinistryVisaExtensionOnProcess> VwRdApplicationViaMinistryVisaExtensionOnProcess { get; set; }
        public DbSet<VwRdApplicationViaMinistryVisaExtensionOnProcessByPeriodCategoryType> VwRdApplicationViaMinistryVisaExtensionOnProcessByPeriodCategoryType { get; set; }
        public DbSet<VwRdApplicationViaMinistryVisaExtensionCompleted> VwRdApplicationViaMinistryVisaExtensionCompleted { get; set; }
        public DbSet<VwRdApplicationViaMinistryVisaExtensionCompletedByPeriodCategoryType> VwRdApplicationViaMinistryVisaExtensionCompletedByPeriodCategoryType { get; set; }
        public DbSet<VwRdApplicationViaMinistryOtherOnProcess> VwRdApplicationViaMinistryOtherOnProcess { get; set; }
        public DbSet<VwRdApplicationViaMinistryOtherCompleted> VwRdApplicationViaMinistryOtherCompleted { get; set; }
        public DbSet<VwRdApplicationDirectMigrationOnProcessA> VwRdApplicationDirectMigrationOnProcessA { get; set; }
        public DbSet<VwRdApplicationDirectMigrationProcessComplete> VwRdApplicationDirectMigrationProcessComplete { get; set; }
        public DbSet<VwRdInvitationRejected> VwRdInvitationRejected { get; set; }
        public DbSet<VwRdInvitationUsed> VwRdInvitationUsed { get; set; }
        public DbSet<VwRdInvitationValidUntil> VwRdInvitationValidUntil { get; set; }
        public DbSet<VwRdVisaAppProgress> VwRdVisaAppProgress { get; set; }
        public DbSet<VwRdProject> VwRdProject { get; set; }
        public DbSet<VwRdPersonRole> VwRdPersonRole { get; set; }
        public DbSet<VwRdVisaState> VwRdVisaState { get; set; }
        public DbSet<VwRdVisaByCategory> VwRdVisaByCategory { get; set; }
        public DbSet<VwRdVisaByType> VwRdVisaByType { get; set; }
        public DbSet<VwRdVisaByPeriod> VwRdVisaByPeriod { get; set; }
        public DbSet<VwRdVisaActiveByProject> VwRdVisaActiveByProject { get; set; }
        public DbSet<VwRdVisaActiveByPeriodCategoryType> VwRdVisaActiveByPeriodCategoryType { get; set; }
        public DbSet<VwRdVisaOnExtension> VwRdVisaOnExtension { get; set; }
        public DbSet<VwRdVisaOnExtensionByPeriodCategoryType> VwRdVisaOnExtensionByPeriodCategoryType { get; set; }
        public DbSet<VwRdVisaExtensionResult> VwRdVisaExtensionResult { get; set; }
        public DbSet<VwRdVisaExtensionResultByPeriodCategoryType> VwRdVisaExtensionResultByPeriodCategoryType { get; set; }
        public DbSet<VwRdVisaByDaysRemaining> VwRdVisaByDaysRemaining { get; set; }
        public DbSet<VwRdVisaExtensionRequired> VwRdVisaExtensionRequired { get; set; }
        public DbSet<VwRdApplication> VwRdApplication { get; set; }
        public DbSet<VwRdEducation> VwRdEducation { get; set; }
        public DbSet<VwRdEducationByCountry> VwRdEducationByCountry { get; set; }
        public DbSet<VwRdIncompletePersonsByMissingArea> VwRdIncompletePersonsByMissingArea { get; set; }
        public DbSet<VwRdPersonSearch> VwRdPersonSearch { get; set; }
        public DbSet<VwRdPositionHistory> VwRdPositionHistory { get; set; }
        public DbSet<VwRdRegistration> VwRdRegistration { get; set; }
        public DbSet<VwRdToBeCheckedIn> VwRdToBeCheckedIn { get; set; }
        public DbSet<VwRdToBeCheckedOut> VwRdToBeCheckedOut { get; set; }
        public DbSet<TravelHistory> TravelHistories { get; set; }
        public DbSet<ExternalArrival> ExternalArrivals { get; set; }
        public DbSet<ExternalDeparture> ExternalDepartures { get; set; }
        public DbSet<InternalArrival> InternalArrivals { get; set; }
        public DbSet<InternalDeparture> InternalDepartures { get; set; }
        public DbSet<SystemSettings> SystemSettings { get; set; }
        public DbSet<MinistryReviewSlaSettings> MinistryReviewSlaSettings { get; set; }
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
        public DbSet<UserReportTemplateApplicationTypeGroup> UserReportTemplateApplicationTypeGroups { get; set; }
        public DbSet<UserReportTemplateProjectContract> UserReportTemplateProjectContracts { get; set; }
        public DbSet<UserReportPlaceholder> UserReportPlaceholders { get; set; }
        public DbSet<PdfGenerationBatch> PdfGenerationBatches { get; set; }
        public DbSet<WordReportGenerationBatch> WordReportGenerationBatches { get; set; }
        public DbSet<PersonExportBatch> PersonExportBatches { get; set; }
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

            modelBuilder.Entity<ProjectContractApprovalLegProfile>(b =>
            {
                b.HasIndex(e => new { e.ProjectContractId, e.ApprovalLegProfileId })
                    .IsUnique()
                    .HasFilter(IndexFilter("[GCRecord] IS NULL"));
            });

            modelBuilder.Entity<TravelHistory>(b =>
            {
                b.HasDiscriminator<string>("Discriminator")
                    .HasValue<ExternalArrival>(nameof(ExternalArrival))
                    .HasValue<ExternalDeparture>(nameof(ExternalDeparture))
                    .HasValue<InternalArrival>(nameof(InternalArrival))
                    .HasValue<InternalDeparture>(nameof(InternalDeparture));

            });

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
                b.HasOne(c => c.ApplicationProfileInstance).WithMany().HasForeignKey(c => c.ApplicationProfileInstanceID).OnDelete(DeleteBehavior.NoAction);
                b.HasOne(c => c.Visa).WithMany().HasForeignKey(c => c.VisaID).OnDelete(DeleteBehavior.NoAction);
                b.HasOne(c => c.Person).WithMany().HasForeignKey(c => c.PersonID).OnDelete(DeleteBehavior.NoAction);
                b.HasOne(c => c.Passport).WithMany().HasForeignKey(c => c.PassportID).OnDelete(DeleteBehavior.NoAction);
                b.HasOne(c => c.CurrentState).WithMany().HasForeignKey(c => c.CurrentStateID).OnDelete(DeleteBehavior.NoAction);
                b.HasOne(c => c.CheckOutState).WithMany().HasForeignKey(c => c.CheckOutStateID).OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<ForeignWorkerMaglumat>(b => {
                b.HasKey(t => t.ID);
                b.ToView("View_ForeignWorkerMaglumat");
            });

            modelBuilder.Entity<VwRdPassport>(b => {
                b.HasKey(t => t.ID);
                b.ToView("vw_rd_passport");
            });

            modelBuilder.Entity<VwRdWorkPermit>(b => {
                b.HasKey(t => t.ID);
                b.ToView("vw_rd_work_permit");
            });

            modelBuilder.Entity<VwRdWorkPermitActive>(b => {
                b.HasKey(t => t.ID);
                b.ToView("vw_rd_work_permit_active");
            });

            modelBuilder.Entity<VwRdWorkPermitAppProgress>(b => {
                b.HasKey(t => t.ID);
                b.ToView("vw_rd_work_permit_app_progress");
            });

            modelBuilder.Entity<VwRdInvitationReady>(b => {
                b.HasKey(t => t.ID);
                b.ToView("vw_rd_invitation_ready");
            });

            modelBuilder.Entity<VwRdInvitationInProcess>(b => {
                b.HasKey(t => t.ID);
                b.ToView("vw_rd_invitation_in_process");
            });

            modelBuilder.Entity<VwRdApplicationViaMinistryInvitationOnProcess>(b => {
                b.HasKey(t => t.ID);
                b.ToView("vw_rd_application_via_ministry_invitation_on_process");
            });
            modelBuilder.Entity<VwRdApplicationViaMinistryInvitationOnProcessByPeriodCategoryType>(b => {
                b.HasKey(t => t.ID);
                b.ToView("vw_rd_application_via_ministry_invitation_on_process_by_period_category_type");
            });
            modelBuilder.Entity<VwRdApplicationViaMinistryInvitationCompleted>(b => {
                b.HasKey(t => t.ID);
                b.ToView("vw_rd_application_via_ministry_invitation_completed");
            });
            modelBuilder.Entity<VwRdApplicationViaMinistryInvitationCompletedByPeriodCategoryType>(b => {
                b.HasKey(t => t.ID);
                b.ToView("vw_rd_application_via_ministry_invitation_completed_by_period_category_type");
            });
            modelBuilder.Entity<VwRdApplicationViaMinistryVisaExtensionOnProcess>(b => {
                b.HasKey(t => t.ID);
                b.ToView("vw_rd_application_via_ministry_visa_extension_on_process");
            });
            modelBuilder.Entity<VwRdApplicationViaMinistryVisaExtensionOnProcessByPeriodCategoryType>(b => {
                b.HasKey(t => t.ID);
                b.ToView("vw_rd_application_via_ministry_visa_extension_on_process_by_period_category_type");
            });
            modelBuilder.Entity<VwRdApplicationViaMinistryVisaExtensionCompleted>(b => {
                b.HasKey(t => t.ID);
                b.ToView("vw_rd_application_via_ministry_visa_extension_completed");
            });
            modelBuilder.Entity<VwRdApplicationViaMinistryVisaExtensionCompletedByPeriodCategoryType>(b => {
                b.HasKey(t => t.ID);
                b.ToView("vw_rd_application_via_ministry_visa_extension_completed_by_period_category_type");
            });
            modelBuilder.Entity<VwRdApplicationViaMinistryOtherOnProcess>(b => {
                b.HasKey(t => t.ID);
                b.ToView("vw_rd_application_via_ministry_other_on_process");
            });
            modelBuilder.Entity<VwRdApplicationViaMinistryOtherCompleted>(b => {
                b.HasKey(t => t.ID);
                b.ToView("vw_rd_application_via_ministry_other_completed");
            });

            modelBuilder.Entity<VwRdApplicationDirectMigrationOnProcessA>(b => {
                b.HasKey(t => t.ID);
                b.ToView("vw_rd_application_direct_migration_on_process_a");
            });
            modelBuilder.Entity<VwRdApplicationDirectMigrationProcessComplete>(b => {
                b.HasKey(t => t.ID);
                b.ToView("vw_rd_application_direct_migration_process_complete");
            });

            modelBuilder.Entity<VwRdInvitationRejected>(b => {
                b.HasKey(t => new { t.SourceKind, t.ID });
                b.ToView("vw_rd_invitation_rejected");
            });

            modelBuilder.Entity<VwRdInvitationUsed>(b => {
                b.HasKey(t => t.ID);
                b.ToView("vw_rd_invitation_used");
            });

            modelBuilder.Entity<VwRdInvitationValidUntil>(b => {
                b.HasKey(t => t.ID);
                b.ToView("vw_rd_invitation_valid_until");
            });

            modelBuilder.Entity<VwRdVisaAppProgress>(b => {
                b.HasKey(t => t.ID);
                b.ToView("vw_rd_visa_app_progress");
                b.HasOne(t => t.ApplicationProfileInstance).WithMany().HasForeignKey(t => t.ApplicationProfileInstanceOid).OnDelete(DeleteBehavior.NoAction);
                b.HasOne(t => t.Person).WithMany().HasForeignKey(t => t.PersonOid).OnDelete(DeleteBehavior.NoAction);
                b.HasOne(t => t.ExpiringVisa).WithMany().HasForeignKey(t => t.ExpiringVisaID).OnDelete(DeleteBehavior.NoAction);
                b.HasOne(t => t.Passport).WithMany().HasForeignKey(t => t.PassportID).OnDelete(DeleteBehavior.NoAction);
                b.HasOne(t => t.CurrentState).WithMany().HasForeignKey(t => t.CurrentStateID).OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<VwRdProject>(b => {
                b.HasKey(t => new { t.ProjectOid, t.PersonRoleCode });
                b.ToView("vw_rd_projects");
            });

            modelBuilder.Entity<VwRdPersonRole>(b => {
                b.HasKey(t => t.PersonRoleCode);
                b.ToView("vw_rd_person_roles");
            });

            modelBuilder.Entity<VwRdVisaState>(b => {
                b.HasKey(t => t.ID);
                b.ToView("vw_rd_visa_state");
            });

            modelBuilder.Entity<VwRdVisaByCategory>(b => {
                b.HasKey(t => t.ID);
                b.ToView("vw_rd_visa_by_category");
            });

            modelBuilder.Entity<VwRdVisaByType>(b => {
                b.HasKey(t => t.ID);
                b.ToView("vw_rd_visa_by_type");
            });

            modelBuilder.Entity<VwRdVisaByPeriod>(b => {
                b.HasKey(t => t.ID);
                b.ToView("vw_rd_visa_by_period");
                b.HasOne(t => t.Person).WithMany().HasForeignKey(t => t.PersonOid).OnDelete(DeleteBehavior.NoAction);
                b.HasOne(t => t.Passport).WithMany().HasForeignKey(t => t.PassportID).OnDelete(DeleteBehavior.NoAction);
                b.HasOne(t => t.Visa).WithMany().HasForeignKey(t => t.ID).OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<VwRdVisaActiveByProject>(b => {
                b.HasKey(t => t.ID);
                b.ToView("vw_rd_visa_active_by_project");
                b.HasOne(t => t.Person).WithMany().HasForeignKey(t => t.PersonOid).OnDelete(DeleteBehavior.NoAction);
                b.HasOne(t => t.Passport).WithMany().HasForeignKey(t => t.PassportID).OnDelete(DeleteBehavior.NoAction);
                b.HasOne(t => t.Visa).WithMany().HasForeignKey(t => t.ID).OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<VwRdVisaActiveByPeriodCategoryType>(b => {
                b.HasKey(t => t.ID);
                b.ToView("vw_rd_visa_active_by_period_category_type");
                b.HasOne(t => t.Person).WithMany().HasForeignKey(t => t.PersonOid).OnDelete(DeleteBehavior.NoAction);
                b.HasOne(t => t.Passport).WithMany().HasForeignKey(t => t.PassportID).OnDelete(DeleteBehavior.NoAction);
                b.HasOne(t => t.Visa).WithMany().HasForeignKey(t => t.ID).OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<VwRdVisaOnExtension>(b => {
                b.HasKey(t => t.ID);
                b.ToView("vw_rd_visa_on_extension");
                b.HasOne(t => t.ApplicationProfileInstance).WithMany().HasForeignKey(t => t.ApplicationProfileInstanceOid).OnDelete(DeleteBehavior.NoAction);
                b.HasOne(t => t.Person).WithMany().HasForeignKey(t => t.PersonOid).OnDelete(DeleteBehavior.NoAction);
                b.HasOne(t => t.ExpiringVisa).WithMany().HasForeignKey(t => t.ExpiringVisaID).OnDelete(DeleteBehavior.NoAction);
                b.HasOne(t => t.Passport).WithMany().HasForeignKey(t => t.PassportID).OnDelete(DeleteBehavior.NoAction);
                b.HasOne(t => t.CurrentState).WithMany().HasForeignKey(t => t.CurrentStateID).OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<VwRdVisaOnExtensionByPeriodCategoryType>(b => {
                b.HasKey(t => t.ID);
                b.ToView("vw_rd_visa_on_extension_by_period_category_type");
                b.HasOne(t => t.ApplicationProfileInstance).WithMany().HasForeignKey(t => t.ApplicationProfileInstanceOid).OnDelete(DeleteBehavior.NoAction);
                b.HasOne(t => t.Person).WithMany().HasForeignKey(t => t.PersonOid).OnDelete(DeleteBehavior.NoAction);
                b.HasOne(t => t.ExpiringVisa).WithMany().HasForeignKey(t => t.ExpiringVisaID).OnDelete(DeleteBehavior.NoAction);
                b.HasOne(t => t.Passport).WithMany().HasForeignKey(t => t.PassportID).OnDelete(DeleteBehavior.NoAction);
                b.HasOne(t => t.CurrentState).WithMany().HasForeignKey(t => t.CurrentStateID).OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<VwRdVisaExtensionResult>(b => {
                b.HasKey(t => t.ID);
                b.ToView("vw_rd_visa_extension_result");
                b.HasOne(t => t.ApplicationProfileInstance).WithMany().HasForeignKey(t => t.ApplicationProfileInstanceOid).OnDelete(DeleteBehavior.NoAction);
                b.HasOne(t => t.Person).WithMany().HasForeignKey(t => t.PersonOid).OnDelete(DeleteBehavior.NoAction);
                b.HasOne(t => t.ExpiringVisa).WithMany().HasForeignKey(t => t.ExpiringVisaID).OnDelete(DeleteBehavior.NoAction);
                b.HasOne(t => t.Passport).WithMany().HasForeignKey(t => t.PassportID).OnDelete(DeleteBehavior.NoAction);
                b.HasOne(t => t.CurrentState).WithMany().HasForeignKey(t => t.CurrentStateID).OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<VwRdVisaExtensionResultByPeriodCategoryType>(b => {
                b.HasKey(t => t.ID);
                b.ToView("vw_rd_visa_extension_result_by_period_category_type");
                b.HasOne(t => t.ApplicationProfileInstance).WithMany().HasForeignKey(t => t.ApplicationProfileInstanceOid).OnDelete(DeleteBehavior.NoAction);
                b.HasOne(t => t.Person).WithMany().HasForeignKey(t => t.PersonOid).OnDelete(DeleteBehavior.NoAction);
                b.HasOne(t => t.ExpiringVisa).WithMany().HasForeignKey(t => t.ExpiringVisaID).OnDelete(DeleteBehavior.NoAction);
                b.HasOne(t => t.Passport).WithMany().HasForeignKey(t => t.PassportID).OnDelete(DeleteBehavior.NoAction);
                b.HasOne(t => t.CurrentState).WithMany().HasForeignKey(t => t.CurrentStateID).OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<VwRdVisaByDaysRemaining>(b => {
                b.HasKey(t => t.ID);
                b.ToView("vw_rd_visa_by_days_remaining");
                b.HasOne(t => t.Person).WithMany().HasForeignKey(t => t.PersonOid).OnDelete(DeleteBehavior.NoAction);
                b.HasOne(t => t.Passport).WithMany().HasForeignKey(t => t.PassportID).OnDelete(DeleteBehavior.NoAction);
                b.HasOne(t => t.Visa).WithMany().HasForeignKey(t => t.ID).OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<VwRdVisaExtensionRequired>(b => {
                b.HasKey(t => t.ID);
                b.ToView("vw_rd_visa_extension_required");
                b.HasOne(t => t.Person).WithMany().HasForeignKey(t => t.PersonOid).OnDelete(DeleteBehavior.NoAction);
                b.HasOne(t => t.Passport).WithMany().HasForeignKey(t => t.PassportID).OnDelete(DeleteBehavior.NoAction);
                b.HasOne(t => t.Visa).WithMany().HasForeignKey(t => t.ID).OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<VwRdApplication>(b => {
                b.HasKey(t => t.ID);
                b.ToView("vw_rd_application");
            });

            modelBuilder.Entity<VwRdEducation>(b => {
                b.HasKey(t => t.ID);
                b.ToView("vw_rd_education");
            });

            modelBuilder.Entity<VwRdEducationByCountry>(b => {
                b.HasKey(t => t.ID);
                b.ToView("vw_rd_education_by_country");
            });

            modelBuilder.Entity<VwRdIncompletePersonsByMissingArea>(b => {
                b.HasKey(t => t.ID);
                b.ToView("vw_rd_incomplete_persons_by_missing_area");
                b.HasOne(t => t.Person).WithMany().HasForeignKey(t => t.PersonOid).OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<VwRdPersonSearch>(b => {
                b.HasKey(t => t.ID);
                b.ToView("vw_rd_person_search");
                b.HasOne(t => t.Person).WithMany().HasForeignKey(t => t.PersonOid).OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<VwRdPositionHistory>(b => {
                b.HasKey(t => t.ID);
                b.ToView("vw_rd_position_history");
            });

            modelBuilder.Entity<VwRdRegistration>(b => {
                b.HasKey(t => t.ID);
                b.ToView("vw_rd_registration");
            });

            modelBuilder.Entity<VwRdToBeCheckedIn>(b => {
                b.HasKey(t => t.ID);
                b.ToView("vw_rd_to_be_checked_in");
            });

            modelBuilder.Entity<VwRdToBeCheckedOut>(b => {
                b.HasKey(t => t.ID);
                b.ToView("vw_rd_to_be_checked_out");
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
                    .HasFilter(IndexFilter("[GCRecord] IS NULL"));
            });

            modelBuilder.Entity<ApplicationTypeGroupMember>(b => {
                b.HasOne(l => l.ApplicationTypeGroup)
                    .WithMany(g => g.Members)
                    .HasForeignKey(l => l.ApplicationTypeGroupId)
                    .OnDelete(DeleteBehavior.Cascade);
                b.HasOne(l => l.ApplicationType)
                    .WithMany()
                    .HasForeignKey(l => l.ApplicationTypeId)
                    .OnDelete(DeleteBehavior.NoAction);
                b.HasIndex(l => new { l.ApplicationTypeGroupId, l.ApplicationTypeId })
                    .IsUnique()
                    .HasFilter(IndexFilter("[GCRecord] IS NULL"));
            });

            modelBuilder.Entity<UserReportTemplateApplicationTypeGroup>(b => {
                b.HasOne(l => l.UserReportTemplate)
                    .WithMany(t => t.ApplicableGroupLinks)
                    .HasForeignKey(l => l.UserReportTemplateId)
                    .OnDelete(DeleteBehavior.Cascade);
                b.HasOne(l => l.ApplicationTypeGroup)
                    .WithMany()
                    .HasForeignKey(l => l.ApplicationTypeGroupId)
                    .OnDelete(DeleteBehavior.NoAction);
                b.HasIndex(l => new { l.UserReportTemplateId, l.ApplicationTypeGroupId })
                    .IsUnique()
                    .HasFilter(IndexFilter("[GCRecord] IS NULL"));
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
                    .HasFilter(IndexFilter("[GCRecord] IS NULL"));
            });

            modelBuilder.Entity<ApplicationProfileInstance>(b => {
                b.HasOne(a => a.LatestProgress)
                    .WithMany()
                    .HasForeignKey(a => a.LatestProgressId)
                    .OnDelete(DeleteBehavior.NoAction);
                b.HasOne(a => a.ApplicationProfile)
                    .WithMany(p => p.Instances)
                    .HasForeignKey("ApplicationProfileID")
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);
                b.HasOne(a => a.Region)
                    .WithMany()
                    .HasForeignKey("RegionId")
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.NoAction);
                b.HasOne(a => a.City)
                    .WithMany()
                    .HasForeignKey("CityId")
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.NoAction);
                b.HasOne(a => a.BusinessTripAddress)
                    .WithMany()
                    .HasForeignKey("BusinessTripAddressId")
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.NoAction);
                b.Property(a => a.Purpose).HasMaxLength(700);
                b.Property(a => a.BorderZoneLocation).HasMaxLength(500);
                b.Property(a => a.MovementPermitLocation).HasMaxLength(500);
                b.HasIndex(a => new { a.AppNumberPrefix, a.ApplicationNumber, a.Year, a.Month })
                 .IsUnique()
                 .HasFilter(IndexFilter("[IsManualEntry] = 0 AND [GCRecord] IS NULL"));
                b.HasIndex(a => a.ProcessNumber)
                 .IsUnique()
                 .HasDatabaseName("IX_ApplicationProfileInstances_ProcessNumber")
                 .HasFilter(IndexFilter("[ProcessNumber] IS NOT NULL AND [ProcessNumber] <> '' AND [GCRecord] IS NULL"));
                b.HasIndex("ApplicationTypeID")
                 .HasDatabaseName("IX_Applications_ApplicationTypeID_List");
                b.HasIndex("ApplicationProfileID")
                 .HasDatabaseName("IX_Applications_ApplicationProfileID");
            });

            modelBuilder.Entity<ApplicationProfile>(b =>
            {
                b.Property(p => p.Name).HasMaxLength(200);
                b.Property(p => p.Code).HasMaxLength(64);
                b.Property(p => p.SelectionCode).HasMaxLength(3);
                b.Property(p => p.DefaultBorderZoneLocation).HasMaxLength(500);
                b.Property(p => p.DefaultWorkPermitLocation).HasMaxLength(500);
                b.HasOne(p => p.DefaultApprovalLegProfile).WithMany().HasForeignKey(p => p.DefaultApprovalLegProfileId).OnDelete(DeleteBehavior.SetNull);
                b.Property(p => p.DefaultPurpose).HasMaxLength(700);
                b.HasOne(p => p.DefaultBusinessTripAddress)
                    .WithMany()
                    .HasForeignKey(p => p.DefaultBusinessTripAddressId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.NoAction);
                b.HasIndex(p => p.Code)
                    .IsUnique()
                    .HasFilter(IndexFilter("[Code] IS NOT NULL AND [Code] <> '' AND [GCRecord] IS NULL"));
                b.HasIndex(p => p.SelectionCode)
                    .HasDatabaseName("IX_ApplicationProfiles_SelectionCode");
            });

            modelBuilder.Entity<BusinessTripAddress>(b =>
            {
                b.ToTable("BusinessTripAddress");
                b.Property(a => a.FullAddress).HasMaxLength(255);
                b.HasOne(a => a.City)
                    .WithMany()
                    .HasForeignKey("CityID")
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<BusinessTripPurpose>(b =>
            {
                b.ToTable("BusinessTripPurpose");
                b.Property(p => p.Name).HasMaxLength(200);
                b.Property(p => p.Description).HasMaxLength(2000);
            });

            modelBuilder.Entity<ApplicationProfileApprovalLegVersion>(b =>
            {
                b.Property(v => v.Name).HasMaxLength(200);
                b.HasOne(v => v.ApplicationProfile)
                    .WithMany(p => p.ApprovalLegVersions)
                    .HasForeignKey(v => v.ApplicationProfileId)
                    .OnDelete(DeleteBehavior.Cascade);
                b.HasIndex(v => new { v.ApplicationProfileId, v.Sequence })
                    .HasDatabaseName("IX_ApplicationProfileApprovalLegVersions_Profile_Sequence");
            });

            modelBuilder.Entity<ApplicationProfileApprovalLeg>(b =>
            {
                b.HasOne(l => l.ApplicationProfile)
                    .WithMany(p => p.ApprovalLegs)
                    .HasForeignKey(l => l.ApplicationProfileId)
                    .OnDelete(DeleteBehavior.Cascade);
                b.HasOne(l => l.ApprovalLegVersion)
                    .WithMany(v => v.Legs)
                    .HasForeignKey(l => l.ApprovalLegVersionId)
                    .OnDelete(DeleteBehavior.SetNull);
                b.HasIndex(l => new { l.ApplicationProfileId, l.Sequence })
                    .HasDatabaseName("IX_ApplicationProfileApprovalLegs_Profile_Sequence");
            });

            modelBuilder.Entity<ApplicationProfileTemplate>(b =>
            {
                b.Property(t => t.TemplateName).HasMaxLength(255);
                b.Property(t => t.CategoryKey).HasMaxLength(64);
                b.Property(t => t.RecycledByUserName).HasMaxLength(255);
                b.Property(t => t.CreatedByUserName).HasMaxLength(255);
                b.Property(t => t.ModifiedByUserName).HasMaxLength(255);
                b.HasOne(t => t.ApplicationProfile)
                    .WithMany(p => p.NestedTemplates)
                    .HasForeignKey(t => t.ApplicationProfileId)
                    .OnDelete(DeleteBehavior.Cascade);
                b.HasOne(t => t.ApplicableProjectContract)
                    .WithMany()
                    .HasForeignKey(t => t.ApplicableProjectContractId)
                    .OnDelete(DeleteBehavior.SetNull);
                b.HasOne(t => t.ApplicableMigrationService)
                    .WithMany()
                    .HasForeignKey(t => t.ApplicableMigrationServiceId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<ApplicationProfileProgressStateSetting>(b =>
            {
                b.Property(s => s.StateCode).HasMaxLength(64);
                b.HasOne(s => s.ApplicationProfile)
                    .WithMany(p => p.ProgressStateSettings)
                    .HasForeignKey(s => s.ApplicationProfileId)
                    .OnDelete(DeleteBehavior.Cascade);
                b.HasIndex(s => new { s.ApplicationProfileId, s.Track, s.StateCode })
                    .HasDatabaseName("IX_ApplicationProfileProgressStateSettings_Profile_Track_Code");
            });

            modelBuilder.Entity<ApplicationProfileInstance>()
                .HasMany(a => a.People)
                .WithMany(p => p.ApplicationProfileInstances)
                .UsingEntity<Dictionary<string, object>>(
                    "ApplicationProfileInstancePeople",
                    right => right.HasOne<Person>()
                        .WithMany()
                        .HasForeignKey("PersonId")
                        .OnDelete(DeleteBehavior.Restrict),
                    left => left.HasOne<ApplicationProfileInstance>()
                        .WithMany()
                        .HasForeignKey("ApplicationProfileInstanceId")
                        .OnDelete(DeleteBehavior.Cascade),
                    join =>
                    {
                        join.ToTable("ApplicationProfileInstancePeople");
                        join.HasKey("ApplicationProfileInstanceId", "PersonId");
                    });

            ConfigureInstanceChildSkipNav(modelBuilder, a => a.Passports, p => p.ApplicationProfileInstances, "ApplicationProfileInstancePassports", "PassportId");
            ConfigureInstanceChildSkipNav(modelBuilder, a => a.Visas, v => v.ApplicationProfileInstances, "ApplicationProfileInstanceVisas", "VisaId");
            ConfigureInstanceChildSkipNav(modelBuilder, a => a.Educations, e => e.ApplicationProfileInstances, "ApplicationProfileInstanceEducations", "EducationId");
            ConfigureInstanceChildSkipNav(modelBuilder, a => a.AddressesOfResidence, aor => aor.ApplicationProfileInstances, "ApplicationProfileInstanceAddressesOfResidence", "AddressOfResidenceId");
            ConfigureInstanceChildSkipNav(modelBuilder, a => a.PositionHistories, p => p.ApplicationProfileInstances, "ApplicationProfileInstanceEmployeePositionHistories", "EmployeePositionHistoryId");
            ConfigureInstanceChildSkipNav(modelBuilder, a => a.Salaries, s => s.ApplicationProfileInstances, "ApplicationProfileInstanceEmployeeSalaries", "EmployeeSalaryId");
            ConfigureInstanceChildSkipNav(modelBuilder, a => a.MedicalRecords, m => m.ApplicationProfileInstances, "ApplicationProfileInstanceMedicalRecords", "MedicalRecordId");
            ConfigureInstanceChildSkipNav(modelBuilder, a => a.WorkDuties, w => w.ApplicationProfileInstances, "ApplicationProfileInstanceWorkDuties", "WorkDutyId");
            ConfigureInstanceChildSkipNav(modelBuilder, a => a.InvitationItems, i => i.ApplicationProfileInstances, "ApplicationProfileInstanceInvitationItems", "InvitationItemId");
            ConfigureInstanceChildSkipNav(modelBuilder, a => a.WorkPermitItems, w => w.ApplicationProfileInstances, "ApplicationProfileInstanceWorkPermitItems", "WorkPermitItemId");
            ConfigureInstanceChildSkipNav(modelBuilder, a => a.BorderZoneItems, b => b.ApplicationProfileInstances, "ApplicationProfileInstanceBorderZoneItems", "BorderZoneItemId");
            ConfigureInstanceChildSkipNav(modelBuilder, a => a.TravelHistories, t => t.ApplicationProfileInstances, "ApplicationProfileInstanceTravelHistories", "TravelHistoryId");

            modelBuilder.Entity<Invitation>(b =>
            {
                b.HasOne(x => x.ApplicationProfileInstance)
                    .WithMany(a => a.Invitations)
                    .HasForeignKey("ApplicationProfileInstanceID")
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.NoAction);
                b.Property(x => x.BorderZoneLocation).HasMaxLength(500);
            });

            modelBuilder.Entity<WorkPermit>(b =>
            {
                b.HasOne(x => x.ApplicationProfileInstance)
                    .WithMany(a => a.WorkPermits)
                    .HasForeignKey("ApplicationProfileInstanceID")
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<BorderZone>(b =>
            {
                b.HasOne(x => x.ApplicationProfileInstance)
                    .WithMany(a => a.BorderZones)
                    .HasForeignKey("ApplicationProfileInstanceID")
                    .IsRequired()
                    .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<Rejection>(b =>
            {
                b.HasOne(x => x.ApplicationProfileInstance)
                    .WithMany(a => a.Rejections)
                    .HasForeignKey("ApplicationProfileInstanceID")
                    .IsRequired()
                    .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<ApplicationProfileInstancePersonResolvedLink>(b =>
            {
                b.HasOne(l => l.ApplicationProfileInstance)
                    .WithMany(a => a.PersonResolvedLinks)
                    .HasForeignKey(l => l.ApplicationProfileInstanceId)
                    .OnDelete(DeleteBehavior.Cascade);
                b.HasOne(l => l.Person)
                    .WithMany()
                    .HasForeignKey(l => l.PersonId)
                    .OnDelete(DeleteBehavior.Restrict);
                b.HasIndex(l => new { l.ApplicationProfileInstanceId, l.PersonId, l.LinkKind, l.LinkedObjectId })
                    .IsUnique()
                    .HasDatabaseName("IX_ApplicationProfileInstancePersonResolvedLinks_Instance_Person_Kind_Object");
            });

            modelBuilder.Entity<ApplicationProfileInstanceProgress>(b => {
                b.HasIndex("ApplicationProfileInstanceID", nameof(ApplicationProfileInstanceProgress.Order))
                 .HasDatabaseName("IX_ApplicationProfileInstanceProgresses_ApplicationProfileInstanceID_ProgressOrder");
            });

            modelBuilder.Entity<ApplicationProfileInstanceApprovalLegSnapshot>(b => {
                b.HasIndex(s => s.ApplicationProfileInstanceId)
                 .HasDatabaseName("IX_ApplicationProfileInstanceApprovalLegSnapshots_ApplicationProfileInstanceId");
            });

            modelBuilder.Entity<ApplicationType>(b => {
                b.Property(t => t.SelectionCode).HasMaxLength(3);
                b.HasIndex(t => t.SelectionCode)
                    .IsUnique()
                    .HasFilter(IndexFilter("[SelectionCode] IS NOT NULL AND [SelectionCode] <> ''"));
            });

            modelBuilder.Entity<ProjectContract>(b =>
            {
                b.Ignore(c => c.Name);
                b.Ignore(c => c.Code);
                b.HasOne(c => c.ApprovalLegProfile)
                    .WithMany(p => p.ProjectContracts)
                    .HasForeignKey(c => c.ApprovalLegProfileId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.SetNull);
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

            modelBuilder.Entity<WorkPermitItem>(b => {
                b.HasOne(wpi => wpi.Person).WithMany(p => p.WorkPermitItems).OnDelete(DeleteBehavior.NoAction);
                b.HasOne(wpi => wpi.Passport).WithMany().OnDelete(DeleteBehavior.NoAction);
                b.Property(wpi => wpi.WorkPermittedLocations).HasMaxLength(500);
            });

            modelBuilder.Entity<Visa>(b => {
                b.HasOne(v => v.Passport).WithMany(p => p.Visas).OnDelete(DeleteBehavior.NoAction);
                b.HasOne(v => v.IssuingApplicationProfileInstance)
                    .WithMany(a => a.IssuedVisas)
                    .HasForeignKey("IssuingApplicationProfileInstanceID")
                    .OnDelete(DeleteBehavior.NoAction)
                    .IsRequired(false);
                b.HasIndex("IssuingApplicationProfileInstanceID")
                    .HasDatabaseName("IX_Visas_IssuingApplicationProfileInstanceID");
                // Single-use by validation (Visa_IssuingInvitationItemSingleUse).
                b.HasOne(v => v.IssuingInvitationItem)
                    .WithOne(ii => ii.IssuedVisa)
                    .HasForeignKey<Visa>("IssuingInvitationItemID")
                    .OnDelete(DeleteBehavior.NoAction)
                    .IsRequired(false);
                b.Metadata.UseSqlOutputClause(false);
                b.Property(v => v.ExtensionRequired).HasDefaultValue(true);
                b.Property(v => v.BorderZoneLocation).HasMaxLength(500);
                b.Property(v => v.ProcessNumber).HasMaxLength(100);
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
                .HasFilter(IndexFilter("[PersonalNumber] IS NOT NULL AND [PersonalNumber] <> N'' AND [PersonalNumber] <> N'0'"));

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
                b.HasOne(x => x.ApplicationProfileInstance)
                    .WithMany()
                    .HasForeignKey(x => x.ApplicationProfileInstanceID)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<PersonExportBatch>(b =>
            {
                b.HasOne(x => x.Person)
                    .WithMany()
                    .HasForeignKey(x => x.PersonID)
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
                // Provider-agnostic: SQL Server → nvarchar(max), PostgreSQL → text.
                b.Property(x => x.StackTrace);
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

        private static void ConfigureInstanceChildSkipNav<TChild>(
            ModelBuilder modelBuilder,
            Expression<Func<ApplicationProfileInstance, IEnumerable<TChild>>> instanceCollection,
            Expression<Func<TChild, IEnumerable<ApplicationProfileInstance>>> childCollection,
            string tableName,
            string childFk)
            where TChild : class
        {
            modelBuilder.Entity<ApplicationProfileInstance>()
                .HasMany(instanceCollection)
                .WithMany(childCollection)
                .UsingEntity<Dictionary<string, object>>(
                    tableName,
                    right => right.HasOne<TChild>()
                        .WithMany()
                        .HasForeignKey(childFk)
                        .OnDelete(DeleteBehavior.Restrict),
                    left => left.HasOne<ApplicationProfileInstance>()
                        .WithMany()
                        .HasForeignKey("ApplicationProfileInstanceId")
                        .OnDelete(DeleteBehavior.Cascade),
                    join =>
                    {
                        join.ToTable(tableName);
                        join.HasKey("ApplicationProfileInstanceId", childFk);
                    });
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