-- Visa Extension Completed (P) — PostgreSQL.
DROP VIEW IF EXISTS vw_rd_application_via_ministry_visa_extension_completed;
CREATE VIEW vw_rd_application_via_ministry_visa_extension_completed AS
SELECT * FROM vw_rd_application_via_ministry_visa_extension_completed_base;
