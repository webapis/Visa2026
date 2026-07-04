-- Delete VISA2014-imported Application scope (IsManualEntry = 1) for local reimport.
-- Run against Visa2026 target DB only — never VISA2015.

SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

DECLARE @appIds TABLE (ID uniqueidentifier PRIMARY KEY);
INSERT INTO @appIds (ID)
SELECT ID FROM Applications
WHERE IsManualEntry = 1 AND (GCRecord IS NULL OR GCRecord = 0);

DECLARE @appCount int = (SELECT COUNT(*) FROM @appIds);
PRINT CONCAT('Applications to delete: ', @appCount);

IF @appCount > 0
BEGIN
    -- Clear ApplicationItem pointers to permit/invitation lines before deleting those rows.
    IF OBJECT_ID('dbo.ApplicationItems', 'U') IS NOT NULL
    BEGIN
        UPDATE ai SET
            CurrentInvitationItemID = NULL,
            PreviousInvitationItemID = NULL,
            CurrentWorkPermitItemID = NULL,
            SecondWorkPermitItemId = NULL
        FROM ApplicationItems ai
        INNER JOIN @appIds a ON ai.ApplicationID = a.ID;
    END

    IF OBJECT_ID('dbo.Visas', 'U') IS NOT NULL AND COL_LENGTH('dbo.Visas', 'InvitationItemID') IS NOT NULL
        UPDATE v SET InvitationItemID = NULL
        FROM Visas v
        INNER JOIN InvitationItems ii ON ii.ID = v.InvitationItemID
        INNER JOIN Invitations i ON i.ID = ii.InvitationID
        INNER JOIN @appIds a ON i.ApplicationID = a.ID;

    -- Grandchildren (item rows under invitation / work permit / rejection / border zone)
    IF OBJECT_ID('dbo.TravelHistories', 'U') IS NOT NULL
        DELETE th FROM TravelHistories th
        INNER JOIN ApplicationItems ai ON th.SourceApplicationItemID = ai.ID
        INNER JOIN @appIds a ON ai.ApplicationID = a.ID;

    IF OBJECT_ID('dbo.InvitationItems', 'U') IS NOT NULL AND OBJECT_ID('dbo.Invitations', 'U') IS NOT NULL
        DELETE ii FROM InvitationItems ii
        INNER JOIN Invitations i ON ii.InvitationID = i.ID
        INNER JOIN @appIds a ON i.ApplicationID = a.ID;

    IF OBJECT_ID('dbo.WorkPermitItems', 'U') IS NOT NULL AND OBJECT_ID('dbo.WorkPermits', 'U') IS NOT NULL
        DELETE wi FROM WorkPermitItems wi
        INNER JOIN WorkPermits w ON wi.WorkPermitID = w.ID
        INNER JOIN @appIds a ON w.ApplicationID = a.ID;

    IF OBJECT_ID('dbo.RejectionItems', 'U') IS NOT NULL AND OBJECT_ID('dbo.Rejections', 'U') IS NOT NULL
        DELETE ri FROM RejectionItems ri
        INNER JOIN Rejections r ON ri.RejectionID = r.ID
        INNER JOIN @appIds a ON r.ApplicationID = a.ID;

    -- Children (order matters for FK constraints)
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

COMMIT TRANSACTION;

SELECT @appCount AS ApplicationsDeleted;
SELECT COUNT(*) AS RemainingManualEntryApps FROM Applications WHERE IsManualEntry = 1 AND (GCRecord IS NULL OR GCRecord = 0);
