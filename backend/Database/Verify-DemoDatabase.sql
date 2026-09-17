USE [HospitalSms];
GO

SET NOCOUNT ON;

DECLARE @expected_objects TABLE (
    [object_type] varchar(20) NOT NULL,
    [object_name] sysname NOT NULL
);

INSERT INTO @expected_objects ([object_type], [object_name])
VALUES
    ('TABLE', 'bthol_fact_sent_messages'),
    ('TABLE', 'bthol_SMS_2_send_list_delta'),
    ('TABLE', 'bthol_SMS_2_send_list_delta_deleted'),
    ('TABLE', 'ddl_log'),
    ('TABLE', 'Sms_AuditLog'),
    ('TABLE', 'sms_category'),
    ('TABLE', 'sms_category_step'),
    ('TABLE', 'sms_hospital'),
    ('TABLE', 'sms_placeholder_catalog'),
    ('TABLE', 'sms_placeholder_scope'),
    ('TABLE', 'sms_project'),
    ('TABLE', 'sms_reject_hospital_unit'),
    ('TABLE', 'sms_static'),
    ('TABLE', 'sms_template'),
    ('TABLE', 'sms_template_language'),
    ('TABLE', 'sms_template_trigger'),
    ('TABLE', 'sms_trigger_catalog'),
    ('TABLE', 'sms_rules'),
    ('TABLE', 'sms_unit'),
    ('TABLE', 'sms_unit_category'),
    ('TABLE', 'sms_test_phone_whitelist'),
    ('VIEW', 'sms_unit_category_v'),
    ('PROCEDURE', 'sp_sms_dev_send_delta_queue_add');

IF EXISTS (
    SELECT 1
    FROM @expected_objects AS expected
    WHERE
        (expected.[object_type] = 'TABLE'
         AND OBJECT_ID(N'dbo.' + expected.[object_name], N'U') IS NULL)
        OR
        (expected.[object_type] = 'VIEW'
         AND OBJECT_ID(N'dbo.' + expected.[object_name], N'V') IS NULL)
        OR
        (expected.[object_type] = 'PROCEDURE'
         AND OBJECT_ID(N'dbo.' + expected.[object_name], N'P') IS NULL)
)
BEGIN
    SELECT expected.*
    FROM @expected_objects AS expected
    WHERE
        (expected.[object_type] = 'TABLE'
         AND OBJECT_ID(N'dbo.' + expected.[object_name], N'U') IS NULL)
        OR
        (expected.[object_type] = 'VIEW'
         AND OBJECT_ID(N'dbo.' + expected.[object_name], N'V') IS NULL)
        OR
        (expected.[object_type] = 'PROCEDURE'
         AND OBJECT_ID(N'dbo.' + expected.[object_name], N'P') IS NULL);

    THROW 51020, 'The demo database is missing one or more required objects.', 1;
END;

IF (SELECT COUNT(*) FROM [dbo].[sms_project]) < 3
    OR (SELECT COUNT(*) FROM [dbo].[sms_hospital]) < 4
    OR (SELECT COUNT(*) FROM [dbo].[sms_template]) < 6
    OR (SELECT COUNT(*) FROM [dbo].[sms_template_language]) < 12
    OR (SELECT COUNT(*) FROM [dbo].[sms_test_phone_whitelist]) < 3
BEGIN
    THROW 51021, 'The demo seed data is incomplete.', 1;
END;

IF EXISTS (
    SELECT 1
    FROM [dbo].[bthol_SMS_2_send_list_delta]
    WHERE [id] <> 'PUBLIC_DEMO'
       OR [password] <> 'NOT_A_REAL_SECRET'
)
BEGIN
    THROW 51022, 'Unexpected non-demo queue credentials were found.', 1;
END;

SELECT
    DB_NAME() AS [database_name],
    CAST(DATABASEPROPERTYEX(DB_NAME(), 'Collation') AS sysname) AS [collation],
    (SELECT COUNT(*) FROM @expected_objects WHERE [object_type] = 'TABLE') AS [expected_table_count],
    (SELECT COUNT(*) FROM [dbo].[sms_project]) AS [project_count],
    (SELECT COUNT(*) FROM [dbo].[sms_hospital]) AS [hospital_count],
    (SELECT COUNT(*) FROM [dbo].[sms_category]) AS [category_count],
    (SELECT COUNT(*) FROM [dbo].[sms_template]) AS [template_count],
    (SELECT COUNT(*) FROM [dbo].[sms_template_language]) AS [template_language_count],
    (SELECT COUNT(*) FROM [dbo].[sms_unit]) AS [unit_count],
    (SELECT COUNT(*) FROM [dbo].[sms_test_phone_whitelist]) AS [test_phone_count];
GO
