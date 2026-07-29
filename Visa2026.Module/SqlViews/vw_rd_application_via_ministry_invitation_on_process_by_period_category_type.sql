-- Invitation on Process (V): same ApplicationItem population + Period/Category/Type.
CREATE OR ALTER VIEW [dbo].[vw_rd_application_via_ministry_invitation_on_process_by_period_category_type] AS
SELECT
    b.ID, b.ApplicationOid, b.ApplicationItemOid, b.PersonOid, b.CurrentStateID,
    b.PersonName, b.ProjectName, b.ProjectNameRaw, b.ProjectNameTm, b.PersonRoleCode,
    b.PositionLabel, b.ApplicationTypeLabel, b.VisaPeriodLabel, b.VisaTypeLabel, b.ApplicationNumber, b.ApplicationDate,
    b.ProgressStateCode, b.StatusLabel, b.StatusCssClass, b.IsArchived,
    COALESCE(NULLIF(LTRIM(RTRIM(vp.NameTm)), N''), NULLIF(LTRIM(RTRIM(vp.Name)), N''), N'(No period)') AS PeriodLabel,
    COALESCE(NULLIF(LTRIM(RTRIM(vc.NameTm)), N''), NULLIF(LTRIM(RTRIM(vc.Name)), N''), N'(No category)') AS CategoryLabel,
    COALESCE(NULLIF(LTRIM(RTRIM(vt.NameTm)), N''), NULLIF(LTRIM(RTRIM(vt.Name)), N''), N'(No type)') AS TypeLabel
FROM [dbo].[vw_rd_application_via_ministry_invitation_on_process] b
LEFT JOIN Applications a ON a.ID = b.ApplicationOid AND ISNULL(a.GCRecord, 0) = 0
LEFT JOIN VisaPeriods vp ON vp.ID = a.VisaPeriodID AND ISNULL(vp.GCRecord, 0) = 0
LEFT JOIN VisaCategories vc ON vc.ID = a.VisaCategoryID AND ISNULL(vc.GCRecord, 0) = 0
LEFT JOIN VisaTypes vt ON vt.ID = a.VisaTypeID AND ISNULL(vt.GCRecord, 0) = 0;
