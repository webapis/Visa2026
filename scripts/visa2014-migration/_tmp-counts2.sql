SELECT 'People' AS t, COUNT(*)::bigint AS c FROM "People"
UNION ALL SELECT 'Applications', COUNT(*) FROM "Applications"
UNION ALL SELECT 'ApplicationItems', COUNT(*) FROM "ApplicationItems"
UNION ALL SELECT 'FileData', COUNT(*) FROM "FileData"
UNION ALL SELECT 'PassportDocuments', COUNT(*) FROM "PassportDocuments"
UNION ALL SELECT 'EducationInstitutions', COUNT(*) FROM "EducationInstitutions" WHERE "GCRecord" = 0;