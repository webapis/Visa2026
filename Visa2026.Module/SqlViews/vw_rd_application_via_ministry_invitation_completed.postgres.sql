-- Invitation Completed (P) — PostgreSQL.
DROP VIEW IF EXISTS vw_rd_application_via_ministry_invitation_completed;
CREATE VIEW vw_rd_application_via_ministry_invitation_completed AS
SELECT * FROM vw_rd_application_via_ministry_invitation_completed_base;
