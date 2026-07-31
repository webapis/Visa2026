using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.EFCore;
using Microsoft.EntityFrameworkCore;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Idempotent CREATE for ApplicationTypeGroup tables (SQL Server + PostgreSQL).
/// Required when ModuleInfo skips EF schema sync and SeedGate runs before tables exist.
/// </summary>
public static class ApplicationTypeGroupSchemaSql
{
    internal const string EnsureTablesSqlServer = """
        IF OBJECT_ID(N'dbo.ApplicationTypeGroups', N'U') IS NULL
        BEGIN
            CREATE TABLE dbo.ApplicationTypeGroups (
                ID uniqueidentifier NOT NULL CONSTRAINT PK_ApplicationTypeGroups PRIMARY KEY,
                GCRecord int NOT NULL CONSTRAINT DF_ApplicationTypeGroups_GCRecord DEFAULT (0),
                OptimisticLockField int NOT NULL CONSTRAINT DF_ApplicationTypeGroups_OLF DEFAULT (0),
                Name nvarchar(200) NULL,
                NameTm nvarchar(200) NULL,
                LocalizationKey nvarchar(64) NULL,
                Code nvarchar(20) NULL,
                IsDefault bit NOT NULL CONSTRAINT DF_ApplicationTypeGroups_IsDefault DEFAULT (0),
                SortOrder int NOT NULL CONSTRAINT DF_ApplicationTypeGroups_SortOrder DEFAULT (0),
                IsActive bit NOT NULL CONSTRAINT DF_ApplicationTypeGroups_IsActive DEFAULT (1)
            );
        END;

        IF OBJECT_ID(N'dbo.ApplicationTypeGroupMembers', N'U') IS NULL
           AND OBJECT_ID(N'dbo.ApplicationTypeGroups', N'U') IS NOT NULL
           AND OBJECT_ID(N'dbo.ApplicationTypes', N'U') IS NOT NULL
        BEGIN
            CREATE TABLE dbo.ApplicationTypeGroupMembers (
                ID uniqueidentifier NOT NULL CONSTRAINT PK_ApplicationTypeGroupMembers PRIMARY KEY,
                GCRecord int NOT NULL CONSTRAINT DF_ApplicationTypeGroupMembers_GCRecord DEFAULT (0),
                OptimisticLockField int NOT NULL CONSTRAINT DF_ApplicationTypeGroupMembers_OLF DEFAULT (0),
                ApplicationTypeGroupId uniqueidentifier NOT NULL,
                ApplicationTypeId uniqueidentifier NOT NULL,
                CONSTRAINT FK_ApplicationTypeGroupMembers_ApplicationTypeGroups_ApplicationTypeGroupId
                    FOREIGN KEY (ApplicationTypeGroupId) REFERENCES dbo.ApplicationTypeGroups(ID) ON DELETE CASCADE,
                CONSTRAINT FK_ApplicationTypeGroupMembers_ApplicationTypes_ApplicationTypeId
                    FOREIGN KEY (ApplicationTypeId) REFERENCES dbo.ApplicationTypes(ID)
            );
            CREATE UNIQUE INDEX IX_ApplicationTypeGroupMembers_ApplicationTypeGroupId_ApplicationTypeId
                ON dbo.ApplicationTypeGroupMembers (ApplicationTypeGroupId, ApplicationTypeId)
                WHERE GCRecord IS NULL;
            CREATE INDEX IX_ApplicationTypeGroupMembers_ApplicationTypeId
                ON dbo.ApplicationTypeGroupMembers (ApplicationTypeId);
        END;

        IF OBJECT_ID(N'dbo.UserReportTemplateApplicationTypeGroups', N'U') IS NULL
           AND OBJECT_ID(N'dbo.UserReportTemplates', N'U') IS NOT NULL
           AND OBJECT_ID(N'dbo.ApplicationTypeGroups', N'U') IS NOT NULL
        BEGIN
            CREATE TABLE dbo.UserReportTemplateApplicationTypeGroups (
                ID uniqueidentifier NOT NULL CONSTRAINT PK_UserReportTemplateApplicationTypeGroups PRIMARY KEY,
                GCRecord int NOT NULL CONSTRAINT DF_UserReportTemplateApplicationTypeGroups_GCRecord DEFAULT (0),
                OptimisticLockField int NOT NULL CONSTRAINT DF_UserReportTemplateApplicationTypeGroups_OLF DEFAULT (0),
                UserReportTemplateId uniqueidentifier NOT NULL,
                ApplicationTypeGroupId uniqueidentifier NOT NULL,
                CONSTRAINT FK_UserReportTemplateApplicationTypeGroups_UserReportTemplates_UserReportTemplateId
                    FOREIGN KEY (UserReportTemplateId) REFERENCES dbo.UserReportTemplates(ID) ON DELETE CASCADE,
                CONSTRAINT FK_UserReportTemplateApplicationTypeGroups_ApplicationTypeGroups_ApplicationTypeGroupId
                    FOREIGN KEY (ApplicationTypeGroupId) REFERENCES dbo.ApplicationTypeGroups(ID)
            );
            CREATE UNIQUE INDEX IX_UserReportTemplateApplicationTypeGroups_UserReportTemplateId_ApplicationTypeGroupId
                ON dbo.UserReportTemplateApplicationTypeGroups (UserReportTemplateId, ApplicationTypeGroupId)
                WHERE GCRecord IS NULL;
            CREATE INDEX IX_UserReportTemplateApplicationTypeGroups_ApplicationTypeGroupId
                ON dbo.UserReportTemplateApplicationTypeGroups (ApplicationTypeGroupId);
        END;
        """;

    internal const string EnsureTablesPostgres = """
        CREATE TABLE IF NOT EXISTS "ApplicationTypeGroups" (
            "ID" uuid NOT NULL,
            "GCRecord" integer NOT NULL DEFAULT 0,
            "OptimisticLockField" integer NOT NULL DEFAULT 0,
            "Name" character varying(200) NULL,
            "NameTm" character varying(200) NULL,
            "LocalizationKey" character varying(64) NULL,
            "Code" character varying(20) NULL,
            "IsDefault" boolean NOT NULL DEFAULT false,
            "SortOrder" integer NOT NULL DEFAULT 0,
            "IsActive" boolean NOT NULL DEFAULT true,
            CONSTRAINT "PK_ApplicationTypeGroups" PRIMARY KEY ("ID")
        );

        DO $$
        BEGIN
          IF to_regclass('public."ApplicationTypeGroupMembers"') IS NULL
             AND to_regclass('public."ApplicationTypeGroups"') IS NOT NULL
             AND to_regclass('public."ApplicationTypes"') IS NOT NULL
          THEN
            CREATE TABLE "ApplicationTypeGroupMembers" (
                "ID" uuid NOT NULL,
                "GCRecord" integer NOT NULL DEFAULT 0,
                "OptimisticLockField" integer NOT NULL DEFAULT 0,
                "ApplicationTypeGroupId" uuid NOT NULL,
                "ApplicationTypeId" uuid NOT NULL,
                CONSTRAINT "PK_ApplicationTypeGroupMembers" PRIMARY KEY ("ID"),
                CONSTRAINT "FK_ApplicationTypeGroupMembers_ApplicationTypeGroups_ApplicationTypeGroupId"
                    FOREIGN KEY ("ApplicationTypeGroupId") REFERENCES "ApplicationTypeGroups" ("ID") ON DELETE CASCADE,
                CONSTRAINT "FK_ApplicationTypeGroupMembers_ApplicationTypes_ApplicationTypeId"
                    FOREIGN KEY ("ApplicationTypeId") REFERENCES "ApplicationTypes" ("ID")
            );
            CREATE UNIQUE INDEX "IX_ApplicationTypeGroupMembers_ApplicationTypeGroupId_ApplicationTypeId"
                ON "ApplicationTypeGroupMembers" ("ApplicationTypeGroupId", "ApplicationTypeId")
                WHERE ("GCRecord" IS NULL);
            CREATE INDEX "IX_ApplicationTypeGroupMembers_ApplicationTypeId"
                ON "ApplicationTypeGroupMembers" ("ApplicationTypeId");
          END IF;

          IF to_regclass('public."UserReportTemplateApplicationTypeGroups"') IS NULL
             AND to_regclass('public."UserReportTemplates"') IS NOT NULL
             AND to_regclass('public."ApplicationTypeGroups"') IS NOT NULL
          THEN
            CREATE TABLE "UserReportTemplateApplicationTypeGroups" (
                "ID" uuid NOT NULL,
                "GCRecord" integer NOT NULL DEFAULT 0,
                "OptimisticLockField" integer NOT NULL DEFAULT 0,
                "UserReportTemplateId" uuid NOT NULL,
                "ApplicationTypeGroupId" uuid NOT NULL,
                CONSTRAINT "PK_UserReportTemplateApplicationTypeGroups" PRIMARY KEY ("ID"),
                CONSTRAINT "FK_UserReportTemplateApplicationTypeGroups_UserReportTemplates_UserReportTemplateId"
                    FOREIGN KEY ("UserReportTemplateId") REFERENCES "UserReportTemplates" ("ID") ON DELETE CASCADE,
                CONSTRAINT "FK_UserReportTemplateApplicationTypeGroups_ApplicationTypeGroups_ApplicationTypeGroupId"
                    FOREIGN KEY ("ApplicationTypeGroupId") REFERENCES "ApplicationTypeGroups" ("ID")
            );
            CREATE UNIQUE INDEX "IX_UserReportTemplateApplicationTypeGroups_UserReportTemplateId_ApplicationTypeGroupId"
                ON "UserReportTemplateApplicationTypeGroups" ("UserReportTemplateId", "ApplicationTypeGroupId")
                WHERE ("GCRecord" IS NULL);
            CREATE INDEX "IX_UserReportTemplateApplicationTypeGroups_ApplicationTypeGroupId"
                ON "UserReportTemplateApplicationTypeGroups" ("ApplicationTypeGroupId");
          END IF;
        END $$;
        """;

    /// <summary>Creates missing group tables using the ObjectSpace connection (SeedGate / ModuleUpdater).</summary>
    public static void EnsureTables(IObjectSpace objectSpace)
    {
        if (objectSpace is not EFCoreObjectSpace { DbContext: { } dbContext })
            return;

        var sql = DatabaseProviderDetector.IsPostgreSql(objectSpace)
            ? EnsureTablesPostgres
            : EnsureTablesSqlServer;

        dbContext.Database.ExecuteSqlRaw(sql);
    }
}