-- Local disposable PostgreSQL: wipe imported transactional BOs.
-- Keeps: lookup catalogs, ProjectContracts, ApprovalLegProfiles, PermissionPolicy*, ApplicationTypes, etc.
-- IMPORTANT: Do NOT truncate FileData (CASCADE would wipe UserReportTemplates / ProjectContractDocuments).
-- After truncate, optionally: DELETE FROM "FileData" WHERE "ID" NOT IN (SELECT "TemplateFileID" FROM "UserReportTemplates" WHERE "TemplateFileID" IS NOT NULL);
BEGIN;
TRUNCATE TABLE
  "ApplicationProgresses",
  "ApplicationItems",
  "ApplicationApprovalLegSnapshots",
  "Applications",
  "WorkPermitItems",
  "WorkPermitLocations",
  "WorkPermitDocuments",
  "WorkPermitImages",
  "WorkPermits",
  "InvitationItems",
  "InvitationDocuments",
  "InvitationImages",
  "Invitations",
  "BorderZoneItem",
  "BorderZoneDocuments",
  "BorderZones",
  "RejectionItems",
  "RejectionDocuments",
  "RejectionImages",
  "Rejections",
  "TravelHistories",
  "BusinessTripAddress",
  "VisaDocument",
  "VisaImages",
  "Visas",
  "PassportDocuments",
  "PassportImages",
  "Passports",
  "EducationDocument",
  "EducationImages",
  "Educations",
  "EmployeePositionHistories",
  "EmployeeSalaries",
  "WorkDuties",
  "AddressOfResidenceDocuments",
  "AddressOfResidenceImages",
  "AddressesOfResidence",
  "MedicalRecordDocuments",
  "MedicalRecordImage",
  "MedicalRecords",
  "PersonDocuments",
  "PersonFamilyRelationDocuments",
  "FamilyMemberImages",
  "LodgingDocuments",
  "LodgingImages",
  "PdfGenerationBatches",
  "WordReportGenerationBatches",
  "BoStateSnapshots",
  "StateChangeLogs",
  "SyncRuleLogs",
  "AuditData",
  "AuditEFCoreWeakReferences",
  "ApplicationRuntimeLogs",
  "UserFeedbacks",
  "People"
RESTART IDENTITY CASCADE;
COMMIT;

-- Keep UserReportTemplates / other non-transactional FileData; drop import blobs.
BEGIN;
DELETE FROM "FileData" fd
WHERE NOT EXISTS (
  SELECT 1 FROM "UserReportTemplates" u WHERE u."TemplateFileID" = fd."ID"
)
AND NOT EXISTS (
  SELECT 1 FROM "ProjectContractDocuments" p WHERE p."File" = fd."ID" OR p."Document" = fd."ID"
);
COMMIT;