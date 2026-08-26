-- Dev partial reimport: Visas + WorkPermitItems for IsCancelled backfill.
-- Keeps WorkPermit headers, People, Applications, ApplicationItems (FKs nulled first).
-- Run against Visa2026 target only -- never VISA2015.
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID('dbo.ApplicationItems', 'U') IS NOT NULL
BEGIN
    UPDATE ApplicationItems SET CurrentVisaId = NULL WHERE CurrentVisaId IS NOT NULL;
    UPDATE ApplicationItems SET NextVisaId = NULL WHERE NextVisaId IS NOT NULL;
    UPDATE ApplicationItems SET CurrentWorkPermitItemID = NULL WHERE CurrentWorkPermitItemID IS NOT NULL;
END

IF OBJECT_ID('dbo.Visas', 'U') IS NOT NULL AND COL_LENGTH('dbo.Visas', 'IssuingInvitationItemID') IS NOT NULL
    UPDATE Visas SET IssuingInvitationItemID = NULL WHERE IssuingInvitationItemID IS NOT NULL;

IF OBJECT_ID('dbo.VisaDocument', 'U') IS NOT NULL
    DELETE vd FROM VisaDocument vd INNER JOIN Visas v ON vd.VisaID = v.ID;
IF OBJECT_ID('dbo.VisaImages', 'U') IS NOT NULL
    DELETE FROM VisaImages;
IF OBJECT_ID('dbo.Visas', 'U') IS NOT NULL
    DELETE FROM Visas;

IF OBJECT_ID('dbo.WorkPermitItems', 'U') IS NOT NULL
    DELETE FROM WorkPermitItems;

COMMIT TRANSACTION;

SELECT COUNT(*) AS RemainingVisas FROM Visas;
SELECT COUNT(*) AS RemainingWorkPermitItems FROM WorkPermitItems;
SELECT COUNT(*) AS ApplicationItemsWithCurrentVisa
FROM ApplicationItems WHERE CurrentVisaId IS NOT NULL;
SELECT COUNT(*) AS ApplicationItemsWithCurrentWorkPermitItem
FROM ApplicationItems WHERE CurrentWorkPermitItemID IS NOT NULL;