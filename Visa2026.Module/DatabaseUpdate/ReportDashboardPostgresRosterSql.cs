namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Shared PostgreSQL fragments for Report Dashboard views: prefer <c>ApplicationPeople</c> +
/// <see cref="BusinessObjects.ApplicationPersonResolvedLink"/>; fall back to legacy
/// <c>ApplicationItems</c> when an application has no M2M roster rows.
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

    internal const string VisaExtensionApplicationTypeNames = @"
            'App_Visa_Ext',
            'App_Visa_Ext_According_to_WP',
            'App_Visa_Ext_FM',
            'App_Visa_and_WP_Ext'";

    internal const string WorkPermitExtensionApplicationTypeNames = @"
            'App_WP_Ext',
            'App_Visa_and_WP_Ext'";

    /// <summary>LATERAL joins: first person on an application (M2M roster, legacy ApplicationItem fallback).</summary>
    internal const string FirstApplicationPersonLateralJoin = @"
LEFT JOIN LATERAL (
    SELECT ap_row.""PersonId""
    FROM ""ApplicationPeople"" ap_row
    WHERE ap_row.""ApplicationId"" = a.""ID""
      AND COALESCE(ap_row.""GCRecord"", 0) = 0
    ORDER BY ap_row.""LinkedAt"", ap_row.""ID""
    LIMIT 1
) first_m2m ON TRUE
LEFT JOIN LATERAL (
    SELECT ai.""PersonID""
    FROM ""ApplicationItems"" ai
    WHERE ai.""ApplicationID"" = a.""ID""
      AND COALESCE(ai.""GCRecord"", 0) = 0
      AND NOT EXISTS (
            SELECT 1
            FROM ""ApplicationPeople"" ap_roster
            WHERE ap_roster.""ApplicationId"" = a.""ID""
              AND COALESCE(ap_roster.""GCRecord"", 0) = 0
      )
    ORDER BY ai.""ID""
    LIMIT 1
) first_legacy ON TRUE";

    internal static string CteVisaExtensionRosterLines(string cteName = "visa_ext_roster") => $@"
{cteName} AS (
    SELECT
        ap.""ID"" AS ""LineId"",
        a.""ID"" AS ""ApplicationID"",
        ap.""PersonId"" AS ""PersonID"",
        rl_visa.""LinkedObjectId"" AS ""ExpiringVisaID"",
        rl_pass.""LinkedObjectId"" AS ""PassportID""
    FROM ""ApplicationPeople"" ap
    INNER JOIN ""Applications"" a
        ON a.""ID"" = ap.""ApplicationId"" AND COALESCE(a.""GCRecord"", 0) = 0
    INNER JOIN ""ApplicationTypes"" at
        ON at.""ID"" = a.""ApplicationTypeID"" AND COALESCE(at.""GCRecord"", 0) = 0
    INNER JOIN ""ApplicationPersonResolvedLinks"" rl_visa
        ON rl_visa.""ApplicationPersonId"" = ap.""ID""
       AND rl_visa.""LinkKind"" = {LinkKindVisa}
       AND rl_visa.""LinkedObjectId"" IS NOT NULL
       AND COALESCE(rl_visa.""GCRecord"", 0) = 0
    LEFT JOIN ""ApplicationPersonResolvedLinks"" rl_pass
        ON rl_pass.""ApplicationPersonId"" = ap.""ID""
       AND rl_pass.""LinkKind"" = {LinkKindPassport}
       AND COALESCE(rl_pass.""GCRecord"", 0) = 0
    WHERE COALESCE(ap.""GCRecord"", 0) = 0
      AND at.""Name"" IN ({VisaExtensionApplicationTypeNames})

    UNION ALL

    SELECT
        ai.""ID"" AS ""LineId"",
        a.""ID"" AS ""ApplicationID"",
        ai.""PersonID"" AS ""PersonID"",
        ai.""CurrentVisaId"" AS ""ExpiringVisaID"",
        ai.""CurrentPassportID"" AS ""PassportID""
    FROM ""ApplicationItems"" ai
    INNER JOIN ""Applications"" a
        ON a.""ID"" = ai.""ApplicationID"" AND COALESCE(a.""GCRecord"", 0) = 0
    INNER JOIN ""ApplicationTypes"" at
        ON at.""ID"" = a.""ApplicationTypeID"" AND COALESCE(at.""GCRecord"", 0) = 0
    WHERE COALESCE(ai.""GCRecord"", 0) = 0
      AND ai.""CurrentVisaId"" IS NOT NULL
      AND at.""Name"" IN ({VisaExtensionApplicationTypeNames})
      {LegacyApplicationItemOnly}
)";

    internal static string CteWorkPermitExtensionRosterLines(string cteName = "wp_ext_roster") => $@"
{cteName} AS (
    SELECT
        ap.""ID"" AS ""LineId"",
        a.""ID"" AS ""ApplicationID"",
        ap.""PersonId"" AS ""PersonID"",
        rl_wp.""LinkedObjectId"" AS ""WorkPermitItemID""
    FROM ""ApplicationPeople"" ap
    INNER JOIN ""Applications"" a
        ON a.""ID"" = ap.""ApplicationId"" AND COALESCE(a.""GCRecord"", 0) = 0
    INNER JOIN ""ApplicationTypes"" at
        ON at.""ID"" = a.""ApplicationTypeID"" AND COALESCE(at.""GCRecord"", 0) = 0
    INNER JOIN ""ApplicationPersonResolvedLinks"" rl_wp
        ON rl_wp.""ApplicationPersonId"" = ap.""ID""
       AND rl_wp.""LinkKind"" = {LinkKindWorkPermitItem}
       AND rl_wp.""LinkedObjectId"" IS NOT NULL
       AND COALESCE(rl_wp.""GCRecord"", 0) = 0
    WHERE COALESCE(ap.""GCRecord"", 0) = 0
      AND at.""Name"" IN ({WorkPermitExtensionApplicationTypeNames})

    UNION ALL

    SELECT
        ai.""ID"" AS ""LineId"",
        a.""ID"" AS ""ApplicationID"",
        ai.""PersonID"" AS ""PersonID"",
        ai.""CurrentWorkPermitItemID"" AS ""WorkPermitItemID""
    FROM ""ApplicationItems"" ai
    INNER JOIN ""Applications"" a
        ON a.""ID"" = ai.""ApplicationID"" AND COALESCE(a.""GCRecord"", 0) = 0
    INNER JOIN ""ApplicationTypes"" at
        ON at.""ID"" = a.""ApplicationTypeID"" AND COALESCE(at.""GCRecord"", 0) = 0
    WHERE COALESCE(ai.""GCRecord"", 0) = 0
      AND ai.""CurrentWorkPermitItemID"" IS NOT NULL
      AND at.""Name"" IN ({WorkPermitExtensionApplicationTypeNames})
      {LegacyApplicationItemOnly}
)";

    internal static string UnfinishedExtensionPeopleCte(string rosterCteName = "visa_ext_roster") => $@"
unfinished_extension_people AS (
    SELECT DISTINCT roster.""PersonID""
    FROM {rosterCteName} roster
    INNER JOIN ""Applications"" a
        ON a.""ID"" = roster.""ApplicationID"" AND COALESCE(a.""GCRecord"", 0) = 0
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
 WHERE iv.""IssuingApplicationItemID"" = {rosterAlias}.""LineId""
    OR (
        iv.""IssuingApplicationID"" = {rosterAlias}.""ApplicationID""
        AND {rosterAlias}.""PassportID"" IS NOT NULL
        AND iv.""PassportID"" = {rosterAlias}.""PassportID""
    )
 LIMIT 1)";

    internal static string ViewVisaExtensionStatusSql => $@"
-- PostgreSQL counterpart of SqlViewsUpdater.CreateViewVisaExtensionStatus (SQL Server).
-- Note: ApplicationItems.""CurrentVisaId"" (mixed case) — not CurrentVisaID.
CREATE VIEW ""View_VisaExtensionStatus"" AS
WITH {CteVisaExtensionRosterLines()}
SELECT
    roster.""LineId"" AS ""ID"",
    roster.""ApplicationID"",
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
     WHERE r.""ApplicationID"" = a.""ID"" AND ri.""PersonID"" = roster.""PersonID""
     LIMIT 1) AS ""RejectionItemID""
FROM visa_ext_roster roster
JOIN ""Applications"" a ON roster.""ApplicationID"" = a.""ID""
LEFT JOIN ""Visas"" v ON roster.""ExpiringVisaID"" = v.""ID""
LEFT JOIN LATERAL (
    SELECT ap.""StateID"", ap.""Date"", ap.""Description""
    FROM ""ApplicationProgresses"" ap
    WHERE ap.""ApplicationID"" = a.""ID""
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
    a.""ID""                                                                          AS ""ApplicationOid"",
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
INNER JOIN ""Applications"" a
    ON a.""ID"" = roster.""ApplicationID"" AND COALESCE(a.""GCRecord"", 0) = 0
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
    FROM ""ApplicationProgresses"" ap
    WHERE ap.""ApplicationID"" = a.""ID""
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
    a.""ID""                                                                          AS ""ApplicationOid"",
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
INNER JOIN ""Applications"" a
    ON a.""ID"" = roster.""ApplicationID"" AND COALESCE(a.""GCRecord"", 0) = 0
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
    FROM ""ApplicationProgresses"" ap
    WHERE ap.""ApplicationID"" = a.""ID""
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
INNER JOIN ""Applications"" a
    ON a.""ID"" = roster.""ApplicationID"" AND COALESCE(a.""GCRecord"", 0) = 0
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
        FROM ""ApplicationProgresses"" ap
        INNER JOIN ""ApplicationStates"" ast
            ON ast.""ID"" = ap.""StateID""
           AND COALESCE(ast.""GCRecord"", 0) = 0
        WHERE ap.""ApplicationID"" = roster.""ApplicationID""
          AND COALESCE(ap.""GCRecord"", 0) = 0
          AND ast.""Code"" = 'PROCESS_CANCELLED'
      );
";

    internal const string RegistrationApplicationTypeNames = @"
            'App_Reg_Check_In',
            'App_Reg_Check_In_Internal',
            'App_Reg_Check_Out',
            'App_Reg_Check_Out_Internal',
            'App_Reg_ext',
            'App_Reg_Info_Change_Address',
            'App_Reg_Info_Change_Passport',
            'App_Reg_Info_Change_Visa'";

    internal const string CheckoutApplicationTypeNames = @"
            'App_Reg_Check_Out',
            'App_Reg_Check_Out_Internal'";

    /// <summary>Legacy ApplicationItems rows only when the parent application has no M2M roster.</summary>
    internal const string LegacyApplicationItemOnly = @"
      AND NOT EXISTS (
            SELECT 1
            FROM ""ApplicationPeople"" ap_roster
            WHERE ap_roster.""ApplicationId"" = ai.""ApplicationID""
              AND COALESCE(ap_roster.""GCRecord"", 0) = 0
      )";

    internal static string CteRegLinkedVisaIds() => $@"
reg_linked AS (
    SELECT DISTINCT rl.""LinkedObjectId"" AS ""VisaId""
    FROM ""ApplicationPersonResolvedLinks"" rl
    INNER JOIN ""ApplicationPeople"" ap
        ON ap.""ID"" = rl.""ApplicationPersonId"" AND COALESCE(ap.""GCRecord"", 0) = 0
    INNER JOIN ""Applications"" a
        ON a.""ID"" = ap.""ApplicationId"" AND COALESCE(a.""GCRecord"", 0) = 0
    INNER JOIN ""ApplicationTypes"" at
        ON at.""ID"" = a.""ApplicationTypeID"" AND COALESCE(at.""GCRecord"", 0) = 0
    WHERE COALESCE(rl.""GCRecord"", 0) = 0
      AND rl.""LinkKind"" = {LinkKindVisa}
      AND rl.""LinkedObjectId"" IS NOT NULL
      AND at.""Name"" IN ({RegistrationApplicationTypeNames})
    UNION
    SELECT DISTINCT ai.""CurrentVisaId"" AS ""VisaId""
    FROM ""ApplicationItems"" ai
    INNER JOIN ""Applications"" a
        ON a.""ID"" = ai.""ApplicationID"" AND COALESCE(a.""GCRecord"", 0) = 0
    INNER JOIN ""ApplicationTypes"" at
        ON at.""ID"" = a.""ApplicationTypeID"" AND COALESCE(at.""GCRecord"", 0) = 0
    WHERE COALESCE(ai.""GCRecord"", 0) = 0
      AND ai.""CurrentVisaId"" IS NOT NULL
      AND at.""Name"" IN ({RegistrationApplicationTypeNames})
      {LegacyApplicationItemOnly}
)";

    internal static string CteCheckoutLinkedVisaIds() => $@"
checkout_linked AS (
    SELECT DISTINCT rl.""LinkedObjectId"" AS ""VisaId""
    FROM ""ApplicationPersonResolvedLinks"" rl
    INNER JOIN ""ApplicationPeople"" ap
        ON ap.""ID"" = rl.""ApplicationPersonId"" AND COALESCE(ap.""GCRecord"", 0) = 0
    INNER JOIN ""Applications"" a
        ON a.""ID"" = ap.""ApplicationId"" AND COALESCE(a.""GCRecord"", 0) = 0
    INNER JOIN ""ApplicationTypes"" at
        ON at.""ID"" = a.""ApplicationTypeID"" AND COALESCE(at.""GCRecord"", 0) = 0
    WHERE COALESCE(rl.""GCRecord"", 0) = 0
      AND rl.""LinkKind"" = {LinkKindVisa}
      AND rl.""LinkedObjectId"" IS NOT NULL
      AND at.""Name"" IN ({CheckoutApplicationTypeNames})
    UNION
    SELECT DISTINCT ai.""CurrentVisaId"" AS ""VisaId""
    FROM ""ApplicationItems"" ai
    INNER JOIN ""Applications"" a
        ON a.""ID"" = ai.""ApplicationID"" AND COALESCE(a.""GCRecord"", 0) = 0
    INNER JOIN ""ApplicationTypes"" at
        ON at.""ID"" = a.""ApplicationTypeID"" AND COALESCE(at.""GCRecord"", 0) = 0
    WHERE COALESCE(ai.""GCRecord"", 0) = 0
      AND ai.""CurrentVisaId"" IS NOT NULL
      AND at.""Name"" IN ({CheckoutApplicationTypeNames})
      {LegacyApplicationItemOnly}
)";

    internal static string RegistrationViewSql => $@"
-- Report Dashboard: Registration category (PostgreSQL).
-- One row per not-expired visa: latest registration Application via roster visa link (M2M + legacy fallback).
CREATE VIEW vw_rd_registration AS
WITH roster_lines AS (
    SELECT
        ap.""ID"" AS ""ID"",
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
        at.""Name"" AS ""ApplicationTypeName"",
        COALESCE(
            NULLIF(BTRIM(at.""NameTm""), ''),
            NULLIF(BTRIM(at.""Name""), ''),
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
        a.""ID"" AS ""ApplicationOid""
    FROM ""Visas"" v
    INNER JOIN ""Passports"" pp
        ON pp.""ID"" = v.""PassportID"" AND COALESCE(pp.""GCRecord"", 0) = 0
    INNER JOIN ""People"" p
        ON p.""ID"" = pp.""PersonID"" AND COALESCE(p.""GCRecord"", 0) = 0
    INNER JOIN ""ApplicationPersonResolvedLinks"" rl_visa
        ON rl_visa.""LinkKind"" = {LinkKindVisa}
       AND rl_visa.""LinkedObjectId"" = v.""ID""
       AND COALESCE(rl_visa.""GCRecord"", 0) = 0
    INNER JOIN ""ApplicationPeople"" ap
        ON ap.""ID"" = rl_visa.""ApplicationPersonId""
       AND ap.""PersonId"" = p.""ID""
       AND COALESCE(ap.""GCRecord"", 0) = 0
    INNER JOIN ""Applications"" a
        ON a.""ID"" = ap.""ApplicationId"" AND COALESCE(a.""GCRecord"", 0) = 0
    INNER JOIN ""ApplicationTypes"" at
        ON at.""ID"" = a.""ApplicationTypeID"" AND COALESCE(at.""GCRecord"", 0) = 0
    LEFT JOIN ""ApplicationPersonResolvedLinks"" rl_addr
        ON rl_addr.""ApplicationPersonId"" = ap.""ID""
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
        FROM ""ApplicationProgresses"" apg
        WHERE apg.""ApplicationID"" = a.""ID""
          AND COALESCE(apg.""GCRecord"", 0) = 0
        ORDER BY apg.""Date"" DESC NULLS LAST, apg.""ID"" DESC
        LIMIT 1
    ) latest_ap ON TRUE
    LEFT JOIN ""ApplicationStates"" ast
        ON ast.""ID"" = latest_ap.""StateID"" AND COALESCE(ast.""GCRecord"", 0) = 0
    WHERE COALESCE(v.""GCRecord"", 0) = 0
      AND at.""Name"" IN ({RegistrationApplicationTypeNames})

    UNION ALL

    SELECT
        ai.""ID"" AS ""ID"",
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
        at.""Name"" AS ""ApplicationTypeName"",
        COALESCE(
            NULLIF(BTRIM(at.""NameTm""), ''),
            NULLIF(BTRIM(at.""Name""), ''),
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
        a.""ID"" AS ""ApplicationOid""
    FROM ""Visas"" v
    INNER JOIN ""Passports"" pp
        ON pp.""ID"" = v.""PassportID"" AND COALESCE(pp.""GCRecord"", 0) = 0
    INNER JOIN ""People"" p
        ON p.""ID"" = pp.""PersonID"" AND COALESCE(p.""GCRecord"", 0) = 0
    INNER JOIN ""ApplicationItems"" ai
        ON ai.""CurrentVisaId"" = v.""ID"" AND COALESCE(ai.""GCRecord"", 0) = 0
    INNER JOIN ""Applications"" a
        ON a.""ID"" = ai.""ApplicationID"" AND COALESCE(a.""GCRecord"", 0) = 0
    INNER JOIN ""ApplicationTypes"" at
        ON at.""ID"" = a.""ApplicationTypeID"" AND COALESCE(at.""GCRecord"", 0) = 0
    LEFT JOIN ""AddressesOfResidence"" addr
        ON addr.""ID"" = ai.""CurrentAddressOfResidenceID"" AND COALESCE(addr.""GCRecord"", 0) = 0
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
        FROM ""ApplicationProgresses"" apg
        WHERE apg.""ApplicationID"" = a.""ID""
          AND COALESCE(apg.""GCRecord"", 0) = 0
        ORDER BY apg.""Date"" DESC NULLS LAST, apg.""ID"" DESC
        LIMIT 1
    ) latest_ap ON TRUE
    LEFT JOIN ""ApplicationStates"" ast
        ON ast.""ID"" = latest_ap.""StateID"" AND COALESCE(ast.""GCRecord"", 0) = 0
    WHERE COALESCE(v.""GCRecord"", 0) = 0
      AND at.""Name"" IN ({RegistrationApplicationTypeNames})
      {LegacyApplicationItemOnly}
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
            ORDER BY rl.""ApplicationDate"" DESC NULLS LAST, rl.""ApplicationOid"" DESC, rl.""ID"" DESC
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
    /// One row per application roster line (M2M ApplicationPerson or legacy ApplicationItem).
    /// </summary>
    internal static string CteMinistryRosterLines(string cteName = "ministry_roster_lines") => $@"
{cteName} AS (
    SELECT
        ap.""ID"" AS ""LineId"",
        ap.""ApplicationId"" AS ""ApplicationID"",
        ap.""PersonId"" AS ""PersonID"",
        rl_pos.""LinkedObjectId"" AS ""PositionHistoryID"",
        rl_visa.""LinkedObjectId"" AS ""ExpiringVisaID"",
        rl_pass.""LinkedObjectId"" AS ""PassportID""
    FROM ""ApplicationPeople"" ap
    LEFT JOIN ""ApplicationPersonResolvedLinks"" rl_pos
        ON rl_pos.""ApplicationPersonId"" = ap.""ID""
       AND rl_pos.""LinkKind"" = {LinkKindPosition}
       AND COALESCE(rl_pos.""GCRecord"", 0) = 0
    LEFT JOIN ""ApplicationPersonResolvedLinks"" rl_visa
        ON rl_visa.""ApplicationPersonId"" = ap.""ID""
       AND rl_visa.""LinkKind"" = {LinkKindVisa}
       AND COALESCE(rl_visa.""GCRecord"", 0) = 0
    LEFT JOIN ""ApplicationPersonResolvedLinks"" rl_pass
        ON rl_pass.""ApplicationPersonId"" = ap.""ID""
       AND rl_pass.""LinkKind"" = {LinkKindPassport}
       AND COALESCE(rl_pass.""GCRecord"", 0) = 0
    WHERE COALESCE(ap.""GCRecord"", 0) = 0

    UNION ALL

    SELECT
        ai.""ID"" AS ""LineId"",
        ai.""ApplicationID"" AS ""ApplicationID"",
        ai.""PersonID"" AS ""PersonID"",
        ai.""CurrentPositionHistoryID"" AS ""PositionHistoryID"",
        ai.""CurrentVisaId"" AS ""ExpiringVisaID"",
        ai.""CurrentPassportID"" AS ""PassportID""
    FROM ""ApplicationItems"" ai
    WHERE COALESCE(ai.""GCRecord"", 0) = 0
      {LegacyApplicationItemOnly}
)";
}
