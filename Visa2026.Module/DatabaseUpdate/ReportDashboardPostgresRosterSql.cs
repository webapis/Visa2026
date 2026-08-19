namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Shared PostgreSQL fragments for Report Dashboard views: <c>ApplicationProfileInstancePeople</c> +
/// <see cref="BusinessObjects.ApplicationProfileInstancePersonResolvedLink"/> only (no ApplicationItems).
/// </summary>
internal static class ReportDashboardPostgresRosterSql
{
    internal const int LinkKindPassport = 0;
    internal const int LinkKindVisa = 1;
    internal const int LinkKindEducation = 2;
    internal const int LinkKindAddressOfResidence = 3;
    internal const int LinkKindPosition = 4;
    internal const int LinkKindMedicalRecord = 6;
    internal const int LinkKindWorkPermitItem = 8;
    internal const int LinkKindTravelHistory = 11;

    /// <summary>
    /// Profile-based visa-extension bucket (deprecated ApplicationType.Name list retired for dashboard SQL).
    /// Issuance + ProduceVisa + RequirePersonVisa, not invitation.
    /// </summary>
    internal const string VisaExtensionProfilePredicate = @"
            COALESCE(apf.""ProduceVisa"", FALSE) = TRUE
            AND COALESCE(apf.""RequirePersonVisa"", FALSE) = TRUE
            AND COALESCE(apf.""ProduceInvitation"", FALSE) = FALSE
            AND COALESCE(apf.""ActionFamily"", 0) = 0";

    /// <summary>Profile produce flags for WP extension (Calik extend_visa_wp / visa+WP extend).</summary>
    internal const string WorkPermitExtensionProfilePredicate = @"
            COALESCE(apf.""ProduceWorkPermit"", FALSE) = TRUE
            AND (
                apf.""Code"" IN ('extend_visa_wp', 'app-wp-ext', 'app-visa-and-wp-ext')
                OR (
                    COALESCE(apf.""ProduceVisa"", FALSE) = TRUE
                    AND COALESCE(apf.""RequirePersonVisa"", FALSE) = TRUE
                )
            )";

    /// <summary>Registration ActionFamily (enum value 2).</summary>
    internal const string RegistrationProfilePredicate = @"
            COALESCE(apf.""ActionFamily"", 0) = 2";

    /// <summary>RegistrationKind CheckIn (enum value 1). Dashboard views not switched yet.</summary>
    internal const string RegistrationCheckInProfilePredicate = @"
            COALESCE(apf.""ActionFamily"", 0) = 2
            AND COALESCE(apf.""RegistrationKind"", 0) = 1";

    /// <summary>RegistrationKind CheckOut (enum value 2). Dashboard views not switched yet.</summary>
    internal const string RegistrationCheckOutProfilePredicate = @"
            COALESCE(apf.""ActionFamily"", 0) = 2
            AND COALESCE(apf.""RegistrationKind"", 0) = 2";

    /// <summary>RegistrationKind InfoChange (enum value 3). Dashboard views not switched yet.</summary>
    internal const string RegistrationInfoChangeProfilePredicate = @"
            COALESCE(apf.""ActionFamily"", 0) = 2
            AND COALESCE(apf.""RegistrationKind"", 0) = 3";

    /// <summary>Calik check-out profile code (App_Reg_Check_Out*). Prefer RegistrationKind when views are rewired.</summary>
    internal const string CheckoutProfilePredicate = @"
            apf.""Code"" = 'check_out'";

    /// <summary>LATERAL join: first person on an application (M2M roster).</summary>
    internal const string FirstApplicationProfileInstancePersonLateralJoin = @"
LEFT JOIN LATERAL (
    SELECT ap_row.""PersonId""
    FROM ""ApplicationProfileInstancePeople"" ap_row
    WHERE ap_row.""ApplicationProfileInstanceId"" = a.""ID""
    ORDER BY ap_row.""PersonId""
    LIMIT 1
) first_m2m ON TRUE";

    internal static string CteVisaExtensionRosterLines(string cteName = "visa_ext_roster") => $@"
{cteName} AS (
    SELECT
        md5(concat(ap.""ApplicationProfileInstanceId""::text, ap.""PersonId""::text))::uuid AS ""LineId"",
        a.""ID"" AS ""ApplicationProfileInstanceID"",
        ap.""PersonId"" AS ""PersonID"",
        rl_visa.""LinkedObjectId"" AS ""ExpiringVisaID"",
        rl_pass.""LinkedObjectId"" AS ""PassportID""
    FROM ""ApplicationProfileInstancePeople"" ap
    INNER JOIN ""ApplicationProfileInstances"" a
        ON a.""ID"" = ap.""ApplicationProfileInstanceId"" AND COALESCE(a.""GCRecord"", 0) = 0
    INNER JOIN ""ApplicationProfiles"" apf
        ON apf.""ID"" = a.""ApplicationProfileID"" AND COALESCE(apf.""GCRecord"", 0) = 0
    INNER JOIN ""ApplicationProfileInstancePersonResolvedLinks"" rl_visa
        ON rl_visa.""ApplicationProfileInstanceId"" = ap.""ApplicationProfileInstanceId"" AND rl_visa.""PersonId"" = ap.""PersonId""
       AND rl_visa.""LinkKind"" = {LinkKindVisa}
       AND rl_visa.""LinkedObjectId"" IS NOT NULL
       AND COALESCE(rl_visa.""GCRecord"", 0) = 0
    LEFT JOIN ""ApplicationProfileInstancePersonResolvedLinks"" rl_pass
        ON rl_pass.""ApplicationProfileInstanceId"" = ap.""ApplicationProfileInstanceId"" AND rl_pass.""PersonId"" = ap.""PersonId""
       AND rl_pass.""LinkKind"" = {LinkKindPassport}
       AND COALESCE(rl_pass.""GCRecord"", 0) = 0
    WHERE {VisaExtensionProfilePredicate}
)";

    internal static string CteWorkPermitExtensionRosterLines(string cteName = "wp_ext_roster") => $@"
{cteName} AS (
    SELECT
        md5(concat(ap.""ApplicationProfileInstanceId""::text, ap.""PersonId""::text))::uuid AS ""LineId"",
        a.""ID"" AS ""ApplicationProfileInstanceID"",
        ap.""PersonId"" AS ""PersonID"",
        rl_wp.""LinkedObjectId"" AS ""WorkPermitItemID""
    FROM ""ApplicationProfileInstancePeople"" ap
    INNER JOIN ""ApplicationProfileInstances"" a
        ON a.""ID"" = ap.""ApplicationProfileInstanceId"" AND COALESCE(a.""GCRecord"", 0) = 0
    INNER JOIN ""ApplicationProfiles"" apf
        ON apf.""ID"" = a.""ApplicationProfileID"" AND COALESCE(apf.""GCRecord"", 0) = 0
    INNER JOIN ""ApplicationProfileInstancePersonResolvedLinks"" rl_wp
        ON rl_wp.""ApplicationProfileInstanceId"" = ap.""ApplicationProfileInstanceId"" AND rl_wp.""PersonId"" = ap.""PersonId""
       AND rl_wp.""LinkKind"" = {LinkKindWorkPermitItem}
       AND rl_wp.""LinkedObjectId"" IS NOT NULL
       AND COALESCE(rl_wp.""GCRecord"", 0) = 0
    WHERE {WorkPermitExtensionProfilePredicate}
)";

    internal static string UnfinishedExtensionPeopleCte(string rosterCteName = "visa_ext_roster") => $@"
unfinished_extension_people AS (
    SELECT DISTINCT roster.""PersonID""
    FROM {rosterCteName} roster
    INNER JOIN ""ApplicationProfileInstances"" a
        ON a.""ID"" = roster.""ApplicationProfileInstanceID"" AND COALESCE(a.""GCRecord"", 0) = 0
    WHERE roster.""ExpiringVisaID"" IS NOT NULL
      AND roster.""PersonID"" IS NOT NULL
      AND (
          a.""LatestPrimaryStateCode"" IS NULL
          OR BTRIM(a.""LatestPrimaryStateCode"") = ''
          OR (
               a.""LatestPrimaryStateCode"" NOT IN ('PROCESS_ISSUED', 'PROCESS_CANCELLED', 'PROCESS_REJECTED')
               AND RIGHT(BTRIM(a.""LatestPrimaryStateCode""), 16) <> '_REVIEW_REJECTED'
             )
      )
)";

    internal static string IssuedVisaIdSubquery(string rosterAlias = "roster") => $@"
(SELECT iv.""ID"" FROM ""Visas"" iv
 WHERE iv.""IssuingApplicationProfileInstanceID"" = {rosterAlias}.""ApplicationProfileInstanceID""
   AND {rosterAlias}.""PassportID"" IS NOT NULL
   AND iv.""PassportID"" = {rosterAlias}.""PassportID""
 LIMIT 1)";

    internal static string ViewVisaExtensionStatusSql => $@"
-- Canonical View_VisaExtensionStatus definition (PostgreSQL, M2M roster only).
CREATE VIEW ""View_VisaExtensionStatus"" AS
WITH {CteVisaExtensionRosterLines()}
SELECT
    roster.""LineId"" AS ""ID"",
    roster.""ApplicationProfileInstanceID"",
    roster.""ExpiringVisaID"",
    roster.""PersonID"",
    roster.""PassportID"",
    a.""ApplicationNumber"",
    a.""ApplicationDate"",
    latest_ap.""StateID"" AS ""CurrentStateID"",
    latest_ap.""Date"" AS ""StatusDate"",
    latest_ap.""Description"" AS ""StatusDescription"",
    CASE
        WHEN COALESCE(v.""IsCancelled"", FALSE) THEN 0
        WHEN v.""ExpirationDate"" IS NULL THEN 0
        WHEN (v.""ExpirationDate""::date - CURRENT_DATE) < 0 THEN 0
        ELSE (v.""ExpirationDate""::date - CURRENT_DATE)
    END AS ""DaysRemainingOnVisa"",
    {IssuedVisaIdSubquery()} AS ""IssuedVisaID"",
    (SELECT ri.""ID""
     FROM ""Rejections"" r
     JOIN ""RejectionItems"" ri ON ri.""RejectionID"" = r.""ID""
     WHERE r.""ApplicationProfileInstanceID"" = a.""ID"" AND ri.""PersonID"" = roster.""PersonID""
     LIMIT 1) AS ""RejectionItemID""
FROM visa_ext_roster roster
JOIN ""ApplicationProfileInstances"" a ON roster.""ApplicationProfileInstanceID"" = a.""ID""
LEFT JOIN ""Visas"" v ON roster.""ExpiringVisaID"" = v.""ID""
LEFT JOIN LATERAL (
    SELECT ap.""StateID"", ap.""Date"", ap.""Description""
    FROM ""ApplicationProfileInstanceProgresses"" ap
    WHERE ap.""ApplicationProfileInstanceID"" = a.""ID""
    ORDER BY ap.""Date"" DESC NULLS LAST, ap.""ID"" DESC
    LIMIT 1
) latest_ap ON TRUE;
";

    internal static string VisaAppProgressViewSql => $@"
-- Report Dashboard: Visa — On Extension (PostgreSQL).
-- Shared by dashboard preview and Open ListView (VwRdVisaAppProgress).
CREATE VIEW vw_rd_visa_app_progress AS
WITH {CteVisaExtensionRosterLines()}
SELECT
    roster.""LineId""                                                                 AS ""ID"",
    a.""ID""                                                                          AS ""ApplicationProfileInstanceOid"",
    p.""ID""                                                                          AS ""PersonOid"",
    roster.""ExpiringVisaID""                                                         AS ""ExpiringVisaID"",
    roster.""PassportID""                                                             AS ""PassportID"",
    COALESCE(NULLIF(BTRIM(pp.""PassportNumber""), ''), '')                             AS ""PassportNumber"",
    latest_ap.""StateID""                                                             AS ""CurrentStateID"",
    CONCAT_WS(' ',
        NULLIF(BTRIM(p.""FirstName""), ''),
        NULLIF(BTRIM(p.""MiddleName""), ''),
        NULLIF(BTRIM(p.""LastName""), '')
    )                                                                               AS ""PersonName"",
    COALESCE(
        NULLIF(BTRIM(pc.""NameTm""), ''),
        NULLIF(BTRIM(spc.""NameTm""), ''),
        ''
    )                                                                               AS ""ProjectName"",
    COALESCE(pc.""NameTm"", spc.""NameTm"", '')                                       AS ""ProjectNameRaw"",
    COALESCE(pc.""NameTm"", spc.""NameTm"", '')                                       AS ""ProjectNameTm"",
    p.""PersonRole""                                                                  AS ""PersonRoleCode"",
    COALESCE(
        NULLIF(BTRIM(a.""FullApplicationNumber""), ''),
        NULLIF(BTRIM(a.""ApplicationNumber""), ''),
        ''
    )                                                                               AS ""ApplicationNumber"",
    a.""ApplicationDate""                                                             AS ""ApplicationDate"",
    latest_ap.""Date""                                                                AS ""StatusDate"",
    COALESCE(
        NULLIF(BTRIM(a.""LatestPrimaryStateCode""), ''),
        NULLIF(BTRIM(ast.""Code""), ''),
        ''
    )                                                                               AS ""ProgressStateCode"",
    COALESCE(
        NULLIF(BTRIM(a.""LatestProgressDisplay""), ''),
        NULLIF(BTRIM(ast.""Name""), ''),
        NULLIF(BTRIM(ast.""NameTm""), ''),
        'Being Prepared'
    )                                                                               AS ""ProgressStateLabel"",
    CASE
      WHEN COALESCE(NULLIF(BTRIM(a.""LatestPrimaryStateCode""), ''), NULLIF(BTRIM(ast.""Code""), ''), '')
           IN ('PROCESS_ISSUED', '1_REVIEW_APPROVED', '2_REVIEW_APPROVED')
                                                                             THEN 'st-approved'
      WHEN COALESCE(NULLIF(BTRIM(a.""LatestPrimaryStateCode""), ''), NULLIF(BTRIM(ast.""Code""), ''), '')
           IN ('PROCESS_REJECTED', 'PROCESS_CANCELLED', '1_REVIEW_REJECTED', '2_REVIEW_REJECTED')
           OR RIGHT(COALESCE(NULLIF(BTRIM(a.""LatestPrimaryStateCode""), ''), NULLIF(BTRIM(ast.""Code""), ''), ''), 16)
              = '_REVIEW_REJECTED'
                                                                             THEN 'st-expiring'
      ELSE                                                                   'st-pending'
    END                                                                             AS ""ProgressStateCssClass"",
    CASE
        WHEN COALESCE(v.""IsCancelled"", FALSE) THEN 0
        WHEN v.""ExpirationDate"" IS NULL THEN 0
        WHEN (v.""ExpirationDate""::date - CURRENT_DATE) < 0 THEN 0
        ELSE (v.""ExpirationDate""::date - CURRENT_DATE)
    END                                                                             AS ""DaysRemainingOnVisa"",
    COALESCE(p.""IsArchived"", FALSE)                                                 AS ""IsArchived""
FROM visa_ext_roster roster
INNER JOIN ""ApplicationProfileInstances"" a
    ON a.""ID"" = roster.""ApplicationProfileInstanceID"" AND COALESCE(a.""GCRecord"", 0) = 0
INNER JOIN ""People"" p
    ON p.""ID"" = roster.""PersonID"" AND COALESCE(p.""GCRecord"", 0) = 0
LEFT JOIN ""ProjectContracts"" pc
    ON pc.""ID"" = COALESCE(a.""ProjectContractID"", p.""ProjectContractID"")
   AND COALESCE(pc.""GCRecord"", 0) = 0
LEFT JOIN ""People"" sp
    ON sp.""ID"" = p.""SponsoringEmployeeID"" AND COALESCE(sp.""GCRecord"", 0) = 0
LEFT JOIN ""ProjectContracts"" spc
    ON spc.""ID"" = sp.""ProjectContractID"" AND COALESCE(spc.""GCRecord"", 0) = 0
LEFT JOIN ""Visas"" v
    ON v.""ID"" = roster.""ExpiringVisaID"" AND COALESCE(v.""GCRecord"", 0) = 0
LEFT JOIN ""Passports"" pp
    ON pp.""ID"" = roster.""PassportID"" AND COALESCE(pp.""GCRecord"", 0) = 0
LEFT JOIN LATERAL (
    SELECT ap.""StateID"", ap.""Date""
    FROM ""ApplicationProfileInstanceProgresses"" ap
    WHERE ap.""ApplicationProfileInstanceID"" = a.""ID""
      AND COALESCE(ap.""GCRecord"", 0) = 0
    ORDER BY ap.""Date"" DESC NULLS LAST, ap.""ID"" DESC
    LIMIT 1
) latest_ap ON TRUE
LEFT JOIN ""ApplicationStates"" ast
    ON ast.""ID"" = latest_ap.""StateID"" AND COALESCE(ast.""GCRecord"", 0) = 0;
";

    internal static string WorkPermitAppProgressViewSql => $@"
-- Report Dashboard: WorkPermit Extension / Extension Result (PostgreSQL).
CREATE VIEW vw_rd_work_permit_app_progress AS
WITH {CteWorkPermitExtensionRosterLines()}
SELECT
    roster.""LineId""                                                                 AS ""ID"",
    a.""ID""                                                                          AS ""ApplicationProfileInstanceOid"",
    p.""ID""                                                                          AS ""PersonOid"",
    CONCAT_WS(' ',
        NULLIF(BTRIM(p.""FirstName""), ''),
        NULLIF(BTRIM(p.""MiddleName""), ''),
        NULLIF(BTRIM(p.""LastName""), '')
    )                                                                               AS ""PersonName"",
    COALESCE(
        NULLIF(BTRIM(pc.""NameTm""), ''),
        NULLIF(BTRIM(spc.""NameTm""), ''),
        ''
    )                                                                               AS ""ProjectName"",
    COALESCE(pc.""NameTm"", spc.""NameTm"", '')                                       AS ""ProjectNameRaw"",
    COALESCE(pc.""NameTm"", spc.""NameTm"", '')                                       AS ""ProjectNameTm"",
    p.""PersonRole""                                                                  AS ""PersonRoleCode"",
    COALESCE(
        NULLIF(BTRIM(a.""FullApplicationNumber""), ''),
        NULLIF(BTRIM(a.""ApplicationNumber""), ''),
        ''
    )                                                                               AS ""ApplicationNumber"",
    a.""ApplicationDate""                                                             AS ""ApplicationDate"",
    COALESCE(
        NULLIF(BTRIM(a.""LatestPrimaryStateCode""), ''),
        NULLIF(BTRIM(ast.""Code""), ''),
        ''
    )                                                                               AS ""ProgressStateCode"",
    COALESCE(
        NULLIF(BTRIM(a.""LatestProgressDisplay""), ''),
        NULLIF(BTRIM(ast.""Name""), ''),
        NULLIF(BTRIM(ast.""NameTm""), ''),
        'Being Prepared'
    )                                                                               AS ""ProgressStateLabel"",
    CASE
      WHEN ast.""Code"" IN ('PROCESS_ISSUED', '1_REVIEW_APPROVED', '2_REVIEW_APPROVED')
                                                                             THEN 'st-approved'
      WHEN ast.""Code"" IN ('PROCESS_REJECTED', 'PROCESS_CANCELLED', '1_REVIEW_REJECTED', '2_REVIEW_REJECTED')
                                                                             THEN 'st-expiring'
      ELSE                                                                   'st-pending'
    END                                                                             AS ""ProgressStateCssClass"",
    COALESCE(p.""IsArchived"", FALSE)                                                 AS ""IsArchived""
FROM wp_ext_roster roster
INNER JOIN ""ApplicationProfileInstances"" a
    ON a.""ID"" = roster.""ApplicationProfileInstanceID"" AND COALESCE(a.""GCRecord"", 0) = 0
INNER JOIN ""People"" p
    ON p.""ID"" = roster.""PersonID"" AND COALESCE(p.""GCRecord"", 0) = 0
LEFT JOIN ""ProjectContracts"" pc
    ON pc.""ID"" = COALESCE(a.""ProjectContractID"", p.""ProjectContractID"")
   AND COALESCE(pc.""GCRecord"", 0) = 0
LEFT JOIN ""People"" sp
    ON sp.""ID"" = p.""SponsoringEmployeeID"" AND COALESCE(sp.""GCRecord"", 0) = 0
LEFT JOIN ""ProjectContracts"" spc
    ON spc.""ID"" = sp.""ProjectContractID"" AND COALESCE(spc.""GCRecord"", 0) = 0
LEFT JOIN LATERAL (
    SELECT ap.""StateID""
    FROM ""ApplicationProfileInstanceProgresses"" ap
    WHERE ap.""ApplicationProfileInstanceID"" = a.""ID""
      AND COALESCE(ap.""GCRecord"", 0) = 0
    ORDER BY ap.""Date"" DESC, ap.""ID"" DESC
    LIMIT 1
) latest_ap ON TRUE
LEFT JOIN ""ApplicationStates"" ast
    ON ast.""ID"" = latest_ap.""StateID"" AND COALESCE(ast.""GCRecord"", 0) = 0;
";

    internal static string VisaStateViewSql => $@"
-- Report Dashboard: Visa State — Extension Started (PostgreSQL).
CREATE VIEW vw_rd_visa_state AS
WITH ranked_visas AS (
    SELECT
        v.""ID"" AS ""VisaID"",
        pp.""PersonID"",
        v.""VisaNumber"",
        v.""ExpirationDate"",
        v.""StartDate"",
        v.""IssueDate"",
        ROW_NUMBER() OVER (
            PARTITION BY pp.""PersonID""
            ORDER BY v.""StartDate"" DESC NULLS LAST, v.""IssueDate"" DESC NULLS LAST, v.""ID"" DESC
        ) AS rn
    FROM ""Visas"" v
    INNER JOIN ""Passports"" pp
        ON pp.""ID"" = v.""PassportID""
       AND COALESCE(pp.""GCRecord"", 0) = 0
    WHERE COALESCE(v.""GCRecord"", 0) = 0
      AND COALESCE(v.""IsCancelled"", FALSE) = FALSE
      AND v.""StartDate"" IS NOT NULL
      AND (v.""StartDate"")::date > DATE '1900-01-01'
      AND (v.""StartDate"")::date <= CURRENT_DATE
),
{CteVisaExtensionRosterLines()}
SELECT
    roster.""LineId""                                                   AS ""ID"",
    p.""ID""                                                              AS ""PersonOid"",
    CONCAT_WS(' ',
        NULLIF(BTRIM(p.""FirstName""), ''),
        NULLIF(BTRIM(p.""MiddleName""), ''),
        NULLIF(BTRIM(p.""LastName""), '')
    )                                                                   AS ""PersonName"",
    COALESCE(
        NULLIF(BTRIM(pc.""NameTm""), ''),
        NULLIF(BTRIM(spc.""NameTm""), ''),
        ''
    )                                                                   AS ""ProjectName"",
    COALESCE(pc.""NameTm"", spc.""NameTm"", '')                             AS ""ProjectNameRaw"",
    COALESCE(pc.""NameTm"", spc.""NameTm"", '')                             AS ""ProjectNameTm"",
    p.""PersonRole""                                                      AS ""PersonRoleCode"",
    COALESCE(NULLIF(BTRIM(rv.""VisaNumber""), ''), '')                    AS ""VisaNumber"",
    CASE WHEN (rv.""ExpirationDate"")::date > DATE '1900-01-01' THEN rv.""ExpirationDate"" ELSE NULL END AS ""ExpirationDate"",
    'Extension Started'                                                 AS ""StateLabel"",
    'st-pending'                                                        AS ""StateCssClass"",
    COALESCE(p.""IsArchived"", FALSE)                                     AS ""IsArchived""
FROM visa_ext_roster roster
INNER JOIN ranked_visas rv
    ON rv.""VisaID"" = roster.""ExpiringVisaID""
   AND rv.""PersonID"" = roster.""PersonID""
   AND rv.rn = 1
INNER JOIN ""ApplicationProfileInstances"" a
    ON a.""ID"" = roster.""ApplicationProfileInstanceID"" AND COALESCE(a.""GCRecord"", 0) = 0
INNER JOIN ""People"" p
    ON p.""ID"" = roster.""PersonID""
   AND COALESCE(p.""GCRecord"", 0) = 0
LEFT JOIN ""ProjectContracts"" pc
    ON pc.""ID"" = COALESCE(a.""ProjectContractID"", p.""ProjectContractID"")
   AND COALESCE(pc.""GCRecord"", 0) = 0
LEFT JOIN ""People"" sp
    ON sp.""ID"" = p.""SponsoringEmployeeID""
   AND COALESCE(sp.""GCRecord"", 0) = 0
LEFT JOIN ""ProjectContracts"" spc
    ON spc.""ID"" = sp.""ProjectContractID""
   AND COALESCE(spc.""GCRecord"", 0) = 0
WHERE rv.""ExpirationDate"" IS NOT NULL
  AND (rv.""ExpirationDate"")::date >= CURRENT_DATE
  AND NOT EXISTS (
        SELECT 1
        FROM ""ApplicationProfileInstanceProgresses"" ap
        INNER JOIN ""ApplicationStates"" ast
            ON ast.""ID"" = ap.""StateID""
           AND COALESCE(ast.""GCRecord"", 0) = 0
        WHERE ap.""ApplicationProfileInstanceID"" = roster.""ApplicationProfileInstanceID""
          AND COALESCE(ap.""GCRecord"", 0) = 0
          AND ast.""Code"" = 'PROCESS_CANCELLED'
      );
";

    internal static string CteRegLinkedVisaIds() => $@"
reg_linked AS (
    SELECT DISTINCT rl.""LinkedObjectId"" AS ""VisaId""
    FROM ""ApplicationProfileInstancePersonResolvedLinks"" rl
    INNER JOIN ""ApplicationProfileInstancePeople"" ap
        ON ap.""ApplicationProfileInstanceId"" = rl.""ApplicationProfileInstanceId"" AND ap.""PersonId"" = rl.""PersonId""
    INNER JOIN ""ApplicationProfileInstances"" a
        ON a.""ID"" = ap.""ApplicationProfileInstanceId"" AND COALESCE(a.""GCRecord"", 0) = 0
    INNER JOIN ""ApplicationProfiles"" apf
        ON apf.""ID"" = a.""ApplicationProfileID"" AND COALESCE(apf.""GCRecord"", 0) = 0
    WHERE COALESCE(rl.""GCRecord"", 0) = 0
      AND rl.""LinkKind"" = {LinkKindVisa}
      AND rl.""LinkedObjectId"" IS NOT NULL
      AND {RegistrationProfilePredicate}
)";

    internal static string CteCheckoutLinkedVisaIds() => $@"
checkout_linked AS (
    SELECT DISTINCT rl.""LinkedObjectId"" AS ""VisaId""
    FROM ""ApplicationProfileInstancePersonResolvedLinks"" rl
    INNER JOIN ""ApplicationProfileInstancePeople"" ap
        ON ap.""ApplicationProfileInstanceId"" = rl.""ApplicationProfileInstanceId"" AND ap.""PersonId"" = rl.""PersonId""
    INNER JOIN ""ApplicationProfileInstances"" a
        ON a.""ID"" = ap.""ApplicationProfileInstanceId"" AND COALESCE(a.""GCRecord"", 0) = 0
    INNER JOIN ""ApplicationProfiles"" apf
        ON apf.""ID"" = a.""ApplicationProfileID"" AND COALESCE(apf.""GCRecord"", 0) = 0
    WHERE COALESCE(rl.""GCRecord"", 0) = 0
      AND rl.""LinkKind"" = {LinkKindVisa}
      AND rl.""LinkedObjectId"" IS NOT NULL
      AND {CheckoutProfilePredicate}
)";

    internal static string RegistrationViewSql => $@"
-- Report Dashboard: Registration category (PostgreSQL).
-- One row per not-expired visa: latest registration ApplicationProfileInstance via roster visa link (M2M + legacy fallback).
CREATE VIEW vw_rd_registration AS
WITH roster_lines AS (
    SELECT
        md5(concat(ap.""ApplicationProfileInstanceId""::text, ap.""PersonId""::text))::uuid AS ""ID"",
        p.""ID"" AS ""PersonOid"",
        CONCAT_WS(' ',
            NULLIF(BTRIM(p.""FirstName""), ''),
            NULLIF(BTRIM(p.""MiddleName""), ''),
            NULLIF(BTRIM(p.""LastName""), '')
        ) AS ""PersonName"",
        COALESCE(
            NULLIF(BTRIM(pc.""NameTm""), ''),
            NULLIF(BTRIM(spc.""NameTm""), ''),
            ''
        ) AS ""ProjectName"",
        COALESCE(pc.""NameTm"", spc.""NameTm"", '') AS ""ProjectNameRaw"",
        COALESCE(pc.""NameTm"", spc.""NameTm"", '') AS ""ProjectNameTm"",
        p.""PersonRole"" AS ""PersonRoleCode"",
        COALESCE(NULLIF(BTRIM(v.""VisaNumber""), ''), '') AS ""VisaNumber"",
        v.""ExpirationDate"" AS ""VisaExpirationDate"",
        COALESCE(
            NULLIF(BTRIM(a.""FullApplicationNumber""), ''),
            NULLIF(BTRIM(a.""ApplicationNumber""), ''),
            ''
        ) AS ""ApplicationNumber"",
        a.""ApplicationDate"" AS ""ApplicationDate"",
        apf.""Code"" AS ""ApplicationTypeName"",
        COALESCE(
            NULLIF(BTRIM(apf.""Name""), ''),
            NULLIF(BTRIM(apf.""Code""), ''),
            'Unknown'
        ) AS ""ApplicationTypeLabel"",
        COALESCE(
            NULLIF(BTRIM(ast.""NameTm""), ''),
            NULLIF(BTRIM(ast.""Name""), ''),
            'OFISDE'
        ) AS ""ProgressStateLabel"",
        CASE
            WHEN ast.""Code"" IN ('PROCESS_ISSUED') THEN 'st-approved'
            WHEN ast.""Code"" IN ('PROCESS_REJECTED', 'PROCESS_CANCELLED') THEN 'st-expiring'
            WHEN ast.""Code"" IS NULL THEN 'st-pending'
            ELSE 'st-pending'
        END AS ""ProgressStateCssClass"",
        COALESCE(ast.""Code"", 'AT_OFFICE') AS ""ProgressStateCode"",
        ((v.""ExpirationDate"")::date - CURRENT_DATE) AS ""DaysRemaining"",
        CASE
            WHEN (v.""ExpirationDate"")::date - CURRENT_DATE < 0   THEN 'Expired'
            WHEN (v.""ExpirationDate"")::date - CURRENT_DATE < 7   THEN '< 7 days'
            WHEN (v.""ExpirationDate"")::date - CURRENT_DATE < 14  THEN '< 14 days'
            WHEN (v.""ExpirationDate"")::date - CURRENT_DATE < 30  THEN '< 1 month'
            WHEN (v.""ExpirationDate"")::date - CURRENT_DATE < 90  THEN '< 3 months'
            WHEN (v.""ExpirationDate"")::date - CURRENT_DATE < 180 THEN '< 6 months'
            ELSE '≥ 6 months'
        END AS ""ExpiryBucketLabel"",
        CASE
            WHEN (v.""ExpirationDate"")::date - CURRENT_DATE < 0   THEN 'st-expiring'
            WHEN (v.""ExpirationDate"")::date - CURRENT_DATE < 14  THEN 'st-expiring'
            WHEN (v.""ExpirationDate"")::date - CURRENT_DATE < 90  THEN 'st-pending'
            ELSE 'st-approved'
        END AS ""ExpiryBucketCssClass"",
        COALESCE(p.""IsArchived"", FALSE) AS ""IsArchived"",
        COALESCE(
            NULLIF(BTRIM(city.""NameTm""), ''),
            NULLIF(BTRIM(city.""Name""), ''),
            'Unknown city'
        ) AS ""CityLabel"",
        v.""ID"" AS ""VisaId"",
        a.""ID"" AS ""ApplicationProfileInstanceOid""
    FROM ""Visas"" v
    INNER JOIN ""Passports"" pp
        ON pp.""ID"" = v.""PassportID"" AND COALESCE(pp.""GCRecord"", 0) = 0
    INNER JOIN ""People"" p
        ON p.""ID"" = pp.""PersonID"" AND COALESCE(p.""GCRecord"", 0) = 0
    INNER JOIN ""ApplicationProfileInstancePersonResolvedLinks"" rl_visa
        ON rl_visa.""LinkKind"" = {LinkKindVisa}
       AND rl_visa.""LinkedObjectId"" = v.""ID""
       AND COALESCE(rl_visa.""GCRecord"", 0) = 0
    INNER JOIN ""ApplicationProfileInstancePeople"" ap
        ON ap.""ApplicationProfileInstanceId"" = rl_visa.""ApplicationProfileInstanceId"" AND ap.""PersonId"" = rl_visa.""PersonId""
       AND ap.""PersonId"" = p.""ID""
    INNER JOIN ""ApplicationProfileInstances"" a
        ON a.""ID"" = ap.""ApplicationProfileInstanceId"" AND COALESCE(a.""GCRecord"", 0) = 0
    INNER JOIN ""ApplicationProfiles"" apf
        ON apf.""ID"" = a.""ApplicationProfileID"" AND COALESCE(apf.""GCRecord"", 0) = 0
    LEFT JOIN ""ApplicationProfileInstancePersonResolvedLinks"" rl_addr
        ON rl_addr.""ApplicationProfileInstanceId"" = ap.""ApplicationProfileInstanceId"" AND rl_addr.""PersonId"" = ap.""PersonId""
       AND rl_addr.""LinkKind"" = {LinkKindAddressOfResidence}
       AND COALESCE(rl_addr.""GCRecord"", 0) = 0
    LEFT JOIN ""AddressesOfResidence"" addr
        ON addr.""ID"" = rl_addr.""LinkedObjectId"" AND COALESCE(addr.""GCRecord"", 0) = 0
    LEFT JOIN ""Cities"" city
        ON city.""ID"" = addr.""CityID"" AND COALESCE(city.""GCRecord"", 0) = 0
    LEFT JOIN ""ProjectContracts"" pc
        ON pc.""ID"" = COALESCE(a.""ProjectContractID"", p.""ProjectContractID"")
       AND COALESCE(pc.""GCRecord"", 0) = 0
    LEFT JOIN ""People"" sp
        ON sp.""ID"" = p.""SponsoringEmployeeID"" AND COALESCE(sp.""GCRecord"", 0) = 0
    LEFT JOIN ""ProjectContracts"" spc
        ON spc.""ID"" = sp.""ProjectContractID"" AND COALESCE(spc.""GCRecord"", 0) = 0
    LEFT JOIN LATERAL (
        SELECT apg.""StateID""
        FROM ""ApplicationProfileInstanceProgresses"" apg
        WHERE apg.""ApplicationProfileInstanceID"" = a.""ID""
          AND COALESCE(apg.""GCRecord"", 0) = 0
        ORDER BY apg.""Date"" DESC NULLS LAST, apg.""ID"" DESC
        LIMIT 1
    ) latest_ap ON TRUE
    LEFT JOIN ""ApplicationStates"" ast
        ON ast.""ID"" = latest_ap.""StateID"" AND COALESCE(ast.""GCRecord"", 0) = 0
    WHERE COALESCE(v.""GCRecord"", 0) = 0
      AND {RegistrationProfilePredicate}
),
ranked AS (
    SELECT
        rl.""ID"",
        rl.""PersonOid"",
        rl.""PersonName"",
        rl.""ProjectName"",
        rl.""ProjectNameRaw"",
        rl.""ProjectNameTm"",
        rl.""PersonRoleCode"",
        rl.""VisaNumber"",
        rl.""VisaExpirationDate"",
        rl.""ApplicationNumber"",
        rl.""ApplicationDate"",
        rl.""ApplicationTypeName"",
        rl.""ApplicationTypeLabel"",
        rl.""ProgressStateLabel"",
        rl.""ProgressStateCssClass"",
        rl.""ProgressStateCode"",
        rl.""DaysRemaining"",
        rl.""ExpiryBucketLabel"",
        rl.""ExpiryBucketCssClass"",
        rl.""IsArchived"",
        rl.""CityLabel"",
        ROW_NUMBER() OVER (
            PARTITION BY rl.""VisaId""
            ORDER BY rl.""ApplicationDate"" DESC NULLS LAST, rl.""ApplicationProfileInstanceOid"" DESC, rl.""ID"" DESC
        ) AS rn
    FROM roster_lines rl
)
SELECT
    ""ID"",
    ""PersonOid"",
    ""PersonName"",
    ""ProjectName"",
    ""ProjectNameRaw"",
    ""ProjectNameTm"",
    ""PersonRoleCode"",
    ""VisaNumber"",
    ""VisaExpirationDate"",
    ""ApplicationNumber"",
    ""ApplicationDate"",
    ""ApplicationTypeName"",
    ""ApplicationTypeLabel"",
    ""ProgressStateLabel"",
    ""ProgressStateCssClass"",
    ""ProgressStateCode"",
    ""DaysRemaining"",
    ""ExpiryBucketLabel"",
    ""ExpiryBucketCssClass"",
    ""IsArchived"",
    ""CityLabel""
FROM ranked
WHERE rn = 1;
";

    internal const string MinistryRosterCtePlaceholder = "{{MINISTRY_ROSTER_CTE}}";

    /// <summary>
    /// One row per skip-navigation People pair (instance + person).
    /// </summary>
    internal static string CteMinistryRosterLines(string cteName = "ministry_roster_lines") => $@"
{cteName} AS (
    SELECT
        md5(concat(ap.""ApplicationProfileInstanceId""::text, ap.""PersonId""::text))::uuid AS ""LineId"",
        ap.""ApplicationProfileInstanceId"" AS ""ApplicationProfileInstanceID"",
        ap.""PersonId"" AS ""PersonID"",
        rl_pos.""LinkedObjectId"" AS ""PositionHistoryID"",
        rl_visa.""LinkedObjectId"" AS ""ExpiringVisaID"",
        rl_pass.""LinkedObjectId"" AS ""PassportID""
    FROM ""ApplicationProfileInstancePeople"" ap
    LEFT JOIN ""ApplicationProfileInstancePersonResolvedLinks"" rl_pos
        ON rl_pos.""ApplicationProfileInstanceId"" = ap.""ApplicationProfileInstanceId"" AND rl_pos.""PersonId"" = ap.""PersonId""
       AND rl_pos.""LinkKind"" = {LinkKindPosition}
       AND COALESCE(rl_pos.""GCRecord"", 0) = 0
    LEFT JOIN ""ApplicationProfileInstancePersonResolvedLinks"" rl_visa
        ON rl_visa.""ApplicationProfileInstanceId"" = ap.""ApplicationProfileInstanceId"" AND rl_visa.""PersonId"" = ap.""PersonId""
       AND rl_visa.""LinkKind"" = {LinkKindVisa}
       AND COALESCE(rl_visa.""GCRecord"", 0) = 0
    LEFT JOIN ""ApplicationProfileInstancePersonResolvedLinks"" rl_pass
        ON rl_pass.""ApplicationProfileInstanceId"" = ap.""ApplicationProfileInstanceId"" AND rl_pass.""PersonId"" = ap.""PersonId""
       AND rl_pass.""LinkKind"" = {LinkKindPassport}
       AND COALESCE(rl_pass.""GCRecord"", 0) = 0
)";
}
