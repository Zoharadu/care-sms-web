SET XACT_ABORT ON;
BEGIN TRANSACTION;
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER VIEW [dbo].[sms_unit_category_v]
AS
SELECT
    uc.[uc_id],
    c.[project_id],
    p.[project_name],
    uc.[category_id],
    c.[category_name],
    u.[hospital_id],
    h.[hospital_name],
    uc.[sms_unit_id],
    u.[unit_id],
    u.[unit_name],
    uc.[is_active]
FROM [dbo].[sms_unit_category] AS uc
LEFT JOIN [dbo].[sms_unit] AS u
    ON u.[sms_unit_id] = uc.[sms_unit_id]
LEFT JOIN [dbo].[sms_hospital] AS h
    ON h.[hospital_id] = u.[hospital_id]
LEFT JOIN [dbo].[sms_category] AS c
    ON c.[category_id] = uc.[category_id]
LEFT JOIN [dbo].[sms_project] AS p
    ON p.[project_id] = c.[project_id];
GO

COMMIT TRANSACTION;
GO
