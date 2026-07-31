-- Report Dashboard: Position History (PostgreSQL).
DROP VIEW IF EXISTS vw_rd_position_history;
CREATE VIEW vw_rd_position_history AS
SELECT
    eph."ID"                                                                AS "ID",
    p."ID"                                                                  AS "PersonOid",
    CONCAT_WS(' ',
        NULLIF(BTRIM(p."FirstName"), ''),
        NULLIF(BTRIM(p."MiddleName"), ''),
        NULLIF(BTRIM(p."LastName"), '')
    )                                                                       AS "PersonName",
    COALESCE(
        NULLIF(BTRIM(pc."NameTm"), ''),
        NULLIF(BTRIM(spc."NameTm"), ''),
        ''
    )                                                                       AS "ProjectName",
    COALESCE(pc."NameTm", spc."NameTm", '')                                 AS "ProjectNameRaw",
    COALESCE(pc."NameTm", spc."NameTm", '')                                 AS "ProjectNameTm",
    p."PersonRole"                                                          AS "PersonRoleCode",
    COALESCE(
        NULLIF(BTRIM(pos."NameTm"), ''),
        NULLIF(BTRIM(pos."Name"), ''),
        'Unknown'
    )                                                                       AS "PositionName",
    eph."StartDate"                                                         AS "StartDate",
    CASE
      WHEN eph."EndDate" IS NULL
        OR (eph."EndDate")::date >= CURRENT_DATE
                                                                              THEN 'Current'
      ELSE                                                                    'Ended'
    END                                                                     AS "StatusLabel",
    CASE
      WHEN eph."EndDate" IS NULL
        OR (eph."EndDate")::date >= CURRENT_DATE
                                                                              THEN 'st-approved'
      ELSE                                                                    'st-pending'
    END                                                                     AS "StatusCssClass",
    COALESCE(
        NULLIF(BTRIM(pos."NameTm"), ''),
        NULLIF(BTRIM(pos."Name"), ''),
        'Unknown'
    )                                                                       AS "PositionLabel",
    COALESCE(
        NULLIF(BTRIM(ap."Name"), ''),
        'Unknown'
    )                                                                       AS "ActualPositionLabel",
    COALESCE(p."IsArchived", FALSE)                                         AS "IsArchived"
FROM "EmployeePositionHistories" eph
INNER JOIN "People" p
    ON p."ID" = eph."PersonID"
   AND COALESCE(p."GCRecord", 0) = 0
LEFT JOIN "ProjectContracts" pc
    ON pc."ID" = p."ProjectContractID" AND COALESCE(pc."GCRecord", 0) = 0
LEFT JOIN "People" sponsor
    ON sponsor."ID" = p."SponsoringEmployeeID" AND COALESCE(sponsor."GCRecord", 0) = 0
LEFT JOIN "ProjectContracts" spc
    ON spc."ID" = sponsor."ProjectContractID" AND COALESCE(spc."GCRecord", 0) = 0
LEFT JOIN "Positions" pos
    ON pos."ID" = eph."PositionID" AND COALESCE(pos."GCRecord", 0) = 0
LEFT JOIN "ActualPositions" ap
    ON ap."ID" = eph."ActualPositionID" AND COALESCE(ap."GCRecord", 0) = 0
WHERE COALESCE(eph."GCRecord", 0) = 0;
