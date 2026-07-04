-- Verification after Apply-PersonRelationshipCorrections.ps1
SET NOCOUNT ON;

PRINT '=== Family members missing Relationship ===';
SELECT COUNT(*) AS MissingRelationship
FROM dbo.People p
WHERE p.PersonRole = 1 AND p.RelationshipID IS NULL;

PRINT '=== Relationship distribution on family members ===';
SELECT r.NameTm, COUNT(*) AS PersonCount
FROM dbo.People p
INNER JOIN dbo.Relationships r ON p.RelationshipID = r.ID
WHERE p.PersonRole = 1
GROUP BY r.NameTm
ORDER BY PersonCount DESC;