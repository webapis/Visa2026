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
    internal const int LinkKindAddressOfResidence = 3;
    internal const int LinkKindTravelHistory = 11;

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
}
