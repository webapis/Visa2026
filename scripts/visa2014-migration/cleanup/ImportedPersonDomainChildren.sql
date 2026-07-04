-- Delete VISA2014-imported person-domain children after Person reimport.
-- Keeps dbo.People and manual-entry Applications. Run against Visa2026 target only -- never VISA2015.
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- Clear ApplicationItem person-current pointers so child BO deletes do not violate FK.
IF OBJECT_ID('dbo.ApplicationItems', 'U') IS NOT NULL
BEGIN
    UPDATE ApplicationItems SET CurrentVisaId = NULL WHERE CurrentVisaId IS NOT NULL;
    UPDATE ApplicationItems SET NextVisaId = NULL WHERE NextVisaId IS NOT NULL;
    UPDATE ApplicationItems SET CurrentPassportID = NULL WHERE CurrentPassportID IS NOT NULL;
    UPDATE ApplicationItems SET PreviousPassportID = NULL WHERE PreviousPassportID IS NOT NULL;
    UPDATE ApplicationItems SET CurrentEducationID = NULL WHERE CurrentEducationID IS NOT NULL;
    UPDATE ApplicationItems SET CurrentSalaryID = NULL WHERE CurrentSalaryID IS NOT NULL;
    UPDATE ApplicationItems SET CurrentPositionHistoryID = NULL WHERE CurrentPositionHistoryID IS NOT NULL;
    UPDATE ApplicationItems SET CurrentAddressOfResidenceID = NULL WHERE CurrentAddressOfResidenceID IS NOT NULL;
END

IF OBJECT_ID('dbo.BorderZoneItem', 'U') IS NOT NULL UPDATE BorderZoneItem SET PassportID = NULL WHERE PassportID IS NOT NULL;
IF OBJECT_ID('dbo.InvitationItems', 'U') IS NOT NULL UPDATE InvitationItems SET PassportID = NULL WHERE PassportID IS NOT NULL;
IF OBJECT_ID('dbo.RejectionItems', 'U') IS NOT NULL UPDATE RejectionItems SET PassportID = NULL WHERE PassportID IS NOT NULL;
IF OBJECT_ID('dbo.WorkPermitItems', 'U') IS NOT NULL
BEGIN
    UPDATE WorkPermitItems SET PassportID = NULL WHERE PassportID IS NOT NULL;
    UPDATE WorkPermitItems SET CurrentPositionHistoryID = NULL WHERE CurrentPositionHistoryID IS NOT NULL;
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
IF OBJECT_ID('dbo.ActualPositions', 'U') IS NOT NULL DELETE FROM ActualPositions;
IF OBJECT_ID('dbo.EmployeeSalaries', 'U') IS NOT NULL DELETE FROM EmployeeSalaries;
IF OBJECT_ID('dbo.AddressesOfResidence', 'U') IS NOT NULL DELETE FROM AddressesOfResidence;

COMMIT TRANSACTION;

SELECT COUNT(*) AS RemainingPeople FROM People;
SELECT COUNT(*) AS RemainingPassports FROM Passports;
SELECT COUNT(*) AS RemainingVisas FROM Visas;
SELECT COUNT(*) AS RemainingEducations FROM Educations;
SELECT COUNT(*) AS RemainingEmployeePositionHistories FROM EmployeePositionHistories;
SELECT COUNT(*) AS RemainingEmployeeSalaries FROM EmployeeSalaries;
SELECT COUNT(*) AS RemainingAddressesOfResidence FROM AddressesOfResidence;
