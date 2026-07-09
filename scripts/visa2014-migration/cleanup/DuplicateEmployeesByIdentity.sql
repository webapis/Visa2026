-- Review / merge duplicate employee Persons (same FirstName + LastName + DateOfBirth, IsEmployee=1).
-- Keeps MIN(ID) per identity group; repoints all FK columns to People; soft-deletes extras (GCRecord = 1).
-- Scope BootstrapSupplement: exactly 2 rows, one bootstrap suffix (…d7f5) + one supplement suffix (…aadd) — prod calik-energi snapshot.
-- Scope AllIdentity: every duplicate employee identity group (includes legacy _dub pairs — review carefully).
-- Run PREVIEW first (@Apply = 0). Repair-DuplicateEmployees.ps1 replaces @Apply / @Scope.

SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Apply bit = 0;
DECLARE @Scope varchar(32) = N'BootstrapSupplement';

IF OBJECT_ID('tempdb..#ScopedEmp') IS NOT NULL DROP TABLE #ScopedEmp;
IF OBJECT_ID('tempdb..#IdentityGroups') IS NOT NULL DROP TABLE #IdentityGroups;
IF OBJECT_ID('tempdb..#Extras') IS NOT NULL DROP TABLE #Extras;

;WITH Emp AS (
    SELECT
        p.ID,
        UPPER(LTRIM(RTRIM(p.FirstName))) AS Fn,
        UPPER(LTRIM(RTRIM(p.LastName))) AS Ln,
        CAST(p.DateOfBirth AS date) AS Dob,
        p.PersonalNumber,
        RIGHT(LOWER(CAST(p.ID AS varchar(36))), 4) AS IdSuffix
    FROM dbo.People p
    WHERE (p.GCRecord IS NULL OR p.GCRecord = 0)
      AND p.IsEmployee = 1
),
DupKeys AS (
    SELECT Fn, Ln, Dob
    FROM Emp
    GROUP BY Fn, Ln, Dob
    HAVING COUNT(*) > 1
),
Scoped AS (
    SELECT e.*
    FROM Emp e
    INNER JOIN DupKeys d ON d.Fn = e.Fn AND d.Ln = e.Ln AND d.Dob = e.Dob
    WHERE @Scope = N'AllIdentity'
       OR (
            @Scope = N'BootstrapSupplement'
            AND (SELECT COUNT(*) FROM Emp e2 WHERE e2.Fn = e.Fn AND e2.Ln = e.Ln AND e2.Dob = e.Dob) = 2
            AND (SELECT COUNT(*) FROM Emp e2 WHERE e2.Fn = e.Fn AND e2.Ln = e.Ln AND e2.Dob = e.Dob AND e2.IdSuffix = N'd7f5') = 1
            AND (SELECT COUNT(*) FROM Emp e2 WHERE e2.Fn = e.Fn AND e2.Ln = e.Ln AND e2.Dob = e.Dob AND e2.IdSuffix = N'aadd') = 1
          )
)
SELECT *
INTO #ScopedEmp
FROM Scoped;

SELECT
    s.Fn,
    s.Ln,
    s.Dob,
    MIN(s.ID) AS KeepId,
    COUNT(*) AS DupRowCount
INTO #IdentityGroups
FROM #ScopedEmp s
GROUP BY s.Fn, s.Ln, s.Dob;

SELECT
    e.ID AS ExtraId,
    g.KeepId,
    g.Fn,
    g.Ln,
    g.Dob,
    e.PersonalNumber,
    e.IdSuffix,
    g.DupRowCount
INTO #Extras
FROM #ScopedEmp e
INNER JOIN #IdentityGroups g ON g.Fn = e.Fn AND g.Ln = e.Ln AND g.Dob = e.Dob
WHERE e.ID <> g.KeepId;

DECLARE @GroupCount int = (SELECT COUNT(*) FROM #IdentityGroups);
DECLARE @ExtraCount int = (SELECT COUNT(*) FROM #Extras);
PRINT CONCAT('Scope: ', @Scope);
PRINT CONCAT('Duplicate identity groups: ', @GroupCount);
PRINT CONCAT('Extra Person rows to soft-delete: ', @ExtraCount);

SELECT TOP 50
    e.Fn + N' ' + e.Ln AS FullNameKey,
    e.Dob,
    keepP.PersonalNumber AS KeepPersonalNumber,
    extraP.PersonalNumber AS ExtraPersonalNumber,
    e.KeepId,
    e.ExtraId,
    e.IdSuffix,
    e.DupRowCount,
    (SELECT COUNT(*) FROM dbo.Passports pp WHERE pp.PersonID = e.ExtraId AND (pp.GCRecord IS NULL OR pp.GCRecord = 0)) AS PassportsOnExtra,
    (SELECT COUNT(*) FROM dbo.WorkPermitItems wpi WHERE wpi.PersonID = e.ExtraId AND (wpi.GCRecord IS NULL OR wpi.GCRecord = 0)) AS WorkPermitItemsOnExtra,
    (SELECT COUNT(*) FROM dbo.ApplicationItems ai WHERE ai.PersonID = e.ExtraId AND (ai.GCRecord IS NULL OR ai.GCRecord = 0)) AS ApplicationItemsOnExtra
FROM #Extras e
INNER JOIN dbo.People keepP ON keepP.ID = e.KeepId
INNER JOIN dbo.People extraP ON extraP.ID = e.ExtraId
ORDER BY e.Ln, e.Fn;

-- Merge pairs for id-map repair (PowerShell reads this result set on -Apply).
SELECT e.KeepId, e.ExtraId
FROM #Extras e
ORDER BY e.KeepId, e.ExtraId;

IF @Apply = 0
BEGIN
    PRINT 'PREVIEW ONLY — no changes applied.';
    RETURN;
END

BEGIN TRANSACTION;

DECLARE @fkSql nvarchar(max) = N'';
SELECT @fkSql = @fkSql + N'
UPDATE t SET t.' + QUOTENAME(c.name) + N' = e.KeepId
FROM dbo.' + QUOTENAME(OBJECT_NAME(fk.parent_object_id)) + N' t
INNER JOIN #Extras e ON t.' + QUOTENAME(c.name) + N' = e.ExtraId
WHERE (t.GCRecord IS NULL OR t.GCRecord = 0);'
FROM sys.foreign_keys fk
INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
INNER JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
WHERE OBJECT_NAME(fk.referenced_object_id) = N'People';

EXEC sp_executesql @fkSql;

-- Dedupe child rows that became duplicates after Person merge (keep MIN(ID) per business key).
UPDATE pp SET pp.GCRecord = 1
FROM dbo.Passports pp
INNER JOIN (
    SELECT PersonID, LTRIM(RTRIM(PassportNumber)) AS Pn, MIN(ID) AS KeepId
    FROM dbo.Passports
    WHERE (GCRecord IS NULL OR GCRecord = 0)
      AND PersonID IS NOT NULL
      AND NULLIF(LTRIM(RTRIM(PassportNumber)), N'') IS NOT NULL
    GROUP BY PersonID, LTRIM(RTRIM(PassportNumber))
    HAVING COUNT(*) > 1
) d ON pp.PersonID = d.PersonID AND LTRIM(RTRIM(pp.PassportNumber)) = d.Pn AND pp.ID <> d.KeepId
WHERE (pp.GCRecord IS NULL OR pp.GCRecord = 0);

UPDATE ai SET ai.GCRecord = 1
FROM dbo.ApplicationItems ai
INNER JOIN (
    SELECT ApplicationID, PersonID, MIN(ID) AS KeepId
    FROM dbo.ApplicationItems
    WHERE (GCRecord IS NULL OR GCRecord = 0) AND ApplicationID IS NOT NULL AND PersonID IS NOT NULL
    GROUP BY ApplicationID, PersonID
    HAVING COUNT(*) > 1
) d ON ai.ApplicationID = d.ApplicationID AND ai.PersonID = d.PersonID AND ai.ID <> d.KeepId
WHERE (ai.GCRecord IS NULL OR ai.GCRecord = 0);

UPDATE wpi SET wpi.GCRecord = 1
FROM dbo.WorkPermitItems wpi
INNER JOIN (
    SELECT WorkPermitID, PersonID, MIN(ID) AS KeepId
    FROM dbo.WorkPermitItems
    WHERE (GCRecord IS NULL OR GCRecord = 0) AND WorkPermitID IS NOT NULL AND PersonID IS NOT NULL
    GROUP BY WorkPermitID, PersonID
    HAVING COUNT(*) > 1
) d ON wpi.WorkPermitID = d.WorkPermitID AND wpi.PersonID = d.PersonID AND wpi.ID <> d.KeepId
WHERE (wpi.GCRecord IS NULL OR wpi.GCRecord = 0);

UPDATE ii SET ii.GCRecord = 1
FROM dbo.InvitationItems ii
INNER JOIN (
    SELECT InvitationID, PersonID, MIN(ID) AS KeepId
    FROM dbo.InvitationItems
    WHERE (GCRecord IS NULL OR GCRecord = 0) AND InvitationID IS NOT NULL AND PersonID IS NOT NULL
    GROUP BY InvitationID, PersonID
    HAVING COUNT(*) > 1
) d ON ii.InvitationID = d.InvitationID AND ii.PersonID = d.PersonID AND ii.ID <> d.KeepId
WHERE (ii.GCRecord IS NULL OR ii.GCRecord = 0);

UPDATE eph SET eph.GCRecord = 1
FROM dbo.EmployeePositionHistories eph
INNER JOIN (
    SELECT PersonID, StartDate, PositionID, MIN(ID) AS KeepId
    FROM dbo.EmployeePositionHistories
    WHERE (GCRecord IS NULL OR GCRecord = 0) AND PersonID IS NOT NULL
    GROUP BY PersonID, StartDate, PositionID
    HAVING COUNT(*) > 1
) d ON eph.PersonID = d.PersonID AND eph.StartDate = d.StartDate AND eph.PositionID = d.PositionID AND eph.ID <> d.KeepId
WHERE (eph.GCRecord IS NULL OR eph.GCRecord = 0);

UPDATE p SET p.GCRecord = 1
FROM dbo.People p
INNER JOIN #Extras e ON e.ExtraId = p.ID
WHERE (p.GCRecord IS NULL OR p.GCRecord = 0);

COMMIT TRANSACTION;

DECLARE @Remaining int = (
    SELECT COUNT(*) FROM (
        SELECT
            UPPER(LTRIM(RTRIM(FirstName))) AS Fn,
            UPPER(LTRIM(RTRIM(LastName))) AS Ln,
            CAST(DateOfBirth AS date) AS Dob
        FROM dbo.People
        WHERE (GCRecord IS NULL OR GCRecord = 0) AND IsEmployee = 1
        GROUP BY UPPER(LTRIM(RTRIM(FirstName))), UPPER(LTRIM(RTRIM(LastName))), CAST(DateOfBirth AS date)
        HAVING COUNT(*) > 1
    ) x
);
PRINT CONCAT('Remaining employee identity duplicate groups (all scopes): ', @Remaining);