using Microsoft.Data.SqlClient;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Idempotent SQL for <see cref="BusinessObjects.ApplicationProfileInstanceProgress.Order"/> (<c>ProgressOrder</c> column).
/// </summary>
public static class ApplicationProfileInstanceProgressOrderSchemaSql
{
    internal const string EnsureProgressOrderColumnSql = """
        IF OBJECT_ID(N'dbo.ApplicationProfileInstanceProgresses', N'U') IS NULL
            RETURN;
        IF COL_LENGTH(N'dbo.ApplicationProfileInstanceProgresses', N'ProgressOrder') IS NOT NULL
            RETURN;
        ALTER TABLE dbo.ApplicationProfileInstanceProgresses ADD ProgressOrder int NOT NULL CONSTRAINT DF_ApplicationProfileInstanceProgresses_ProgressOrder DEFAULT 0;
        """;

    internal const string BackfillProgressOrderSql = """
        IF OBJECT_ID(N'dbo.ApplicationProfileInstanceProgresses', N'U') IS NULL
            RETURN;
        IF COL_LENGTH(N'dbo.ApplicationProfileInstanceProgresses', N'ProgressOrder') IS NULL
            RETURN;
        ;WITH ordered AS (
            SELECT
                ap.ID,
                ROW_NUMBER() OVER (
                    PARTITION BY ap.ApplicationProfileInstanceID
                    ORDER BY
                        CASE
                            WHEN st.Code = N'IS_BEING_PREPARED' THEN 0
                            WHEN st.Code LIKE N'[1-5]_REVIEW_STARTED' THEN 9 + CAST(LEFT(st.Code, 1) AS int) * 2
                            WHEN st.Code LIKE N'[1-5]_REVIEW_APPROVED' THEN 10 + CAST(LEFT(st.Code, 1) AS int) * 2
                            WHEN st.Code LIKE N'[1-5]_REVIEW_REJECTED' THEN 11 + CAST(LEFT(st.Code, 1) AS int) * 2
                            WHEN st.Code = N'PROCESS_STARTED' THEN 999
                            WHEN st.Code = N'PROCESS_ISSUED' THEN 1000
                            WHEN st.Code = N'PROCESS_CANCELLED' THEN 1001
                            WHEN st.Code = N'PROCESS_REJECTED' THEN 1002
                            ELSE 500
                        END ASC,
                        ap.Date ASC,
                        ap.ID ASC) AS StepOrder
            FROM dbo.ApplicationProfileInstanceProgresses ap
            LEFT JOIN dbo.ApplicationStates st ON st.ID = ap.StateID
            WHERE ap.ApplicationProfileInstanceID IS NOT NULL
        )
        UPDATE ap
        SET ProgressOrder = o.StepOrder
        FROM dbo.ApplicationProfileInstanceProgresses ap
        INNER JOIN ordered o ON ap.ID = o.ID
        WHERE ap.ProgressOrder = 0;
        """;

    internal const string RecomputeAllProgressOrderSql = """
        IF OBJECT_ID(N'dbo.ApplicationProfileInstanceProgresses', N'U') IS NULL
            RETURN;
        IF COL_LENGTH(N'dbo.ApplicationProfileInstanceProgresses', N'ProgressOrder') IS NULL
            RETURN;
        ;WITH ordered AS (
            SELECT
                ap.ID,
                ROW_NUMBER() OVER (
                    PARTITION BY ap.ApplicationProfileInstanceID
                    ORDER BY
                        CASE
                            WHEN st.Code = N'IS_BEING_PREPARED' THEN 0
                            WHEN st.Code LIKE N'[1-5]_REVIEW_STARTED' THEN 9 + CAST(LEFT(st.Code, 1) AS int) * 2
                            WHEN st.Code LIKE N'[1-5]_REVIEW_APPROVED' THEN 10 + CAST(LEFT(st.Code, 1) AS int) * 2
                            WHEN st.Code LIKE N'[1-5]_REVIEW_REJECTED' THEN 11 + CAST(LEFT(st.Code, 1) AS int) * 2
                            WHEN st.Code = N'PROCESS_STARTED' THEN 999
                            WHEN st.Code = N'PROCESS_ISSUED' THEN 1000
                            WHEN st.Code = N'PROCESS_CANCELLED' THEN 1001
                            WHEN st.Code = N'PROCESS_REJECTED' THEN 1002
                            ELSE 500
                        END ASC,
                        ap.Date ASC,
                        ap.ID ASC) AS StepOrder
            FROM dbo.ApplicationProfileInstanceProgresses ap
            LEFT JOIN dbo.ApplicationStates st ON st.ID = ap.StateID
            WHERE ap.ApplicationProfileInstanceID IS NOT NULL
        )
        UPDATE ap
        SET ProgressOrder = o.StepOrder
        FROM dbo.ApplicationProfileInstanceProgresses ap
        INNER JOIN ordered o ON ap.ID = o.ID;
        """;

    public static void ApplyIfMissing(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return;

        using var connection = new SqlConnection(connectionString);
        connection.Open();
        ExecuteBatch(connection, EnsureProgressOrderColumnSql);
        ExecuteBatch(connection, BackfillProgressOrderSql);
    }

    private static void ExecuteBatch(SqlConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}