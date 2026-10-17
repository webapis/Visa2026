-- Report Dashboard: Sanaw (Work Permit) — PostgreSQL.
-- Same people as A. Maglumat (vw_rd_work_permit_active). Columns follow the sanaw sheet.
DROP VIEW IF EXISTS vw_rd_work_permit_sanaw;
CREATE VIEW vw_rd_work_permit_sanaw AS
SELECT
    a."ID"                                                                  AS "ID",
    a."PersonOid"                                                           AS "PersonOid",
    COALESCE(NULLIF(BTRIM(p."LastName"), ''), '')                           AS "LastName",
    COALESCE(NULLIF(BTRIM(p."FirstName"), ''), '')                          AS "FirstName",
    a."DateOfBirth"                                                         AS "DateOfBirth",
    COALESCE(NULLIF(BTRIM(p."BirthPlace"), ''), '')                         AS "BirthPlace",
    COALESCE(NULLIF(BTRIM(g."NameTm"), ''), NULLIF(BTRIM(g."Name"), ''), '') AS "GenderLabel",
    a."CitizenshipCode"                                                     AS "CitizenshipCode",
    a."PassportNumber"                                                      AS "PassportNumber",
    a."PassportExpirationDate"                                              AS "PassportExpirationDate",
    a."EducationLevelLabel"                                                 AS "EducationLevelLabel",
    a."EducationInstitutionLabel"                                           AS "EducationInstitutionLabel",
    COALESCE(edu."SpecialtyName", '')                                       AS "SpecialtyLabel",
    COALESCE(visa_pos."Name", '')                                          AS "PositionLabel",
    CASE WHEN (sal."StartDate")::date > DATE '1900-01-01' THEN sal."StartDate" ELSE NULL END AS "SalaryStart",
    CASE WHEN (sal."EndDate")::date > DATE '1900-01-01' THEN sal."EndDate" ELSE NULL END AS "SalaryEnd",
    COALESCE(NULLIF(BTRIM(sal."Amount"), ''), '')                           AS "SalaryAmount",
    a."AddressRegionTm"                                                     AS "AddressRegionTm",
    a."AddressCityTm"                                                       AS "AddressCityTm",
    a."AddressStreet"                                                       AS "AddressStreet",
    CASE
        WHEN COALESCE(NULLIF(BTRIM(fc."Code"), ''), '') <> ''
         AND COALESCE(NULLIF(BTRIM(p."ForeignAddress"), ''), '') <> ''
         AND POSITION(BTRIM(fc."Code") IN p."ForeignAddress") = 0
            THEN BTRIM(fc."Code") || ', ' || BTRIM(p."ForeignAddress")
        WHEN COALESCE(NULLIF(BTRIM(p."ForeignAddress"), ''), '') <> ''
            THEN BTRIM(p."ForeignAddress")
        ELSE COALESCE(NULLIF(BTRIM(fc."Code"), ''), '')
    END                                                                     AS "ForeignAddressLabel",
    a."ProjectName"                                                         AS "ProjectName",
    a."ProjectNameRaw"                                                      AS "ProjectNameRaw",
    a."ProjectNameTm"                                                       AS "ProjectNameTm",
    COALESCE(fam."Label", '')                                               AS "FamilyMembersLabel",
    COALESCE(fam."MemberCount", 0)                                          AS "FamilyMemberCount",
    a."PersonRoleCode"                                                      AS "PersonRoleCode",
    a."StatusLabel"                                                         AS "StatusLabel",
    a."StatusCssClass"                                                      AS "StatusCssClass",
    a."IsArchived"                                                          AS "IsArchived"
FROM vw_rd_work_permit_active a
INNER JOIN "People" p
    ON p."ID" = a."PersonOid" AND COALESCE(p."GCRecord", 0) = 0
LEFT JOIN LATERAL (
    SELECT COALESCE(NULLIF(BTRIM(pos."NameTm"), ''), NULLIF(BTRIM(pos."Name"), ''), '') AS "Name"
    FROM "EmployeePositionHistories" eph
    LEFT JOIN "Positions" pos
        ON pos."ID" = eph."PositionID" AND COALESCE(pos."GCRecord", 0) = 0
    WHERE eph."PersonID" = p."ID"
      AND COALESCE(eph."GCRecord", 0) = 0
    ORDER BY
        CASE
            WHEN eph."EndDate" IS NULL OR (eph."EndDate")::date >= CURRENT_DATE THEN 0
            ELSE 1
        END,
        eph."StartDate" DESC NULLS LAST,
        eph."ID" DESC
    LIMIT 1
) visa_pos ON TRUE
LEFT JOIN "Genders" g
    ON g."ID" = p."GenderID" AND COALESCE(g."GCRecord", 0) = 0
LEFT JOIN "Countries" fc
    ON fc."ID" = p."ForeignAddressCountryID" AND COALESCE(fc."GCRecord", 0) = 0
LEFT JOIN LATERAL (
    SELECT COALESCE(NULLIF(BTRIM(sp."NameTm"), ''), NULLIF(BTRIM(sp."Name"), ''), '') AS "SpecialtyName"
    FROM "Educations" ed
    LEFT JOIN "Specialties" sp
        ON sp."ID" = ed."SpecialtyID" AND COALESCE(sp."GCRecord", 0) = 0
    WHERE ed."PersonID" = p."ID"
      AND COALESCE(ed."GCRecord", 0) = 0
    ORDER BY ed."GraduationYear" DESC NULLS LAST, ed."ID" DESC
    LIMIT 1
) edu ON TRUE
LEFT JOIN LATERAL (
    SELECT s."StartDate", s."EndDate", s."Amount"
    FROM "EmployeeSalaries" s
    WHERE s."PersonID" = p."ID"
      AND COALESCE(s."GCRecord", 0) = 0
    ORDER BY
        CASE
            WHEN s."EndDate" IS NULL OR (s."EndDate")::date >= CURRENT_DATE THEN 0
            ELSE 1
        END,
        s."StartDate" DESC NULLS LAST,
        s."ID" DESC
    LIMIT 1
) sal ON TRUE
LEFT JOIN LATERAL (
    SELECT
        string_agg(
            member."Seq"::text || ' ' ||
            CASE
                WHEN member."RelationName" = '' THEN member."FullName"
                WHEN member."FullName" = '' THEN member."RelationName"
                ELSE member."RelationName" || '-' || member."FullName"
            END,
            E'\n' ORDER BY member."Seq"
        ) AS "Label",
        COUNT(*)::int AS "MemberCount"
    FROM (
        SELECT
            ROW_NUMBER() OVER (
                ORDER BY
                    COALESCE(NULLIF(BTRIM(rel."NameTm"), ''), NULLIF(BTRIM(rel."Name"), ''), ''),
                    COALESCE(NULLIF(BTRIM(fm."LastName"), ''), ''),
                    COALESCE(NULLIF(BTRIM(fm."FirstName"), ''), ''),
                    fm."ID"
            ) AS "Seq",
            COALESCE(NULLIF(BTRIM(rel."NameTm"), ''), NULLIF(BTRIM(rel."Name"), ''), '') AS "RelationName",
            CONCAT_WS(' ',
                NULLIF(BTRIM(fm."FirstName"), ''),
                NULLIF(BTRIM(fm."MiddleName"), ''),
                NULLIF(BTRIM(fm."LastName"), '')
            ) AS "FullName"
        FROM "People" fm
        LEFT JOIN "Relationships" rel
            ON rel."ID" = fm."RelationshipID" AND COALESCE(rel."GCRecord", 0) = 0
        WHERE fm."SponsoringEmployeeID" = p."ID"
          AND COALESCE(fm."GCRecord", 0) = 0
          AND fm."PersonRole" = 1
    ) member
) fam ON TRUE;