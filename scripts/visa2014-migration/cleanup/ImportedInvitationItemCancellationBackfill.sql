-- Dev partial reimport: InvitationItems for IsCancelled backfill.
-- Keeps Invitation headers, People, Applications, ApplicationItems (FKs nulled first).
-- Run against Visa2026 target only -- never VISA2015.
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID('dbo.ApplicationItems', 'U') IS NOT NULL
BEGIN
    UPDATE ApplicationItems SET CurrentInvitationItemID = NULL WHERE CurrentInvitationItemID IS NOT NULL;
    UPDATE ApplicationItems SET PreviousInvitationItemID = NULL WHERE PreviousInvitationItemID IS NOT NULL;
END

IF OBJECT_ID('dbo.Visas', 'U') IS NOT NULL AND COL_LENGTH('dbo.Visas', 'InvitationItemID') IS NOT NULL
    UPDATE Visas SET InvitationItemID = NULL WHERE InvitationItemID IS NOT NULL;

IF OBJECT_ID('dbo.InvitationItems', 'U') IS NOT NULL
    DELETE FROM InvitationItems;

COMMIT TRANSACTION;

SELECT COUNT(*) AS RemainingInvitationItems FROM InvitationItems;
SELECT COUNT(*) AS ApplicationItemsWithCurrentInvitationItem
FROM ApplicationItems WHERE CurrentInvitationItemID IS NOT NULL;