-- Visa Extension on Process (P) — PostgreSQL.
DROP VIEW IF EXISTS vw_rd_application_via_ministry_visa_extension_on_process;
CREATE VIEW vw_rd_application_via_ministry_visa_extension_on_process AS
SELECT * FROM vw_rd_application_via_ministry_visa_extension_on_process_base;
