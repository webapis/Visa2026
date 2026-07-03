-- Delete VISA2014-imported person-domain data (+ application scope that FKs to Person).
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @appIds TABLE (ID uniqueidentifier PRIMARY KEY);
INSERT INTO @appIds (ID)
SELECT ID FROM Applications WHERE IsManualEntry = 1 AND (GCRecord IS NULL OR GCRecord = 0);
DECLARE @appCount int = (SELECT COUNT(*) FROM @appIds);
PRINT CONCAT('Manual-entry applications in scope: ', @appCount);
IF @appCount > 0
BEGIN
    IF OBJECT_ID('dbo.TravelHistories', 'U') IS NOT NULL
        DELETE th FROM TravelHistories th INNER JOIN ApplicationItems ai ON th.SourceApplicationItemID = ai.ID INNER JOIN @appIds a ON ai.ApplicationID = a.ID;
    IF OBJECT_ID('dbo.InvitationItems', 'U') IS NOT NULL AND OBJECT_ID('dbo.Invitations', 'U') IS NOT NULL
        DELETE ii FROM InvitationItems ii INNER JOIN Invitations i ON ii.InvitationID = i.ID INNER JOIN @appIds a ON i.ApplicationID = a.ID;
    IF OBJECT_ID('dbo.WorkPermitItems', 'U') IS NOT NULL AND OBJECT_ID('dbo.WorkPermits', 'U') IS NOT NULL
        DELETE wi FROM WorkPermitItems wi INNER JOIN WorkPermits w ON wi.WorkPermitID = w.ID INNER JOIN @appIds a ON w.ApplicationID = a.ID;
    IF OBJECT_ID('dbo.RejectionItems', 'U') IS NOT NULL AND OBJECT_ID('dbo.Rejections', 'U') IS NOT NULL
        DELETE ri FROM RejectionItems ri INNER JOIN Rejections r ON ri.RejectionID = r.ID INNER JOIN @appIds a ON r.ApplicationID = a.ID;
    IF OBJECT_ID('dbo.ApplicationProgresses', 'U') IS NOT NULL
        DELETE ap FROM ApplicationProgresses ap INNER JOIN @appIds a ON ap.ApplicationID = a.ID;
    IF OBJECT_ID('dbo.ApplicationApprovalLegSnapshots', 'U') IS NOT NULL
        DELETE s FROM ApplicationApprovalLegSnapshots s INNER JOIN @appIds a ON s.ApplicationID = a.ID;
    IF OBJECT_ID('dbo.ApplicationItems', 'U') IS NOT NULL
        DELETE ai FROM ApplicationItems ai INNER JOIN @appIds a ON ai.ApplicationID = a.ID;
    IF OBJECT_ID('dbo.WordReportGenerationBatches', 'U') IS NOT NULL
        DELETE b FROM WordReportGenerationBatches b INNER JOIN @appIds a ON b.ApplicationID = a.ID;
    IF OBJECT_ID('dbo.Invitations', 'U') IS NOT NULL
        DELETE i FROM Invitations i INNER JOIN @appIds a ON i.ApplicationID = a.ID;
    IF OBJECT_ID('dbo.WorkPermits', 'U') IS NOT NULL
        DELETE w FROM WorkPermits w INNER JOIN @appIds a ON w.ApplicationID = a.ID;
    IF OBJECT_ID('dbo.Rejections', 'U') IS NOT NULL
        DELETE r FROM Rejections r INNER JOIN @appIds a ON r.ApplicationID = a.ID;
    IF OBJECT_ID('dbo.BorderZones', 'U') IS NOT NULL
        DELETE bz FROM BorderZones bz INNER JOIN @appIds a ON bz.ApplicationID = a.ID;
    DELETE app FROM Applications app INNER JOIN @appIds a ON app.ID = a.ID;
END
IF OBJECT_ID('dbo.TravelHistories', 'U') IS NOT NULL DELETE FROM TravelHistories WHERE PersonID IS NOT NULL;
IF OBJECT_ID('dbo.BorderZoneItems', 'U') IS NOT NULL DELETE FROM BorderZoneItems WHERE PersonID IS NOT NULL;
IF OBJECT_ID('dbo.WorkPermitItems', 'U') IS NOT NULL DELETE FROM WorkPermitItems WHERE PersonID IS NOT NULL;
IF OBJECT_ID('dbo.InvitationItems', 'U') IS NOT NULL DELETE FROM InvitationItems WHERE PersonID IS NOT NULL;
IF OBJECT_ID('dbo.RejectionItems', 'U') IS NOT NULL DELETE FROM RejectionItems WHERE PersonID IS NOT NULL;
IF OBJECT_ID('dbo.VisaDocument', 'U') IS NOT NULL DELETE vd FROM VisaDocument vd INNER JOIN Visas v ON vd.VisaID = v.ID;
IF OBJECT_ID('dbo.VisaImages', 'U') IS NOT NULL DELETE FROM VisaImages;
IF OBJECT_ID('dbo.Visas', 'U') IS NOT NULL DELETE FROM Visas;
IF OBJECT_ID('dbo.PassportDocuments', 'U') IS NOT NULL DELETE FROM PassportDocuments;
IF OBJECT_ID('dbo.PassportImages', 'U') IS NOT NULL DELETE FROM PassportImages;
IF OBJECT_ID('dbo.Passports', 'U') IS NOT NULL DELETE FROM Passports;
IF OBJECT_ID('dbo.EducationDocument', 'U') IS NOT NULL DELETE ed FROM EducationDocument ed INNER JOIN Educations e ON ed.EducationID = e.ID;
IF OBJECT_ID('dbo.EducationImages', 'U') IS NOT NULL DELETE FROM EducationImages;
IF OBJECT_ID('dbo.Educations', 'U') IS NOT NULL DELETE FROM Educations;
IF OBJECT_ID('dbo.MedicalRecordDocuments', 'U') IS NOT NULL DELETE FROM MedicalRecordDocuments;
IF OBJECT_ID('dbo.MedicalRecordImage', 'U') IS NOT NULL DELETE FROM MedicalRecordImage;
IF OBJECT_ID('dbo.MedicalRecords', 'U') IS NOT NULL DELETE FROM MedicalRecords;
IF OBJECT_ID('dbo.EmployeePositionHistories', 'U') IS NOT NULL DELETE FROM EmployeePositionHistories;
IF OBJECT_ID('dbo.EmployeeSalaries', 'U') IS NOT NULL DELETE FROM EmployeeSalaries;
IF OBJECT_ID('dbo.AddressesOfResidence', 'U') IS NOT NULL DELETE FROM AddressesOfResidence;
IF OBJECT_ID('dbo.PersonDocuments', 'U') IS NOT NULL DELETE FROM PersonDocuments;
IF OBJECT_ID('dbo.PersonFamilyRelationDocuments', 'U') IS NOT NULL DELETE FROM PersonFamilyRelationDocuments;
IF OBJECT_ID('dbo.FamilyMemberImages', 'U') IS NOT NULL DELETE FROM FamilyMemberImages;
IF OBJECT_ID('dbo.WorkDuties', 'U') IS NOT NULL DELETE FROM WorkDuties;
UPDATE People SET SponsoringEmployeeID = NULL WHERE SponsoringEmployeeID IS NOT NULL;
DELETE FROM People;
COMMIT TRANSACTION;
SELECT COUNT(*) AS RemainingPeople FROM People;
SELECT COUNT(*) AS RemainingManualApps FROM Applications WHERE IsManualEntry = 1 AND (GCRecord IS NULL OR GCRecord = 0);
