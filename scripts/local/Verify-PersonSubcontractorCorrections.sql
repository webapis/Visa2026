-- Verification after Apply-PersonSubcontractorCorrections.ps1
SET NOCOUNT ON;

PRINT '=== Subcontractor distribution on People ===';
SELECT s.NameTm, COUNT(*) AS PersonCount
FROM dbo.People p
INNER JOIN dbo.Subcontractors s ON p.SubcontractorID = s.ID
GROUP BY s.NameTm
ORDER BY PersonCount DESC;

PRINT '=== Persons still on default only (expect < total after correction) ===';
SELECT COUNT(*) AS PersonsOnDefaultOnly
FROM dbo.People p
INNER JOIN dbo.Subcontractors s ON p.SubcontractorID = s.ID
WHERE s.NameTm = N'Çalyk Energi';

PRINT '=== Distinct subcontractor count ===';
SELECT COUNT(DISTINCT p.SubcontractorID) AS DistinctSubcontractors
FROM dbo.People p;