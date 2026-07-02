SET NOCOUNT ON;

PRINT '=== Employees with zero AddressOfResidence children (Visa2026) ===';
SELECT COUNT(*) AS employees_without_aor
FROM People p
WHERE p.PersonRole = 0
  AND NOT EXISTS (SELECT 1 FROM AddressOfResidences a WHERE a.PersonID = p.ID);

PRINT '=== ApplicationItems with null CurrentAddressOfResidence (sample) ===';
SELECT TOP 20 ai.ID, ai.PersonID, p.DisplayName
FROM ApplicationItems ai
INNER JOIN People p ON p.ID = ai.PersonID
WHERE ai.CurrentAddressOfResidenceID IS NULL
ORDER BY ai.ID;

PRINT '=== ApplicationItems with address set ===';
SELECT COUNT(*) AS with_address
FROM ApplicationItems
WHERE CurrentAddressOfResidenceID IS NOT NULL;