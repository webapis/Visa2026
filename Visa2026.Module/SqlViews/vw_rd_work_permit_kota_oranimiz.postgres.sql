-- Report Dashboard: kota oranımız — PostgreSQL.
-- One summary row. Foreign = distinct Employees on vw_rd_work_permit_active.
-- InProcess = distinct Employees on a ProduceWorkPermit application that has not
-- issued a work permit for them yet and is not in a terminal state.
DROP VIEW IF EXISTS vw_rd_work_permit_kota_oranimiz;
CREATE VIEW vw_rd_work_permit_kota_oranimiz AS
SELECT
    'a1000000-0000-4000-8000-000000000001'::uuid AS "ID",
    CURRENT_DATE                                 AS "ReportDate",
    (
        SELECT COUNT(*)::int
        FROM vw_rd_work_permit_active a
        WHERE COALESCE(a."PersonRoleCode", 0) = 0
          AND COALESCE(a."IsArchived", FALSE) = FALSE
    )                                            AS "ForeignPersonnelCount",
    (
        SELECT COUNT(*)::int
        FROM (
            SELECT DISTINCT p."ID"
            FROM "ApplicationProfileInstances" a
            INNER JOIN "ApplicationProfiles" apf
                ON apf."ID" = a."ApplicationProfileID"
               AND COALESCE(apf."GCRecord", 0) = 0
               AND COALESCE(apf."ProduceWorkPermit", FALSE) = TRUE
            INNER JOIN "ApplicationProfileInstancePeople" roster
                ON roster."ApplicationProfileInstanceId" = a."ID"
            INNER JOIN "People" p
                ON p."ID" = roster."PersonId"
               AND COALESCE(p."GCRecord", 0) = 0
               AND COALESCE(p."IsArchived", FALSE) = FALSE
               AND COALESCE(p."PersonRole", 0) = 0
            LEFT JOIN LATERAL (
                SELECT ap."StateID"
                FROM "ApplicationProfileInstanceProgresses" ap
                WHERE ap."ApplicationProfileInstanceID" = a."ID"
                  AND COALESCE(ap."GCRecord", 0) = 0
                ORDER BY ap."Date" DESC NULLS LAST, ap."ID" DESC
                LIMIT 1
            ) latest_ap ON TRUE
            LEFT JOIN "ApplicationStates" ast
                ON ast."ID" = latest_ap."StateID"
               AND COALESCE(ast."GCRecord", 0) = 0
            WHERE COALESCE(a."GCRecord", 0) = 0
              AND NOT EXISTS (
                    SELECT 1
                    FROM "WorkPermits" wp
                    INNER JOIN "WorkPermitItems" wpi
                        ON wpi."WorkPermitID" = wp."ID"
                       AND COALESCE(wpi."GCRecord", 0) = 0
                       AND wpi."PersonID" = p."ID"
                    WHERE wp."ApplicationProfileInstanceID" = a."ID"
                      AND COALESCE(wp."GCRecord", 0) = 0)
              AND (
                    ast."Code" IS NULL
                    OR ast."Code" NOT IN (
                        'PROCESS_ISSUED',
                        'PROCESS_REJECTED',
                        'PROCESS_CANCELLED',
                        '1_REVIEW_REJECTED',
                        '2_REVIEW_REJECTED',
                        '3_REVIEW_REJECTED',
                        '4_REVIEW_REJECTED',
                        '5_REVIEW_REJECTED')
                  )
              AND (
                    BTRIM(COALESCE(a."LatestPrimaryStateCode", '')) = ''
                    OR BTRIM(a."LatestPrimaryStateCode") NOT IN (
                        'PROCESS_ISSUED',
                        'PROCESS_REJECTED',
                        'PROCESS_CANCELLED',
                        '1_REVIEW_REJECTED',
                        '2_REVIEW_REJECTED',
                        '3_REVIEW_REJECTED',
                        '4_REVIEW_REJECTED',
                        '5_REVIEW_REJECTED')
                  )
        ) in_process_people
    )                                            AS "InProcessCount";