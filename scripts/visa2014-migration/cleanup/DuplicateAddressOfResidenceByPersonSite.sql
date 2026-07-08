-- Review / soft-delete duplicate AddressesOfResidence (same Person + Type + City + FullAddress).
-- Keeps MIN(ID). Repoints ApplicationItems.CurrentAddressOfResidenceID. Soft-deletes extras.
-- Run PREVIEW first (@Apply = 0).

SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Apply bit = 0;

IF OBJECT_ID('tempdb..#DupGroups') IS NOT NULL DROP TABLE #DupGroups;
IF OBJECT_ID('tempdb..#Extras') IS NOT NULL DROP TABLE #Extras;

SELECT aor.PersonID, aor.Type, aor.CityID, aor.FullAddress, MIN(aor.ID) AS KeepId, COUNT(*) AS DupRowCount
INTO #DupGroups
FROM dbo.AddressesOfResidence aor
WHERE (aor.GCRecord IS NULL OR aor.GCRecord = 0) AND aor.PersonID IS NOT NULL
GROUP BY aor.PersonID, aor.Type, aor.CityID, aor.FullAddress
HAVING COUNT(*) > 1;

SELECT aor.ID AS ExtraId, g.KeepId, g.PersonID, g.Type, g.CityID, g.FullAddress, g.DupRowCount AS DupCountInGroup
INTO #Extras
FROM dbo.AddressesOfResidence aor
INNER JOIN #DupGroups g
    ON g.PersonID = aor.PersonID AND g.Type = aor.Type
   AND ISNULL(g.CityID, '00000000-0000-0000-0000-000000000000') = ISNULL(aor.CityID, '00000000-0000-0000-0000-000000000000')
   AND ISNULL(g.FullAddress, '') = ISNULL(aor.FullAddress, '')
WHERE aor.ID <> g.KeepId AND (aor.GCRecord IS NULL OR aor.GCRecord = 0);

DECLARE @GroupCount int = (SELECT COUNT(*) FROM #DupGroups);
DECLARE @ExtraCount int = (SELECT COUNT(*) FROM #Extras);
PRINT CONCAT('Duplicate groups: ', @GroupCount);
PRINT CONCAT('Extras to soft-delete: ', @ExtraCount);

SELECT TOP 50 p.PersonalNumber, p.FirstName, p.LastName, e.Type, e.FullAddress, e.KeepId, e.ExtraId, e.DupCountInGroup,
       (SELECT COUNT(*) FROM ApplicationItems ai WHERE ai.CurrentAddressOfResidenceID = e.ExtraId AND (ai.GCRecord IS NULL OR ai.GCRecord = 0)) AS AppItemsOnExtra
FROM #Extras e
INNER JOIN dbo.People p ON p.ID = e.PersonID
ORDER BY e.DupCountInGroup DESC, p.LastName, p.FirstName;

IF @Apply = 0 BEGIN PRINT 'PREVIEW ONLY'; RETURN; END

BEGIN TRANSACTION;
UPDATE ai SET ai.CurrentAddressOfResidenceID = e.KeepId
FROM dbo.ApplicationItems ai
INNER JOIN #Extras e ON ai.CurrentAddressOfResidenceID = e.ExtraId
WHERE (ai.GCRecord IS NULL OR ai.GCRecord = 0);
UPDATE aor SET aor.GCRecord = 1
FROM dbo.AddressesOfResidence aor
INNER JOIN #Extras e ON e.ExtraId = aor.ID
WHERE aor.GCRecord IS NULL OR aor.GCRecord = 0;
COMMIT TRANSACTION;

SELECT COUNT(*) AS RemainingDuplicateGroups FROM (
  SELECT PersonID, Type, CityID, FullAddress FROM AddressesOfResidence
  WHERE (GCRecord IS NULL OR GCRecord = 0) AND PersonID IS NOT NULL
  GROUP BY PersonID, Type, CityID, FullAddress HAVING COUNT(*) > 1) post;
