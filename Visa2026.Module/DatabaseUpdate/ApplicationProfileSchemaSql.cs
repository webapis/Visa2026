using System;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Idempotent SQL for <see cref="BusinessObjects.ApplicationProfile"/> tables and
/// <see cref="BusinessObjects.ApplicationProfileInstance.ApplicationProfile"/> FK column.
/// Host-start heal when ModuleUpdater is skipped (ModuleInfo already current).
/// </summary>
public static class ApplicationProfileSchemaSql
{
    internal const string EnsureSchemaPostgres = """
        CREATE TABLE IF NOT EXISTS "ApplicationProfiles" (
            "ID" uuid NOT NULL,
            "GCRecord" integer NOT NULL DEFAULT 0,
            "OptimisticLockField" integer NOT NULL DEFAULT 0,
            "Name" character varying(200) NOT NULL DEFAULT '',
            "Description" character varying(1000) NULL,
            "Code" character varying(64) NOT NULL DEFAULT '',
            "SelectionCode" character varying(3) NULL,
            "ProgressRoute" integer NOT NULL DEFAULT 0,
            "ForEmployee" boolean NOT NULL DEFAULT true,
            "ForFamilyMember" boolean NOT NULL DEFAULT false,
            "ForTemporaryVisitor" boolean NOT NULL DEFAULT false,
            "ActionFamily" integer NOT NULL DEFAULT 0,
            "RegistrationKind" integer NOT NULL DEFAULT 0,
            "ProduceInvitation" boolean NOT NULL DEFAULT false,
            "ProduceWorkPermit" boolean NOT NULL DEFAULT false,
            "ProduceVisa" boolean NOT NULL DEFAULT false,
            "ProduceBorderZone" boolean NOT NULL DEFAULT false,
            "ProduceWorkLocation" boolean NOT NULL DEFAULT false,
            "ProduceRejection" boolean NOT NULL DEFAULT false,
            "CancelInvitations" boolean NOT NULL DEFAULT false,
            "CancelWorkPermits" boolean NOT NULL DEFAULT false,
            "CancelVisas" boolean NOT NULL DEFAULT false,
            "CancelBorderZonePermits" boolean NOT NULL DEFAULT false,
            "CancelApplicationProfileInstances" boolean NOT NULL DEFAULT false,
            "ChangeInvitations" boolean NOT NULL DEFAULT false,
            "ChangeWorkPermits" boolean NOT NULL DEFAULT false,
            "ChangeVisas" boolean NOT NULL DEFAULT false,
            "ChangeBorderZonePermits" boolean NOT NULL DEFAULT false,
            "ChangeApplicationProfileInstances" boolean NOT NULL DEFAULT false,
            "RequireVisaType" boolean NOT NULL DEFAULT false,
            "DefaultVisaTypeId" uuid NULL,
            "RequireVisaCategory" boolean NOT NULL DEFAULT false,
            "DefaultVisaCategoryId" uuid NULL,
            "RequireVisaPeriod" boolean NOT NULL DEFAULT false,
            "DefaultVisaPeriodId" uuid NULL,
            "RequireBorderZone" boolean NOT NULL DEFAULT false,
            "DefaultBorderZoneLocation" character varying(500) NULL,
            "RequireMigrationService" boolean NOT NULL DEFAULT false,
            "DefaultMigrationServiceId" uuid NULL,
            "RequireStartDate" boolean NOT NULL DEFAULT false,
            "RequireEndDate" boolean NOT NULL DEFAULT false,
            "RequireRegion" boolean NOT NULL DEFAULT false,
            "DefaultRegionId" uuid NULL,
            "RequireCity" boolean NOT NULL DEFAULT false,
            "DefaultCityId" uuid NULL,
            "RequireRegionCity" boolean NOT NULL DEFAULT false,
            "RequireBusinessTripAddress" boolean NOT NULL DEFAULT false,
            "DefaultBusinessTripAddressId" uuid NULL,
            "RequirePurpose" boolean NOT NULL DEFAULT false,
            "DefaultPurpose" character varying(700) NULL,
            "RequireProject" boolean NOT NULL DEFAULT false,
            "DefaultProjectContractId" uuid NULL,
            "RequireUrgency" boolean NOT NULL DEFAULT false,
            "DefaultUrgencyId" uuid NULL,
            "RequireWorkPermitLocation" boolean NOT NULL DEFAULT false,
            "DefaultWorkPermitLocation" character varying(500) NULL,
            "RequireProcessNumber" boolean NOT NULL DEFAULT false,
            "RequireEntryDate" boolean NOT NULL DEFAULT false,
            "RequireEntryCheckPoint" boolean NOT NULL DEFAULT false,
            "DefaultEntryCheckPointId" uuid NULL,
            "DefaultAuthorizedSignatoryId" uuid NULL,
            "DefaultVisaRepresentativeId" uuid NULL,
            "MinistrySlaDays" integer NOT NULL DEFAULT 14,
            "MigrationSlaDays" integer NOT NULL DEFAULT 14,
            "RequirePersonPassport" boolean NOT NULL DEFAULT true,
            "RequirePersonEducation" boolean NOT NULL DEFAULT false,
            "RequirePersonPosition" boolean NOT NULL DEFAULT false,
            "RequirePersonAddressOfResidence" boolean NOT NULL DEFAULT false,
            "RequirePersonVisa" boolean NOT NULL DEFAULT false,
            "RequirePersonInvitationItem" boolean NOT NULL DEFAULT false,
            "RequirePersonWorkPermitItem" boolean NOT NULL DEFAULT false,
            "RequirePersonBorderZoneItem" boolean NOT NULL DEFAULT false,
            "RequirePersonSalary" boolean NOT NULL DEFAULT false,
            "RequirePersonMedical" boolean NOT NULL DEFAULT false,
            "RequirePersonRejectionItem" boolean NOT NULL DEFAULT false,
            "RequirePersonTravelHistory" boolean NOT NULL DEFAULT false,
            "PersonPassportLastCount" integer NOT NULL DEFAULT 1,
            "PersonVisaLastCount" integer NOT NULL DEFAULT 1,
            "PersonInvitationItemLastCount" integer NOT NULL DEFAULT 1,
            "PersonWorkPermitItemLastCount" integer NOT NULL DEFAULT 1,
            "PersonBorderZoneItemLastCount" integer NOT NULL DEFAULT 1,
            "ApplicabilityCriteria" text NULL,
            "IsActive" boolean NOT NULL DEFAULT true,
            CONSTRAINT "PK_ApplicationProfiles" PRIMARY KEY ("ID")
        );

        DO $$
        BEGIN
          IF to_regclass('public."ApplicationProfileApprovalLegs"') IS NULL
             AND to_regclass('public."ApplicationProfiles"') IS NOT NULL THEN
            CREATE TABLE "ApplicationProfileApprovalLegs" (
                "ID" uuid NOT NULL,
                "GCRecord" integer NOT NULL DEFAULT 0,
                "OptimisticLockField" integer NOT NULL DEFAULT 0,
                "ApplicationProfileId" uuid NOT NULL,
                "Sequence" integer NULL,
                "ApprovingMinistryId" uuid NULL,
                CONSTRAINT "PK_ApplicationProfileApprovalLegs" PRIMARY KEY ("ID"),
                CONSTRAINT "FK_ApplicationProfileApprovalLegs_ApplicationProfiles_ApplicationProfileId"
                    FOREIGN KEY ("ApplicationProfileId") REFERENCES "ApplicationProfiles" ("ID") ON DELETE CASCADE
            );
            CREATE INDEX "IX_ApplicationProfileApprovalLegs_Profile_Sequence"
                ON "ApplicationProfileApprovalLegs" ("ApplicationProfileId", "Sequence");
          END IF;

          IF to_regclass('public."ApplicationProfileTemplates"') IS NULL
             AND to_regclass('public."ApplicationProfiles"') IS NOT NULL THEN
            CREATE TABLE "ApplicationProfileTemplates" (
                "ID" uuid NOT NULL,
                "GCRecord" integer NOT NULL DEFAULT 0,
                "OptimisticLockField" integer NOT NULL DEFAULT 0,
                "ApplicationProfileId" uuid NOT NULL,
                "TemplateName" character varying(255) NOT NULL DEFAULT '',
                "TemplateKind" integer NOT NULL DEFAULT 0,
                "CatalogScope" integer NOT NULL DEFAULT 0,
                "DataScope" integer NOT NULL DEFAULT 1,
                "CategoryKey" character varying(64) NULL,
                "TemplateFileID" uuid NULL,
                "SortOrder" integer NOT NULL DEFAULT 0,
                "ApplicableProjectContractId" uuid NULL,
                "ApplicableMigrationServiceId" uuid NULL,
                CONSTRAINT "PK_ApplicationProfileTemplates" PRIMARY KEY ("ID"),
                CONSTRAINT "FK_ApplicationProfileTemplates_ApplicationProfiles_ApplicationProfileId"
                    FOREIGN KEY ("ApplicationProfileId") REFERENCES "ApplicationProfiles" ("ID") ON DELETE CASCADE
            );
          END IF;

          IF to_regclass('public."ApplicationProfileProgressStateSettings"') IS NULL
             AND to_regclass('public."ApplicationProfiles"') IS NOT NULL THEN
            CREATE TABLE "ApplicationProfileProgressStateSettings" (
                "ID" uuid NOT NULL,
                "GCRecord" integer NOT NULL DEFAULT 0,
                "OptimisticLockField" integer NOT NULL DEFAULT 0,
                "ApplicationProfileId" uuid NOT NULL,
                "Track" integer NOT NULL DEFAULT 0,
                "StateCode" character varying(64) NOT NULL DEFAULT '',
                "IsIncluded" boolean NOT NULL DEFAULT true,
                "IsSlaTracked" boolean NOT NULL DEFAULT false,
                CONSTRAINT "PK_ApplicationProfileProgressStateSettings" PRIMARY KEY ("ID"),
                CONSTRAINT "FK_ApplicationProfileProgressStateSettings_ApplicationProfiles_ApplicationProfileId"
                    FOREIGN KEY ("ApplicationProfileId") REFERENCES "ApplicationProfiles" ("ID") ON DELETE CASCADE
            );
            CREATE INDEX "IX_ApplicationProfileProgressStateSettings_Profile_Track_Code"
                ON "ApplicationProfileProgressStateSettings" ("ApplicationProfileId", "Track", "StateCode");
          END IF;

          IF to_regclass('public."ApplicationProfileInstances"') IS NULL THEN
            RETURN;
          END IF;

          ALTER TABLE "ApplicationProfileInstances" ADD COLUMN IF NOT EXISTS "ApplicationProfileID" uuid NULL;

          IF NOT EXISTS (
            SELECT 1 FROM pg_indexes
            WHERE schemaname = 'public'
              AND indexname = 'IX_Applications_ApplicationProfileID') THEN
            CREATE INDEX "IX_Applications_ApplicationProfileID"
                ON "ApplicationProfileInstances" ("ApplicationProfileID");
          END IF;

          IF to_regclass('public."ApplicationProfiles"') IS NOT NULL
             AND NOT EXISTS (
               SELECT 1 FROM pg_constraint
               WHERE conname = 'FK_Applications_ApplicationProfiles_ApplicationProfileID') THEN
            ALTER TABLE "ApplicationProfileInstances"
                ADD CONSTRAINT "FK_Applications_ApplicationProfiles_ApplicationProfileID"
                FOREIGN KEY ("ApplicationProfileID") REFERENCES "ApplicationProfiles" ("ID");
          END IF;
        END $$;
        """;

    internal const string EnsureSchemaSqlServer = """
        IF OBJECT_ID(N'dbo.ApplicationProfiles', N'U') IS NULL
        BEGIN
            CREATE TABLE dbo.ApplicationProfiles (
                ID uniqueidentifier NOT NULL CONSTRAINT PK_ApplicationProfiles PRIMARY KEY,
                GCRecord int NOT NULL CONSTRAINT DF_ApplicationProfiles_GCRecord DEFAULT (0),
                OptimisticLockField int NOT NULL CONSTRAINT DF_ApplicationProfiles_OLF DEFAULT (0),
                Name nvarchar(200) NOT NULL CONSTRAINT DF_ApplicationProfiles_Name DEFAULT (N''),
                Description nvarchar(1000) NULL,
                Code nvarchar(64) NOT NULL CONSTRAINT DF_ApplicationProfiles_Code DEFAULT (N''),
                SelectionCode nvarchar(3) NULL,
                ProgressRoute int NOT NULL CONSTRAINT DF_ApplicationProfiles_ProgressRoute DEFAULT (0),
                ForEmployee bit NOT NULL CONSTRAINT DF_ApplicationProfiles_ForEmployee DEFAULT (1),
                ForFamilyMember bit NOT NULL CONSTRAINT DF_ApplicationProfiles_ForFamilyMember DEFAULT (0),
                ForTemporaryVisitor bit NOT NULL CONSTRAINT DF_ApplicationProfiles_ForTemporaryVisitor DEFAULT (0),
                ActionFamily int NOT NULL CONSTRAINT DF_ApplicationProfiles_ActionFamily DEFAULT (0),
                RegistrationKind int NOT NULL CONSTRAINT DF_ApplicationProfiles_RegistrationKind DEFAULT (0),
                ProduceInvitation bit NOT NULL CONSTRAINT DF_ApplicationProfiles_ProduceInvitation DEFAULT (0),
                ProduceWorkPermit bit NOT NULL CONSTRAINT DF_ApplicationProfiles_ProduceWorkPermit DEFAULT (0),
                ProduceVisa bit NOT NULL CONSTRAINT DF_ApplicationProfiles_ProduceVisa DEFAULT (0),
                ProduceBorderZone bit NOT NULL CONSTRAINT DF_ApplicationProfiles_ProduceBorderZone DEFAULT (0),
                ProduceWorkLocation bit NOT NULL CONSTRAINT DF_ApplicationProfiles_ProduceWorkLocation DEFAULT (0),
                ProduceRejection bit NOT NULL CONSTRAINT DF_ApplicationProfiles_ProduceRejection DEFAULT (0),
                CancelInvitations bit NOT NULL CONSTRAINT DF_ApplicationProfiles_CancelInvitations DEFAULT (0),
                CancelWorkPermits bit NOT NULL CONSTRAINT DF_ApplicationProfiles_CancelWorkPermits DEFAULT (0),
                CancelVisas bit NOT NULL CONSTRAINT DF_ApplicationProfiles_CancelVisas DEFAULT (0),
                CancelBorderZonePermits bit NOT NULL CONSTRAINT DF_ApplicationProfiles_CancelBorderZonePermits DEFAULT (0),
                CancelApplicationProfileInstances bit NOT NULL CONSTRAINT DF_ApplicationProfiles_CancelApplicationProfileInstances DEFAULT (0),
                RequireVisaType bit NOT NULL CONSTRAINT DF_ApplicationProfiles_RequireVisaType DEFAULT (0),
                DefaultVisaTypeId uniqueidentifier NULL,
                RequireVisaCategory bit NOT NULL CONSTRAINT DF_ApplicationProfiles_RequireVisaCategory DEFAULT (0),
                DefaultVisaCategoryId uniqueidentifier NULL,
                RequireVisaPeriod bit NOT NULL CONSTRAINT DF_ApplicationProfiles_RequireVisaPeriod DEFAULT (0),
                DefaultVisaPeriodId uniqueidentifier NULL,
                RequireBorderZone bit NOT NULL CONSTRAINT DF_ApplicationProfiles_RequireBorderZone DEFAULT (0),
                DefaultBorderZoneLocation nvarchar(500) NULL,
                RequireMigrationService bit NOT NULL CONSTRAINT DF_ApplicationProfiles_RequireMigrationService DEFAULT (0),
                DefaultMigrationServiceId uniqueidentifier NULL,
                RequireStartDate bit NOT NULL CONSTRAINT DF_ApplicationProfiles_RequireStartDate DEFAULT (0),
                RequireEndDate bit NOT NULL CONSTRAINT DF_ApplicationProfiles_RequireEndDate DEFAULT (0),
                RequireRegion bit NOT NULL CONSTRAINT DF_ApplicationProfiles_RequireRegion DEFAULT (0),
                DefaultRegionId uniqueidentifier NULL,
                RequireCity bit NOT NULL CONSTRAINT DF_ApplicationProfiles_RequireCity DEFAULT (0),
                DefaultCityId uniqueidentifier NULL,
                RequireRegionCity bit NOT NULL CONSTRAINT DF_ApplicationProfiles_RequireRegionCity DEFAULT (0),
                RequireBusinessTripAddress bit NOT NULL CONSTRAINT DF_ApplicationProfiles_RequireBusinessTripAddress DEFAULT (0),
                DefaultBusinessTripAddressId uniqueidentifier NULL,
                RequirePurpose bit NOT NULL CONSTRAINT DF_ApplicationProfiles_RequirePurpose DEFAULT (0),
                DefaultPurpose nvarchar(700) NULL,
                RequireProject bit NOT NULL CONSTRAINT DF_ApplicationProfiles_RequireProject DEFAULT (0),
                DefaultProjectContractId uniqueidentifier NULL,
                RequireUrgency bit NOT NULL CONSTRAINT DF_ApplicationProfiles_RequireUrgency DEFAULT (0),
                DefaultUrgencyId uniqueidentifier NULL,
                RequireWorkPermitLocation bit NOT NULL CONSTRAINT DF_ApplicationProfiles_RequireWorkPermitLocation DEFAULT (0),
                RequireProcessNumber bit NOT NULL CONSTRAINT DF_ApplicationProfiles_RequireProcessNumber DEFAULT (0),
                DefaultWorkPermitLocation nvarchar(500) NULL,
                RequireEntryDate bit NOT NULL CONSTRAINT DF_ApplicationProfiles_RequireEntryDate DEFAULT (0),
                RequireEntryCheckPoint bit NOT NULL CONSTRAINT DF_ApplicationProfiles_RequireEntryCheckPoint DEFAULT (0),
                DefaultEntryCheckPointId uniqueidentifier NULL,
                DefaultAuthorizedSignatoryId uniqueidentifier NULL,
                DefaultVisaRepresentativeId uniqueidentifier NULL,
                MinistrySlaDays int NOT NULL CONSTRAINT DF_ApplicationProfiles_MinistrySlaDays DEFAULT (14),
                MigrationSlaDays int NOT NULL CONSTRAINT DF_ApplicationProfiles_MigrationSlaDays DEFAULT (14),
                RequirePersonPassport bit NOT NULL CONSTRAINT DF_ApplicationProfiles_RequirePersonPassport DEFAULT (1),
                RequirePersonEducation bit NOT NULL CONSTRAINT DF_ApplicationProfiles_RequirePersonEducation DEFAULT (0),
                RequirePersonPosition bit NOT NULL CONSTRAINT DF_ApplicationProfiles_RequirePersonPosition DEFAULT (0),
                RequirePersonAddressOfResidence bit NOT NULL CONSTRAINT DF_ApplicationProfiles_RequirePersonAddressOfResidence DEFAULT (0),
                RequirePersonVisa bit NOT NULL CONSTRAINT DF_ApplicationProfiles_RequirePersonVisa DEFAULT (0),
                RequirePersonInvitationItem bit NOT NULL CONSTRAINT DF_ApplicationProfiles_RequirePersonInvitationItem DEFAULT (0),
                RequirePersonWorkPermitItem bit NOT NULL CONSTRAINT DF_ApplicationProfiles_RequirePersonWorkPermitItem DEFAULT (0),
                RequirePersonBorderZoneItem bit NOT NULL CONSTRAINT DF_ApplicationProfiles_RequirePersonBorderZoneItem DEFAULT (0),
                RequirePersonSalary bit NOT NULL CONSTRAINT DF_ApplicationProfiles_RequirePersonSalary DEFAULT (0),
                RequirePersonMedical bit NOT NULL CONSTRAINT DF_ApplicationProfiles_RequirePersonMedical DEFAULT (0),
                RequirePersonRejectionItem bit NOT NULL CONSTRAINT DF_ApplicationProfiles_RequirePersonRejectionItem DEFAULT (0),
                RequirePersonTravelHistory bit NOT NULL CONSTRAINT DF_ApplicationProfiles_RequirePersonTravelHistory DEFAULT (0),
                PersonPassportLastCount int NOT NULL CONSTRAINT DF_ApplicationProfiles_PersonPassportLastCount DEFAULT (1),
                PersonVisaLastCount int NOT NULL CONSTRAINT DF_ApplicationProfiles_PersonVisaLastCount DEFAULT (1),
                PersonInvitationItemLastCount int NOT NULL CONSTRAINT DF_ApplicationProfiles_PersonInvitationItemLastCount DEFAULT (1),
                PersonWorkPermitItemLastCount int NOT NULL CONSTRAINT DF_ApplicationProfiles_PersonWorkPermitItemLastCount DEFAULT (1),
                PersonBorderZoneItemLastCount int NOT NULL CONSTRAINT DF_ApplicationProfiles_PersonBorderZoneItemLastCount DEFAULT (1),
                ApplicabilityCriteria nvarchar(max) NULL,
                IsActive bit NOT NULL CONSTRAINT DF_ApplicationProfiles_IsActive DEFAULT (1)
            );
        END;

        IF OBJECT_ID(N'dbo.ApplicationProfileApprovalLegs', N'U') IS NULL
           AND OBJECT_ID(N'dbo.ApplicationProfiles', N'U') IS NOT NULL
        BEGIN
            CREATE TABLE dbo.ApplicationProfileApprovalLegs (
                ID uniqueidentifier NOT NULL CONSTRAINT PK_ApplicationProfileApprovalLegs PRIMARY KEY,
                GCRecord int NOT NULL CONSTRAINT DF_ApplicationProfileApprovalLegs_GCRecord DEFAULT (0),
                OptimisticLockField int NOT NULL CONSTRAINT DF_ApplicationProfileApprovalLegs_OLF DEFAULT (0),
                ApplicationProfileId uniqueidentifier NOT NULL,
                Sequence int NULL,
                ApprovingMinistryId uniqueidentifier NULL,
                CONSTRAINT FK_ApplicationProfileApprovalLegs_ApplicationProfiles_ApplicationProfileId
                    FOREIGN KEY (ApplicationProfileId) REFERENCES dbo.ApplicationProfiles(ID) ON DELETE CASCADE
            );
            CREATE INDEX IX_ApplicationProfileApprovalLegs_Profile_Sequence
                ON dbo.ApplicationProfileApprovalLegs (ApplicationProfileId, Sequence);
        END;

        IF OBJECT_ID(N'dbo.ApplicationProfileTemplates', N'U') IS NULL
           AND OBJECT_ID(N'dbo.ApplicationProfiles', N'U') IS NOT NULL
        BEGIN
            CREATE TABLE dbo.ApplicationProfileTemplates (
                ID uniqueidentifier NOT NULL CONSTRAINT PK_ApplicationProfileTemplates PRIMARY KEY,
                GCRecord int NOT NULL CONSTRAINT DF_ApplicationProfileTemplates_GCRecord DEFAULT (0),
                OptimisticLockField int NOT NULL CONSTRAINT DF_ApplicationProfileTemplates_OLF DEFAULT (0),
                ApplicationProfileId uniqueidentifier NOT NULL,
                TemplateName nvarchar(255) NOT NULL CONSTRAINT DF_ApplicationProfileTemplates_TemplateName DEFAULT (N''),
                TemplateKind int NOT NULL CONSTRAINT DF_ApplicationProfileTemplates_TemplateKind DEFAULT (0),
                CatalogScope int NOT NULL CONSTRAINT DF_ApplicationProfileTemplates_CatalogScope DEFAULT (0),
                DataScope int NOT NULL CONSTRAINT DF_ApplicationProfileTemplates_DataScope DEFAULT (1),
                CategoryKey nvarchar(64) NULL,
                TemplateFileID uniqueidentifier NULL,
                SortOrder int NOT NULL CONSTRAINT DF_ApplicationProfileTemplates_SortOrder DEFAULT (0),
                ApplicableProjectContractId uniqueidentifier NULL,
                ApplicableMigrationServiceId uniqueidentifier NULL,
                CONSTRAINT FK_ApplicationProfileTemplates_ApplicationProfiles_ApplicationProfileId
                    FOREIGN KEY (ApplicationProfileId) REFERENCES dbo.ApplicationProfiles(ID) ON DELETE CASCADE
            );
        END;

        IF OBJECT_ID(N'dbo.ApplicationProfileProgressStateSettings', N'U') IS NULL
           AND OBJECT_ID(N'dbo.ApplicationProfiles', N'U') IS NOT NULL
        BEGIN
            CREATE TABLE dbo.ApplicationProfileProgressStateSettings (
                ID uniqueidentifier NOT NULL CONSTRAINT PK_ApplicationProfileProgressStateSettings PRIMARY KEY,
                GCRecord int NOT NULL CONSTRAINT DF_ApplicationProfileProgressStateSettings_GCRecord DEFAULT (0),
                OptimisticLockField int NOT NULL CONSTRAINT DF_ApplicationProfileProgressStateSettings_OLF DEFAULT (0),
                ApplicationProfileId uniqueidentifier NOT NULL,
                Track int NOT NULL CONSTRAINT DF_ApplicationProfileProgressStateSettings_Track DEFAULT (0),
                StateCode nvarchar(64) NOT NULL CONSTRAINT DF_ApplicationProfileProgressStateSettings_StateCode DEFAULT (N''),
                IsIncluded bit NOT NULL CONSTRAINT DF_ApplicationProfileProgressStateSettings_IsIncluded DEFAULT (1),
                IsSlaTracked bit NOT NULL CONSTRAINT DF_ApplicationProfileProgressStateSettings_IsSlaTracked DEFAULT (0),
                CONSTRAINT FK_ApplicationProfileProgressStateSettings_ApplicationProfiles_ApplicationProfileId
                    FOREIGN KEY (ApplicationProfileId) REFERENCES dbo.ApplicationProfiles(ID) ON DELETE CASCADE
            );
            CREATE INDEX IX_ApplicationProfileProgressStateSettings_Profile_Track_Code
                ON dbo.ApplicationProfileProgressStateSettings (ApplicationProfileId, Track, StateCode);
        END;

        IF OBJECT_ID(N'dbo.ApplicationProfileInstances', N'U') IS NULL
            RETURN;

        IF COL_LENGTH(N'dbo.ApplicationProfileInstances', N'ApplicationProfileID') IS NULL
            ALTER TABLE dbo.ApplicationProfileInstances ADD ApplicationProfileID uniqueidentifier NULL;

        IF NOT EXISTS (
            SELECT 1 FROM sys.indexes
            WHERE name = N'IX_Applications_ApplicationProfileID'
              AND object_id = OBJECT_ID(N'dbo.ApplicationProfileInstances'))
            CREATE INDEX IX_Applications_ApplicationProfileID ON dbo.ApplicationProfileInstances (ApplicationProfileID);

        IF OBJECT_ID(N'dbo.ApplicationProfiles', N'U') IS NOT NULL
           AND NOT EXISTS (
               SELECT 1 FROM sys.foreign_keys
               WHERE name = N'FK_Applications_ApplicationProfiles_ApplicationProfileID')
            ALTER TABLE dbo.ApplicationProfileInstances WITH CHECK ADD CONSTRAINT FK_Applications_ApplicationProfiles_ApplicationProfileID
                FOREIGN KEY (ApplicationProfileID) REFERENCES dbo.ApplicationProfiles(ID);
        """;

    // Prefer ADD COLUMN IF NOT EXISTS (outside DO $$) so existing DBs get wizard CatalogScope/DataScope columns.
    internal const string EnsureTemplateCatalogScopePostgres =
        """ALTER TABLE "ApplicationProfileTemplates" ADD COLUMN IF NOT EXISTS "CatalogScope" integer NOT NULL DEFAULT 0;""";

    internal const string EnsureTemplateDataScopePostgres =
        """ALTER TABLE "ApplicationProfileTemplates" ADD COLUMN IF NOT EXISTS "DataScope" integer NOT NULL DEFAULT 1;""";

    internal const string EnsureTemplateCategoryKeyPostgres =
        """ALTER TABLE "ApplicationProfileTemplates" ADD COLUMN IF NOT EXISTS "CategoryKey" character varying(64) NULL;""";

    internal const string EnsureProduceRejectionPostgres =
        """ALTER TABLE "ApplicationProfiles" ADD COLUMN IF NOT EXISTS "ProduceRejection" boolean NOT NULL DEFAULT false;""";

    internal const string EnsureChangeInvitationsPostgres =
        """ALTER TABLE "ApplicationProfiles" ADD COLUMN IF NOT EXISTS "ChangeInvitations" boolean NOT NULL DEFAULT false;""";

    internal const string EnsureChangeWorkPermitsPostgres =
        """ALTER TABLE "ApplicationProfiles" ADD COLUMN IF NOT EXISTS "ChangeWorkPermits" boolean NOT NULL DEFAULT false;""";

    internal const string EnsureChangeVisasPostgres =
        """ALTER TABLE "ApplicationProfiles" ADD COLUMN IF NOT EXISTS "ChangeVisas" boolean NOT NULL DEFAULT false;""";

    internal const string EnsureChangeBorderZonePermitsPostgres =
        """ALTER TABLE "ApplicationProfiles" ADD COLUMN IF NOT EXISTS "ChangeBorderZonePermits" boolean NOT NULL DEFAULT false;""";

    internal const string EnsureChangeApplicationProfileInstancesPostgres =
        """ALTER TABLE "ApplicationProfiles" ADD COLUMN IF NOT EXISTS "ChangeApplicationProfileInstances" boolean NOT NULL DEFAULT false;""";

    internal const string EnsureRegistrationKindPostgres =
        """ALTER TABLE "ApplicationProfiles" ADD COLUMN IF NOT EXISTS "RegistrationKind" integer NOT NULL DEFAULT 0;""";

    internal const string EnsureRequireRegionPostgres =
        """ALTER TABLE "ApplicationProfiles" ADD COLUMN IF NOT EXISTS "RequireRegion" boolean NOT NULL DEFAULT false;""";

    internal const string EnsureDefaultRegionIdPostgres =
        """ALTER TABLE "ApplicationProfiles" ADD COLUMN IF NOT EXISTS "DefaultRegionId" uuid NULL;""";

    internal const string EnsureRequireCityPostgres =
        """ALTER TABLE "ApplicationProfiles" ADD COLUMN IF NOT EXISTS "RequireCity" boolean NOT NULL DEFAULT false;""";

    internal const string EnsureDefaultCityIdPostgres =
        """ALTER TABLE "ApplicationProfiles" ADD COLUMN IF NOT EXISTS "DefaultCityId" uuid NULL;""";

    internal const string HealRequireRegionCitySplitPostgres = """
        UPDATE "ApplicationProfiles"
        SET "RequireRegion" = TRUE, "RequireCity" = TRUE
        WHERE COALESCE("RequireRegionCity", FALSE) = TRUE
          AND COALESCE("RequireRegion", FALSE) = FALSE
          AND COALESCE("RequireCity", FALSE) = FALSE;
        """;

    internal const string EnsureInstanceRegionIdPostgres =
        """ALTER TABLE "ApplicationProfileInstances" ADD COLUMN IF NOT EXISTS "RegionId" uuid NULL;""";

    internal const string EnsureInstanceCityIdPostgres =
        """ALTER TABLE "ApplicationProfileInstances" ADD COLUMN IF NOT EXISTS "CityId" uuid NULL;""";

    internal const string EnsureDefaultBusinessTripAddressIdPostgres =
        """ALTER TABLE "ApplicationProfiles" ADD COLUMN IF NOT EXISTS "DefaultBusinessTripAddressId" uuid NULL;""";

    internal const string EnsureInstanceBusinessTripAddressIdPostgres =
        """ALTER TABLE "ApplicationProfileInstances" ADD COLUMN IF NOT EXISTS "BusinessTripAddressId" uuid NULL;""";

    internal const string EnsureRequirePurposePostgres =
        """ALTER TABLE "ApplicationProfiles" ADD COLUMN IF NOT EXISTS "RequirePurpose" boolean NOT NULL DEFAULT false;""";

    internal const string EnsureDefaultPurposePostgres =
        """ALTER TABLE "ApplicationProfiles" ADD COLUMN IF NOT EXISTS "DefaultPurpose" character varying(700) NULL;""";

    internal const string EnsureInstancePurposePostgres =
        """ALTER TABLE "ApplicationProfileInstances" ADD COLUMN IF NOT EXISTS "Purpose" character varying(700) NULL;""";

    internal const string EnsureOfficePreparationNotesPostgres =
        """ALTER TABLE "ApplicationProfileInstances" ADD COLUMN IF NOT EXISTS "OfficePreparationNotes" text NULL;""";

    internal const string EnsureInstanceEntryCheckPointPostgres =
        """ALTER TABLE "ApplicationProfileInstances" ADD COLUMN IF NOT EXISTS "EntryCheckPointID" uuid NULL;""";

    internal const string EnsureTemplateApplicableProjectContractPostgres =
        """ALTER TABLE "ApplicationProfileTemplates" ADD COLUMN IF NOT EXISTS "ApplicableProjectContractId" uuid NULL;""";

    internal const string EnsureTemplateApplicableMigrationServicePostgres =
        """ALTER TABLE "ApplicationProfileTemplates" ADD COLUMN IF NOT EXISTS "ApplicableMigrationServiceId" uuid NULL;""";

    internal const string EnsureApprovalLegVersionsTablePostgres = """
        CREATE TABLE IF NOT EXISTS "ApplicationProfileApprovalLegVersions" (
            "ID" uuid NOT NULL,
            "GCRecord" integer NOT NULL DEFAULT 0,
            "OptimisticLockField" integer NOT NULL DEFAULT 0,
            "ApplicationProfileId" uuid NOT NULL,
            "Name" character varying(200) NOT NULL DEFAULT 'Version 1',
            "IsDefault" boolean NOT NULL DEFAULT false,
            "Sequence" integer NOT NULL DEFAULT 1,
            CONSTRAINT "PK_ApplicationProfileApprovalLegVersions" PRIMARY KEY ("ID")
        );
        """;

    /// <summary>
    /// Existing DBs created the versions table with nullable <c>GCRecord</c>.
    /// XAF deferred deletion maps that column as non-nullable <c>int</c>; INSERT omits it,
    /// Postgres stores NULL, then RETURNING throws "Column 'GCRecord' is null."
    /// Match sibling Application Profile tables: NOT NULL DEFAULT 0.
    /// </summary>
    internal const string HealApprovalLegVersionsGcRecordPostgres = """
        DO $$
        BEGIN
          IF to_regclass('public."ApplicationProfileApprovalLegVersions"') IS NULL THEN
            RETURN;
          END IF;

          UPDATE "ApplicationProfileApprovalLegVersions" SET "GCRecord" = 0 WHERE "GCRecord" IS NULL;
          UPDATE "ApplicationProfileApprovalLegVersions" SET "OptimisticLockField" = 0 WHERE "OptimisticLockField" IS NULL;

          ALTER TABLE "ApplicationProfileApprovalLegVersions" ALTER COLUMN "GCRecord" SET DEFAULT 0;
          ALTER TABLE "ApplicationProfileApprovalLegVersions" ALTER COLUMN "OptimisticLockField" SET DEFAULT 0;
          ALTER TABLE "ApplicationProfileApprovalLegVersions" ALTER COLUMN "GCRecord" SET NOT NULL;
          ALTER TABLE "ApplicationProfileApprovalLegVersions" ALTER COLUMN "OptimisticLockField" SET NOT NULL;
        END $$;
        """;

    internal const string EnsureApprovalLegVersionsFkPostgres = """
        DO $$
        BEGIN
          IF to_regclass('public."ApplicationProfileApprovalLegVersions"') IS NULL
             OR to_regclass('public."ApplicationProfiles"') IS NULL THEN
            RETURN;
          END IF;

          IF NOT EXISTS (
            SELECT 1 FROM pg_constraint
            WHERE conname = 'FK_ApplicationProfileApprovalLegVersions_ApplicationProfiles_ApplicationProfileId') THEN
            ALTER TABLE "ApplicationProfileApprovalLegVersions"
              ADD CONSTRAINT "FK_ApplicationProfileApprovalLegVersions_ApplicationProfiles_ApplicationProfileId"
              FOREIGN KEY ("ApplicationProfileId") REFERENCES "ApplicationProfiles" ("ID") ON DELETE CASCADE;
          END IF;

          IF NOT EXISTS (
            SELECT 1 FROM pg_indexes
            WHERE schemaname = 'public'
              AND indexname = 'IX_ApplicationProfileApprovalLegVersions_Profile_Sequence') THEN
            CREATE INDEX "IX_ApplicationProfileApprovalLegVersions_Profile_Sequence"
              ON "ApplicationProfileApprovalLegVersions" ("ApplicationProfileId", "Sequence");
          END IF;
        END $$;
        """;

    internal const string EnsureApprovalLegVersionIdOnLegsPostgres =
        """ALTER TABLE "ApplicationProfileApprovalLegs" ADD COLUMN IF NOT EXISTS "ApprovalLegVersionId" uuid NULL;""";

    internal const string EnsureInstanceApprovalLegVersionNamePostgres =
        """ALTER TABLE "ApplicationProfileInstances" ADD COLUMN IF NOT EXISTS "ApprovalLegVersionName" character varying(200) NULL;""";

    internal const string EnsureInstanceApprovalLegVersionIdPostgres =
        """ALTER TABLE "ApplicationProfileInstances" ADD COLUMN IF NOT EXISTS "ApprovalLegVersionId" uuid NULL;""";


    internal const string EnsureDefaultApprovalLegProfilePostgres =
        """ALTER TABLE "ApplicationProfiles" ADD COLUMN IF NOT EXISTS "DefaultApprovalLegProfileId" uuid NULL;""";

    internal const string EnsurePersonPassportLastCountPostgres =
        """ALTER TABLE "ApplicationProfiles" ADD COLUMN IF NOT EXISTS "PersonPassportLastCount" integer NOT NULL DEFAULT 1;""";

    internal const string EnsurePersonVisaLastCountPostgres =
        """ALTER TABLE "ApplicationProfiles" ADD COLUMN IF NOT EXISTS "PersonVisaLastCount" integer NOT NULL DEFAULT 1;""";

    internal const string EnsurePersonInvitationItemLastCountPostgres =
        """ALTER TABLE "ApplicationProfiles" ADD COLUMN IF NOT EXISTS "PersonInvitationItemLastCount" integer NOT NULL DEFAULT 1;""";

    internal const string EnsurePersonWorkPermitItemLastCountPostgres =
        """ALTER TABLE "ApplicationProfiles" ADD COLUMN IF NOT EXISTS "PersonWorkPermitItemLastCount" integer NOT NULL DEFAULT 1;""";

    internal const string EnsurePersonBorderZoneItemLastCountPostgres =
        """ALTER TABLE "ApplicationProfiles" ADD COLUMN IF NOT EXISTS "PersonBorderZoneItemLastCount" integer NOT NULL DEFAULT 1;""";
    internal const string EnsureDefaultWorkPermitLocationPostgres =
        """ALTER TABLE "ApplicationProfiles" ADD COLUMN IF NOT EXISTS "DefaultWorkPermitLocation" character varying(500) NULL;""";

    internal const string EnsureRequireProcessNumberPostgres =
        """ALTER TABLE "ApplicationProfiles" ADD COLUMN IF NOT EXISTS "RequireProcessNumber" boolean NOT NULL DEFAULT false;""";

    /// <summary>
    /// Converts instance work-permit location from FK (<c>MovementPermitLocationID</c>) to
    /// comma-separated <c>MovementPermitLocation</c> text using <c>WorkPermittedLocationName</c>.
    /// </summary>
    internal const string ConvertInstanceMovementPermitLocationToStringPostgres = """
        DO $$
        DECLARE
          fk_col text;
          rec record;
        BEGIN
          IF to_regclass('public."ApplicationProfileInstances"') IS NULL THEN
            RETURN;
          END IF;

          IF EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_schema = 'public'
              AND table_name = 'ApplicationProfileInstances'
              AND column_name = 'MovementPermitLocationID'
              AND data_type = 'uuid') THEN
            fk_col := 'MovementPermitLocationID';
          ELSIF EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_schema = 'public'
              AND table_name = 'ApplicationProfileInstances'
              AND column_name = 'MovementPermitLocationId'
              AND data_type = 'uuid') THEN
            fk_col := 'MovementPermitLocationId';
          ELSIF EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_schema = 'public'
              AND table_name = 'ApplicationProfileInstances'
              AND column_name = 'MovementPermitLocation'
              AND udt_name = 'uuid') THEN
            ALTER TABLE "ApplicationProfileInstances" RENAME COLUMN "MovementPermitLocation" TO "MovementPermitLocationID";
            fk_col := 'MovementPermitLocationID';
          END IF;

          ALTER TABLE "ApplicationProfileInstances"
            ADD COLUMN IF NOT EXISTS "MovementPermitLocation" character varying(500) NULL;

          IF fk_col IS NOT NULL AND to_regclass('public."MovementPermitLocations"') IS NOT NULL THEN
            EXECUTE format(
              'UPDATE "ApplicationProfileInstances" a
               SET "MovementPermitLocation" = COALESCE(NULLIF(BTRIM(m."NameTm"), ''''), a."MovementPermitLocation")
               FROM "MovementPermitLocations" m
               WHERE m."ID" = a.%I
                 AND (a."MovementPermitLocation" IS NULL OR BTRIM(a."MovementPermitLocation") = '''')',
              fk_col);

            FOR rec IN
              SELECT c.conname
              FROM pg_constraint c
              JOIN pg_class t ON t.oid = c.conrelid
              JOIN pg_namespace n ON n.oid = t.relnamespace
              JOIN pg_attribute a ON a.attrelid = t.oid AND a.attnum = ANY (c.conkey)
              WHERE n.nspname = 'public'
                AND t.relname = 'ApplicationProfileInstances'
                AND a.attname = fk_col
                AND c.contype = 'f'
            LOOP
              EXECUTE format(
                'ALTER TABLE "ApplicationProfileInstances" DROP CONSTRAINT IF EXISTS %I',
                rec.conname);
            END LOOP;

            EXECUTE format(
              'ALTER TABLE "ApplicationProfileInstances" DROP COLUMN IF EXISTS %I CASCADE',
              fk_col);
          END IF;
        END $$;
        """;

    internal const string BackfillApprovalLegVersionsPostgres = """
        DO $$
        BEGIN
          IF to_regclass('public."ApplicationProfileApprovalLegVersions"') IS NULL
             OR to_regclass('public."ApplicationProfileApprovalLegs"') IS NULL THEN
            RETURN;
          END IF;

          INSERT INTO "ApplicationProfileApprovalLegVersions"
            ("ID", "GCRecord", "OptimisticLockField", "ApplicationProfileId", "Name", "IsDefault", "Sequence")
          SELECT gen_random_uuid(), 0, 0, p."ID", 'Version 1', true, 1
          FROM "ApplicationProfiles" p
          WHERE COALESCE(p."GCRecord", 0) = 0
            AND EXISTS (
              SELECT 1 FROM "ApplicationProfileApprovalLegs" l
              WHERE l."ApplicationProfileId" = p."ID"
                AND l."ApprovalLegVersionId" IS NULL
                AND COALESCE(l."GCRecord", 0) = 0)
            AND NOT EXISTS (
              SELECT 1 FROM "ApplicationProfileApprovalLegVersions" v
              WHERE v."ApplicationProfileId" = p."ID"
                AND COALESCE(v."GCRecord", 0) = 0);

          UPDATE "ApplicationProfileApprovalLegs" l
          SET "ApprovalLegVersionId" = v."ID"
          FROM "ApplicationProfileApprovalLegVersions" v
          WHERE l."ApplicationProfileId" = v."ApplicationProfileId"
            AND l."ApprovalLegVersionId" IS NULL
            AND COALESCE(l."GCRecord", 0) = 0
            AND COALESCE(v."GCRecord", 0) = 0
            AND v."IsDefault" = true;

          IF NOT EXISTS (
            SELECT 1 FROM pg_constraint
            WHERE conname = 'FK_ApplicationProfileApprovalLegs_Versions_ApprovalLegVersionId') THEN
            ALTER TABLE "ApplicationProfileApprovalLegs"
              ADD CONSTRAINT "FK_ApplicationProfileApprovalLegs_Versions_ApprovalLegVersionId"
              FOREIGN KEY ("ApprovalLegVersionId") REFERENCES "ApplicationProfileApprovalLegVersions" ("ID") ON DELETE SET NULL;
          END IF;
        END $$;
        """;

    internal static readonly string[] EnsureTemplateCatalogColumnsPostgresStatements =
    [
        EnsureTemplateCatalogScopePostgres,
        EnsureTemplateDataScopePostgres,
        EnsureTemplateCategoryKeyPostgres,
        EnsureProduceRejectionPostgres,
        EnsureChangeInvitationsPostgres,
        EnsureChangeWorkPermitsPostgres,
        EnsureChangeVisasPostgres,
        EnsureChangeBorderZonePermitsPostgres,
        EnsureChangeApplicationProfileInstancesPostgres,
        EnsureRegistrationKindPostgres,
        EnsureRequireRegionPostgres,
        EnsureDefaultRegionIdPostgres,
        EnsureRequireCityPostgres,
        EnsureDefaultCityIdPostgres,
        HealRequireRegionCitySplitPostgres,
        EnsureInstanceRegionIdPostgres,
        EnsureInstanceCityIdPostgres,
        EnsureDefaultBusinessTripAddressIdPostgres,
        EnsureInstanceBusinessTripAddressIdPostgres,
        EnsureRequirePurposePostgres,
        EnsureDefaultPurposePostgres,
        EnsureInstancePurposePostgres,
        EnsureOfficePreparationNotesPostgres,
        EnsureInstanceEntryCheckPointPostgres,
        EnsureTemplateApplicableProjectContractPostgres,
        EnsureTemplateApplicableMigrationServicePostgres,
        EnsureApprovalLegVersionsTablePostgres,
        HealApprovalLegVersionsGcRecordPostgres,
        EnsureApprovalLegVersionsFkPostgres,
        EnsureApprovalLegVersionIdOnLegsPostgres,
        EnsureInstanceApprovalLegVersionNamePostgres,
        EnsureInstanceApprovalLegVersionIdPostgres,
        BackfillApprovalLegVersionsPostgres,
        EnsureDefaultWorkPermitLocationPostgres,
        EnsureRequireProcessNumberPostgres,
        EnsureDefaultApprovalLegProfilePostgres,
        EnsurePersonPassportLastCountPostgres,
        EnsurePersonVisaLastCountPostgres,
        EnsurePersonInvitationItemLastCountPostgres,
        EnsurePersonWorkPermitItemLastCountPostgres,
        EnsurePersonBorderZoneItemLastCountPostgres,
        ConvertInstanceMovementPermitLocationToStringPostgres,
    ];

    internal const string EnsureTemplateCatalogColumnsSqlServer = """
        IF OBJECT_ID(N'dbo.ApplicationProfileTemplates', N'U') IS NULL
            RETURN;

        IF COL_LENGTH(N'dbo.ApplicationProfileTemplates', N'CatalogScope') IS NULL
            ALTER TABLE dbo.ApplicationProfileTemplates ADD CatalogScope int NOT NULL
                CONSTRAINT DF_ApplicationProfileTemplates_CatalogScope DEFAULT (0);

        IF COL_LENGTH(N'dbo.ApplicationProfileTemplates', N'DataScope') IS NULL
            ALTER TABLE dbo.ApplicationProfileTemplates ADD DataScope int NOT NULL
                CONSTRAINT DF_ApplicationProfileTemplates_DataScope DEFAULT (1);

        IF COL_LENGTH(N'dbo.ApplicationProfileTemplates', N'CategoryKey') IS NULL
            ALTER TABLE dbo.ApplicationProfileTemplates ADD CategoryKey nvarchar(64) NULL;

        IF COL_LENGTH(N'dbo.ApplicationProfileTemplates', N'ApplicableProjectContractId') IS NULL
            ALTER TABLE dbo.ApplicationProfileTemplates ADD ApplicableProjectContractId uniqueidentifier NULL;

        IF COL_LENGTH(N'dbo.ApplicationProfileTemplates', N'ApplicableMigrationServiceId') IS NULL
            ALTER TABLE dbo.ApplicationProfileTemplates ADD ApplicableMigrationServiceId uniqueidentifier NULL;

        IF OBJECT_ID(N'dbo.ApplicationProfiles', N'U') IS NOT NULL
           AND COL_LENGTH(N'dbo.ApplicationProfiles', N'ProduceRejection') IS NULL
            ALTER TABLE dbo.ApplicationProfiles ADD ProduceRejection bit NOT NULL
                CONSTRAINT DF_ApplicationProfiles_ProduceRejection DEFAULT (0);

        IF OBJECT_ID(N'dbo.ApplicationProfiles', N'U') IS NOT NULL
           AND COL_LENGTH(N'dbo.ApplicationProfiles', N'RegistrationKind') IS NULL
            ALTER TABLE dbo.ApplicationProfiles ADD RegistrationKind int NOT NULL
                CONSTRAINT DF_ApplicationProfiles_RegistrationKind DEFAULT (0);

        IF OBJECT_ID(N'dbo.ApplicationProfileInstances', N'U') IS NOT NULL
           AND COL_LENGTH(N'dbo.ApplicationProfileInstances', N'OfficePreparationNotes') IS NULL
            ALTER TABLE dbo.ApplicationProfileInstances ADD OfficePreparationNotes nvarchar(max) NULL;

        IF OBJECT_ID(N'dbo.ApplicationProfiles', N'U') IS NOT NULL
           AND COL_LENGTH(N'dbo.ApplicationProfiles', N'DefaultBusinessTripAddressId') IS NULL
            ALTER TABLE dbo.ApplicationProfiles ADD DefaultBusinessTripAddressId uniqueidentifier NULL;

        IF OBJECT_ID(N'dbo.ApplicationProfileInstances', N'U') IS NOT NULL
           AND COL_LENGTH(N'dbo.ApplicationProfileInstances', N'BusinessTripAddressId') IS NULL
            ALTER TABLE dbo.ApplicationProfileInstances ADD BusinessTripAddressId uniqueidentifier NULL;

        IF OBJECT_ID(N'dbo.ApplicationProfiles', N'U') IS NOT NULL
           AND COL_LENGTH(N'dbo.ApplicationProfiles', N'RequirePurpose') IS NULL
            ALTER TABLE dbo.ApplicationProfiles ADD RequirePurpose bit NOT NULL
                CONSTRAINT DF_ApplicationProfiles_RequirePurpose DEFAULT (0);

        IF OBJECT_ID(N'dbo.ApplicationProfiles', N'U') IS NOT NULL
           AND COL_LENGTH(N'dbo.ApplicationProfiles', N'DefaultPurpose') IS NULL
            ALTER TABLE dbo.ApplicationProfiles ADD DefaultPurpose nvarchar(700) NULL;

        IF OBJECT_ID(N'dbo.ApplicationProfileInstances', N'U') IS NOT NULL
           AND COL_LENGTH(N'dbo.ApplicationProfileInstances', N'Purpose') IS NULL
            ALTER TABLE dbo.ApplicationProfileInstances ADD Purpose nvarchar(700) NULL;

        IF OBJECT_ID(N'dbo.ApplicationProfiles', N'U') IS NOT NULL
           AND COL_LENGTH(N'dbo.ApplicationProfiles', N'PersonPassportLastCount') IS NULL
            ALTER TABLE dbo.ApplicationProfiles ADD PersonPassportLastCount int NOT NULL
                CONSTRAINT DF_ApplicationProfiles_PersonPassportLastCount DEFAULT (1);

        IF OBJECT_ID(N'dbo.ApplicationProfiles', N'U') IS NOT NULL
           AND COL_LENGTH(N'dbo.ApplicationProfiles', N'PersonVisaLastCount') IS NULL
            ALTER TABLE dbo.ApplicationProfiles ADD PersonVisaLastCount int NOT NULL
                CONSTRAINT DF_ApplicationProfiles_PersonVisaLastCount DEFAULT (1);

        IF OBJECT_ID(N'dbo.ApplicationProfiles', N'U') IS NOT NULL
           AND COL_LENGTH(N'dbo.ApplicationProfiles', N'PersonInvitationItemLastCount') IS NULL
            ALTER TABLE dbo.ApplicationProfiles ADD PersonInvitationItemLastCount int NOT NULL
                CONSTRAINT DF_ApplicationProfiles_PersonInvitationItemLastCount DEFAULT (1);

        IF OBJECT_ID(N'dbo.ApplicationProfiles', N'U') IS NOT NULL
           AND COL_LENGTH(N'dbo.ApplicationProfiles', N'PersonWorkPermitItemLastCount') IS NULL
            ALTER TABLE dbo.ApplicationProfiles ADD PersonWorkPermitItemLastCount int NOT NULL
                CONSTRAINT DF_ApplicationProfiles_PersonWorkPermitItemLastCount DEFAULT (1);

        IF OBJECT_ID(N'dbo.ApplicationProfiles', N'U') IS NOT NULL
           AND COL_LENGTH(N'dbo.ApplicationProfiles', N'PersonBorderZoneItemLastCount') IS NULL
            ALTER TABLE dbo.ApplicationProfiles ADD PersonBorderZoneItemLastCount int NOT NULL
                CONSTRAINT DF_ApplicationProfiles_PersonBorderZoneItemLastCount DEFAULT (1);

        IF OBJECT_ID(N'dbo.ApplicationProfiles', N'U') IS NOT NULL
           AND COL_LENGTH(N'dbo.ApplicationProfiles', N'RequireProcessNumber') IS NULL
            ALTER TABLE dbo.ApplicationProfiles ADD RequireProcessNumber bit NOT NULL
                CONSTRAINT DF_ApplicationProfiles_RequireProcessNumber DEFAULT (0);
        """;

    public static void ApplyIfMissing(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return;

        // Greenfield: CREATE TABLE IF NOT EXISTS would poison an empty DB so EF EnsureCreated no-ops.
        if (DatabaseProviderDetector.IsPostgreSql(connectionString)
            && !PostgresRelationExists.All(connectionString, "People"))
            return;

        var cleaned = DatabaseProviderDetector.StripEfCoreProvider(connectionString);
        if (DatabaseProviderDetector.IsPostgreSql(connectionString))
        {
            using var connection = new NpgsqlConnection(cleaned);
            connection.Open();
            Execute(connection, EnsureSchemaPostgres);
            foreach (var sql in EnsureTemplateCatalogColumnsPostgresStatements)
                Execute(connection, sql);
            return;
        }

        using var sqlConnection = new SqlConnection(cleaned);
        sqlConnection.Open();
        Execute(sqlConnection, EnsureSchemaSqlServer);
        Execute(sqlConnection, EnsureTemplateCatalogColumnsSqlServer);
    }

    private static void Execute(System.Data.Common.DbConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}
