/*
  Public demo database for HospitalSms.

  The schema mirrors HospitalSmsDbContext. All organizations, people, phone
  numbers and operational values below are fictional demo data. References to
  Maccabi, Assuta, Meuhedet and Leumit are illustrative only and do not imply
  affiliation, endorsement or production use.

  Safety: this script refuses to overwrite an existing HospitalSms database.
*/

USE [master];
GO

IF DB_ID(N'HospitalSms') IS NOT NULL
BEGIN
    THROW 51000, 'Database HospitalSms already exists. No changes were made.', 1;
END;
GO

CREATE DATABASE [HospitalSms] COLLATE Hebrew_100_CI_AS;
GO

ALTER DATABASE [HospitalSms] SET RECOVERY SIMPLE;
GO

USE [HospitalSms];
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;

CREATE TABLE [dbo].[bthol_fact_sent_messages] (
    [row_id] bigint NOT NULL IDENTITY(1, 1),
    [kod_msg_project] int NOT NULL,
    [kod_sug_msg] int NOT NULL,
    [kod_mosad] int NOT NULL,
    [kod_language] tinyint NOT NULL,
    [msg_text] nvarchar(1000) NOT NULL,
    [mispar_kabala] bigint NOT NULL,
    [teudat_zehut_src] bigint NOT NULL,
    [phone_num_male] bigint NOT NULL,
    [msg_taarich_shaa] datetime NULL,
    [sapak_msg_id] bigint NOT NULL,
    [msg_status] tinyint NOT NULL,
    [taarich_lechishuv] datetime NULL,
    [kod_machlaka] int NULL,
    [kod_makor_phone_no] int NULL,
    CONSTRAINT [PK_bthol_fact_sent_messages] PRIMARY KEY ([row_id])
);

CREATE TABLE [dbo].[bthol_SMS_2_send_list_delta] (
    [kod_msg_project] int NOT NULL,
    [kod_sug_msg] int NOT NULL,
    [kod_sug_msg_rn] tinyint NOT NULL,
    [kod_mosad] int NOT NULL,
    [kod_language] tinyint NOT NULL,
    [mispar_kabala] varchar(20) NOT NULL,
    [phone_num_male] varchar(20) NOT NULL,
    [kod_machlaka] int NOT NULL,
    [msg_txt] nvarchar(1000) NOT NULL,
    [taarich_lechishuv] datetime NOT NULL,
    [teudat_zehut_src] varchar(20) NOT NULL,
    [id] varchar(24) NOT NULL,
    [password] varchar(33) NOT NULL,
    [sw_test] int NOT NULL,
    [sw_started] bit NOT NULL,
    [date_created] datetime NOT NULL,
    [date_started] datetime NULL,
    [kod_makor_phone_no] int NULL,
    CONSTRAINT [PK_bthol_SMS_2_send_list_delta] PRIMARY KEY (
        [kod_msg_project],
        [kod_sug_msg],
        [kod_sug_msg_rn],
        [kod_mosad],
        [kod_language],
        [mispar_kabala],
        [phone_num_male],
        [kod_machlaka]
    )
);

CREATE TABLE [dbo].[bthol_SMS_2_send_list_delta_deleted] (
    [kod_msg_project] int NOT NULL,
    [kod_sug_msg] int NOT NULL,
    [kod_mosad] int NOT NULL,
    [kod_language] tinyint NOT NULL,
    [mispar_kabala] varchar(20) NOT NULL,
    [phone_num_male] varchar(20) NOT NULL,
    [kod_machlaka] int NOT NULL,
    [date_created] datetime NOT NULL,
    [kod_sug_msg_rn] tinyint NULL,
    [msg_txt] nvarchar(1000) NOT NULL,
    [taarich_lechishuv] datetime NOT NULL,
    [teudat_zehut_src] varchar(20) NOT NULL,
    [id] varchar(24) NOT NULL,
    [password] varchar(33) NOT NULL,
    [sw_test] int NOT NULL,
    [sw_started] bit NOT NULL,
    [date_started] datetime NULL,
    [date_deleted] datetime NULL,
    [sw_sent] bit NULL,
    [row_id] bigint NULL,
    [kod_makor_phone_no] int NULL,
    CONSTRAINT [PK_bthol_SMS_2_send_list_delta_deleted] PRIMARY KEY (
        [kod_msg_project],
        [kod_sug_msg],
        [kod_mosad],
        [kod_language],
        [mispar_kabala],
        [kod_machlaka],
        [phone_num_male],
        [date_created]
    )
);

CREATE TABLE [dbo].[ddl_log] (
    [PostTime] datetime NULL,
    [sys_user] varchar(100) NULL,
    [DB_User] varchar(100) NULL,
    [OriginalUserName] varchar(100) NULL,
    [Host] varchar(100) NULL,
    [ApplicationName] varchar(400) NULL,
    [Event] varchar(100) NULL,
    [ObjectName] nchar(100) NULL,
    [TSQL] varchar(2000) NULL,
    [EventData] xml NULL
);

CREATE TABLE [dbo].[Sms_AuditLog] (
    [AuditLogId] bigint NOT NULL IDENTITY(1, 1),
    [EntityName] nvarchar(100) NOT NULL,
    [EntityId] int NOT NULL,
    [ActionType] nvarchar(50) NOT NULL,
    [UserId] int NULL,
    [OldValueJson] nvarchar(max) NULL,
    [NewValueJson] nvarchar(max) NULL,
    [DiffJson] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL CONSTRAINT [DF_Sms_AuditLog_CreatedAt] DEFAULT (sysutcdatetime()),
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_Sms_AuditLog] PRIMARY KEY ([AuditLogId])
);

CREATE TABLE [dbo].[sms_category] (
    [category_id] int NOT NULL IDENTITY(1, 1),
    [category_name] varchar(50) NOT NULL,
    [project_id] int NOT NULL,
    [is_active] bit NOT NULL CONSTRAINT [DF_sms_category_is_active] DEFAULT (1),
    [is_editable] bit NOT NULL CONSTRAINT [DF_sms_category_is_editable] DEFAULT (1),
    [create_date] datetime NOT NULL CONSTRAINT [DF_sms_category_create_date] DEFAULT (GETDATE()),
    [update_date] datetime NULL,
    CONSTRAINT [PK_sms_category] PRIMARY KEY ([category_id])
);

CREATE TABLE [dbo].[sms_category_step] (
    [category_step_id] int NOT NULL IDENTITY(1, 1),
    [category_id] int NOT NULL,
    [template_id] int NOT NULL,
    [dependentat_id] int NULL,
    [delay_in_minutes] tinyint NULL,
    [is_active] bit NOT NULL CONSTRAINT [DF_sms_category_step_is_active] DEFAULT (1),
    [is_editable] bit NOT NULL CONSTRAINT [DF_sms_category_step_is_editable] DEFAULT (1),
    [create_date] datetime NOT NULL CONSTRAINT [DF_sms_category_step_create_date] DEFAULT (GETDATE()),
    [update_date] datetime NULL,
    CONSTRAINT [PK_sms_category_step] PRIMARY KEY ([category_step_id])
);

CREATE TABLE [dbo].[sms_hospital] (
    [hospital_id] int NOT NULL IDENTITY(1, 1),
    [hospital_type_id] int NOT NULL,
    [hospital_description] varchar(15) NOT NULL,
    [hospital_name] varchar(30) NOT NULL,
    [is_active] bit NOT NULL CONSTRAINT [DF_sms_hospital_is_active] DEFAULT (1),
    [create_date] datetime NOT NULL CONSTRAINT [DF_sms_hospital_create_date] DEFAULT (GETDATE()),
    [update_date] datetime NULL,
    CONSTRAINT [PK_sms_hospital] PRIMARY KEY ([hospital_id])
);

CREATE TABLE [dbo].[sms_placeholder_catalog] (
    [placeholder_id] int NOT NULL IDENTITY(1, 1),
    [placeholder_name] varchar(100) NOT NULL,
    [display_name] varchar(100) NOT NULL,
    [source_type] varchar(50) NULL,
    [value_source] varchar(200) NULL,
    [is_required] bit NOT NULL CONSTRAINT [DF_sms_placeholder_catalog_is_required] DEFAULT (0),
    [is_static] bit NOT NULL CONSTRAINT [DF_sms_placeholder_catalog_is_static] DEFAULT (0),
    [is_editable] bit NOT NULL CONSTRAINT [DF_sms_placeholder_catalog_is_editable] DEFAULT (1),
    [default_value] varchar(500) NULL,
    [create_date] datetime NOT NULL CONSTRAINT [DF_sms_placeholder_catalog_create_date] DEFAULT (GETDATE()),
    [update_date] datetime NULL,
    CONSTRAINT [PK_sms_placeholder_catalog] PRIMARY KEY ([placeholder_id])
);

CREATE TABLE [dbo].[sms_placeholder_scope] (
    [placeholder_scope_id] int NOT NULL IDENTITY(1, 1),
    [placeholder_id] int NOT NULL,
    [project_id] int NOT NULL,
    [category_id] int NULL,
    [is_active] bit NOT NULL CONSTRAINT [DF_sms_placeholder_scope_is_active] DEFAULT (1),
    [create_date] datetime NOT NULL CONSTRAINT [DF_sms_placeholder_scope_create_date] DEFAULT (GETDATE()),
    [update_date] datetime NULL,
    CONSTRAINT [PK_sms_placeholder_scope] PRIMARY KEY ([placeholder_scope_id])
);

CREATE TABLE [dbo].[sms_project] (
    [project_id] int NOT NULL IDENTITY(1, 1),
    [project_name] varchar(50) NOT NULL,
    [is_active] bit NOT NULL CONSTRAINT [DF_sms_project_is_active] DEFAULT (1),
    [create_date] datetime NOT NULL CONSTRAINT [DF_sms_project_create_date] DEFAULT (GETDATE()),
    [update_date] datetime NULL,
    CONSTRAINT [PK_sms_project] PRIMARY KEY ([project_id])
);

CREATE TABLE [dbo].[sms_reject_hospital_unit] (
    [template_id] int NOT NULL,
    [hospital_id] int NOT NULL,
    [unit_id] int NOT NULL,
    [create_date] datetime NOT NULL CONSTRAINT [DF_sms_reject_hospital_unit_create_date] DEFAULT (GETDATE()),
    [update_date] datetime NULL,
    CONSTRAINT [PK_sms_reject_hospital_unit] PRIMARY KEY ([template_id], [hospital_id], [unit_id])
);

CREATE TABLE [dbo].[sms_static] (
    [static_id] int NOT NULL IDENTITY(1, 1),
    [placeholder_id] int NOT NULL,
    [category_id] int NULL,
    [hospital_id] int NULL,
    [unit_id] int NULL,
    [source_code] int NULL,
    [value_name] nvarchar(500) NULL,
    [static_value] nvarchar(2000) NOT NULL,
    [is_active] bit NOT NULL CONSTRAINT [DF_sms_static_is_active] DEFAULT (1),
    [create_date] datetime NOT NULL CONSTRAINT [DF_sms_static_create_date] DEFAULT (GETDATE()),
    [update_date] datetime NULL,
    CONSTRAINT [PK_sms_static] PRIMARY KEY ([static_id])
);

CREATE TABLE [dbo].[sms_template] (
    [template_id] int NOT NULL IDENTITY(1, 1),
    [template_description] varchar(300) NULL,
    [template_name] varchar(100) NOT NULL,
    [category_id] int NOT NULL,
    [is_active] bit NOT NULL CONSTRAINT [DF_sms_template_is_active] DEFAULT (1),
    [is_editable] bit NOT NULL CONSTRAINT [DF_sms_template_is_editable] DEFAULT (1),
    [version_number] int NULL CONSTRAINT [DF_sms_template_version_number] DEFAULT (1),
    [create_date] datetime NOT NULL CONSTRAINT [DF_sms_template_create_date] DEFAULT (GETDATE()),
    [update_date] datetime NULL,
    [update_user] nvarchar(50) NULL,
    CONSTRAINT [PK_sms_template] PRIMARY KEY ([template_id])
);

CREATE TABLE [dbo].[sms_template_language] (
    [template_language_id] int NOT NULL IDENTITY(1, 1),
    [template_id] int NOT NULL,
    [language_code] varchar(10) NOT NULL,
    [template_text] nvarchar(1000) NOT NULL,
    [is_default] bit NOT NULL CONSTRAINT [DF_sms_template_language_is_default] DEFAULT (0),
    [is_active] bit NOT NULL CONSTRAINT [DF_sms_template_language_is_active] DEFAULT (1),
    [create_date] datetime NOT NULL CONSTRAINT [DF_sms_template_language_create_date] DEFAULT (GETDATE()),
    [update_date] datetime NULL,
    [update_user] nvarchar(50) NULL,
    CONSTRAINT [PK_sms_template_language] PRIMARY KEY ([template_language_id])
);

CREATE TABLE [dbo].[sms_trigger_catalog] (
    [trigger_id] int NOT NULL IDENTITY(1, 1),
    [trigger_code] varchar(50) NOT NULL,
    [trigger_name] varchar(100) NOT NULL,
    [description] varchar(500) NULL,
    [is_active] bit NOT NULL CONSTRAINT [DF_sms_trigger_catalog_is_active] DEFAULT (1),
    [create_date] datetime NOT NULL CONSTRAINT [DF_sms_trigger_catalog_create_date] DEFAULT (GETDATE()),
    [update_date] datetime NULL,
    CONSTRAINT [PK_sms_trigger_catalog] PRIMARY KEY ([trigger_id])
);

CREATE TABLE [dbo].[sms_unit] (
    [sms_unit_id] int NOT NULL IDENTITY(1, 1),
    [hospital_id] int NOT NULL,
    [unit_id] int NOT NULL,
    [unit_name] varchar(50) NOT NULL,
    [is_active] bit NOT NULL CONSTRAINT [DF_sms_unit_is_active] DEFAULT (1),
    [create_date] datetime NOT NULL CONSTRAINT [DF_sms_unit_create_date] DEFAULT (GETDATE()),
    [update_date] datetime NULL,
    CONSTRAINT [PK_sms_unit] PRIMARY KEY ([sms_unit_id])
);

CREATE TABLE [dbo].[sms_unit_category] (
    [uc_id] int NOT NULL IDENTITY(1, 1),
    [sms_unit_id] int NOT NULL,
    [category_id] int NOT NULL,
    [is_active] bit NOT NULL CONSTRAINT [DF_sms_unit_category_is_active] DEFAULT (1),
    [create_date] datetime NOT NULL CONSTRAINT [DF_sms_unit_category_create_date] DEFAULT (GETDATE()),
    [update_user] nvarchar(50) NULL,
    CONSTRAINT [PK_sms_unit_category] PRIMARY KEY ([uc_id])
);

CREATE TABLE [dbo].[sms_template_trigger] (
    [tt_id] int NOT NULL IDENTITY(1, 1),
    [template_id] int NOT NULL,
    [trigger_id] int NOT NULL,
    [create_date] datetime NOT NULL CONSTRAINT [DF_sms_template_trigger_create_date] DEFAULT (GETDATE()),
    [update_date] datetime NULL,
    [update_user] nvarchar(50) NULL,
    CONSTRAINT [PK_sms_template_trigger] PRIMARY KEY ([tt_id]),
    CONSTRAINT [FK_sms_template_trigger_sms_template_template_id]
        FOREIGN KEY ([template_id]) REFERENCES [dbo].[sms_template] ([template_id])
);

CREATE TABLE [dbo].[sms_rules] (
    [rule_id] int NOT NULL IDENTITY(1, 1),
    [tt_id] int NOT NULL,
    [depend_on_tt_id] int NULL,
    [dependency_max_minutes] float NULL CONSTRAINT [DF_sms_rules_dependency_max_minutes] DEFAULT (0),
    [max_time_tokef_in_minutes] float NULL CONSTRAINT [DF_sms_rules_max_time_tokef_in_minutes] DEFAULT (60),
    [is_constant] bit NULL,
    [start_time_range] time(7) NULL,
    [end_time_range] time(7) NULL,
    [is_recurring] bit NULL,
    [recurring_interval_days] int NULL,
    [recurring_time_of_day] varchar(20) NULL,
    [recurring_stop_condition] varchar(50) NULL,
    [create_date] datetime NOT NULL,
    [update_date] datetime NULL,
    [update_user] nvarchar(50) NULL,
    CONSTRAINT [PK_sms_rules] PRIMARY KEY ([rule_id]),
    CONSTRAINT [FK_sms_rules_sms_template_trigger_tt_id]
        FOREIGN KEY ([tt_id]) REFERENCES [dbo].[sms_template_trigger] ([tt_id])
);

CREATE TABLE [dbo].[sms_test_phone_whitelist] (
    [phone_num_male] bigint NOT NULL,
    [name] nvarchar(100) NOT NULL,
    [is_active] bit NOT NULL CONSTRAINT [DF_sms_test_phone_whitelist_is_active] DEFAULT (1),
    [create_date] datetime NOT NULL CONSTRAINT [DF_sms_test_phone_whitelist_create_date] DEFAULT (GETDATE()),
    CONSTRAINT [PK_sms_test_phone_whitelist] PRIMARY KEY ([phone_num_male])
);

CREATE UNIQUE INDEX [UX_sms_rules_tt_id]
    ON [dbo].[sms_rules] ([tt_id]);

CREATE UNIQUE INDEX [UX_sms_template_trigger_template_id]
    ON [dbo].[sms_template_trigger] ([template_id]);

CREATE UNIQUE INDEX [UX_sms_unit_category_unit_category]
    ON [dbo].[sms_unit_category] ([sms_unit_id], [category_id]);

CREATE SEQUENCE [dbo].[sms_demo_receipt_seq]
    AS bigint
    START WITH 100000
    INCREMENT BY 1;

COMMIT TRANSACTION;
GO

CREATE VIEW [dbo].[sms_unit_category_v]
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

CREATE PROCEDURE [dbo].[sp_sms_dev_send_delta_queue_add]
    @category_id int,
    @template_id int,
    @language_id char(2),
    @msg_text nvarchar(1000),
    @phone_num bigint
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @project_id int;
    DECLARE @language_code tinyint;
    DECLARE @receipt_number bigint;

    SELECT @project_id = c.[project_id]
    FROM [dbo].[sms_category] AS c
    INNER JOIN [dbo].[sms_template] AS t
        ON t.[category_id] = c.[category_id]
    WHERE c.[category_id] = @category_id
      AND t.[template_id] = @template_id;

    IF @project_id IS NULL
    BEGIN
        THROW 51010, 'The template does not belong to the requested category.', 1;
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM [dbo].[sms_test_phone_whitelist]
        WHERE [phone_num_male] = @phone_num
          AND [is_active] = 1
    )
    BEGIN
        THROW 51011, 'The phone number is not present in the demo whitelist.', 1;
    END;

    SET @language_code =
        CASE LOWER(RTRIM(@language_id))
            WHEN 'he' THEN 1
            WHEN 'en' THEN 2
            WHEN 'ar' THEN 3
            WHEN 'ru' THEN 4
            ELSE 2
        END;

    SELECT @receipt_number = NEXT VALUE FOR [dbo].[sms_demo_receipt_seq];

    INSERT INTO [dbo].[bthol_SMS_2_send_list_delta] (
        [kod_msg_project],
        [kod_sug_msg],
        [kod_sug_msg_rn],
        [kod_mosad],
        [kod_language],
        [mispar_kabala],
        [phone_num_male],
        [kod_machlaka],
        [msg_txt],
        [taarich_lechishuv],
        [teudat_zehut_src],
        [id],
        [password],
        [sw_test],
        [sw_started],
        [date_created],
        [date_started],
        [kod_makor_phone_no]
    )
    VALUES (
        @project_id,
        @template_id,
        1,
        1,
        @language_code,
        CONVERT(varchar(20), @receipt_number),
        CONVERT(varchar(20), @phone_num),
        0,
        @msg_text,
        GETDATE(),
        '000000000',
        'PUBLIC_DEMO',
        'NOT_A_REAL_SECRET',
        1,
        0,
        GETDATE(),
        NULL,
        0
    );
END;
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;

SET IDENTITY_INSERT [dbo].[sms_project] ON;
INSERT INTO [dbo].[sms_project] (
    [project_id], [project_name], [is_active], [create_date], [update_date]
)
VALUES
    (1, 'Maccabi Patient Messaging Demo', 1, GETDATE(), NULL),
    (2, 'Assuta Procedure Journey Demo', 1, GETDATE(), NULL),
    (3, 'Community Care Messaging Demo', 1, GETDATE(), NULL);
SET IDENTITY_INSERT [dbo].[sms_project] OFF;

SET IDENTITY_INSERT [dbo].[sms_hospital] ON;
INSERT INTO [dbo].[sms_hospital] (
    [hospital_id],
    [hospital_type_id],
    [hospital_description],
    [hospital_name],
    [is_active],
    [create_date],
    [update_date]
)
VALUES
    (1, 1, 'Maccabi Demo', 'Maccabi Demo Center', 1, GETDATE(), NULL),
    (2, 2, 'Assuta Demo', 'Assuta Demo Hospital', 1, GETDATE(), NULL),
    (3, 1, 'Meuhedet Demo', 'Meuhedet Demo Clinic', 1, GETDATE(), NULL),
    (4, 1, 'Leumit Demo', 'Leumit Demo Center', 1, GETDATE(), NULL);
SET IDENTITY_INSERT [dbo].[sms_hospital] OFF;

SET IDENTITY_INSERT [dbo].[sms_category] ON;
INSERT INTO [dbo].[sms_category] (
    [category_id],
    [category_name],
    [project_id],
    [is_active],
    [is_editable],
    [create_date],
    [update_date]
)
VALUES
    (1, 'Appointments', 1, 1, 1, GETDATE(), NULL),
    (2, 'Lab Results', 1, 1, 1, GETDATE(), NULL),
    (3, 'Pre-Procedure', 2, 1, 1, GETDATE(), NULL),
    (4, 'Discharge', 2, 1, 1, GETDATE(), NULL),
    (5, 'Medication Reminders', 3, 1, 1, GETDATE(), NULL),
    (6, 'Follow-up', 3, 1, 1, GETDATE(), NULL);
SET IDENTITY_INSERT [dbo].[sms_category] OFF;

SET IDENTITY_INSERT [dbo].[sms_unit] ON;
INSERT INTO [dbo].[sms_unit] (
    [sms_unit_id],
    [hospital_id],
    [unit_id],
    [unit_name],
    [is_active],
    [create_date],
    [update_date]
)
VALUES
    (1, 1, 101, 'Family Medicine - Demo', 1, GETDATE(), NULL),
    (2, 1, 102, 'Laboratory - Demo', 1, GETDATE(), NULL),
    (3, 2, 201, 'Surgery Reception - Demo', 1, GETDATE(), NULL),
    (4, 2, 202, 'Day Hospital - Demo', 1, GETDATE(), NULL),
    (5, 3, 301, 'Community Clinic - Demo', 1, GETDATE(), NULL),
    (6, 3, 302, 'Nursing Center - Demo', 1, GETDATE(), NULL),
    (7, 4, 401, 'Family Clinic - Demo', 1, GETDATE(), NULL),
    (8, 4, 402, 'Pharmacy Support - Demo', 1, GETDATE(), NULL);
SET IDENTITY_INSERT [dbo].[sms_unit] OFF;

SET IDENTITY_INSERT [dbo].[sms_placeholder_catalog] ON;
INSERT INTO [dbo].[sms_placeholder_catalog] (
    [placeholder_id],
    [placeholder_name],
    [display_name],
    [source_type],
    [value_source],
    [is_required],
    [is_static],
    [is_editable],
    [default_value],
    [create_date],
    [update_date]
)
VALUES
    (1, '{PatientName}', 'Patient name', 'dynamic', 'patient.name', 1, 0, 1, 'Demo Patient', GETDATE(), NULL),
    (2, '{HospitalName}', 'Organization name', 'dynamic', 'hospital.name', 1, 0, 1, 'Demo Medical Center', GETDATE(), NULL),
    (3, '{AppointmentDate}', 'Appointment date', 'dynamic', 'appointment.date', 0, 0, 1, '01/01/2030', GETDATE(), NULL),
    (4, '{AppointmentTime}', 'Appointment time', 'dynamic', 'appointment.time', 0, 0, 1, '10:00', GETDATE(), NULL),
    (5, '{UnitName}', 'Unit name', 'dynamic', 'unit.name', 0, 0, 1, 'Demo Unit', GETDATE(), NULL),
    (6, '{ContactPhone}', 'Contact phone', 'static', 'sms_static', 0, 1, 1, '1-555-0100', GETDATE(), NULL),
    (7, '{OptOutLink}', 'Opt-out link', 'dynamic', 'links.optout', 0, 0, 1, 'https://example.org/optout', GETDATE(), NULL),
    (8, '{OrganizationName}', 'Organization display name', 'static', 'sms_static', 1, 1, 1, 'Demo Health', GETDATE(), NULL);
SET IDENTITY_INSERT [dbo].[sms_placeholder_catalog] OFF;

INSERT INTO [dbo].[sms_placeholder_scope] (
    [placeholder_id],
    [project_id],
    [category_id],
    [is_active],
    [create_date],
    [update_date]
)
SELECT
    p.[placeholder_id],
    project_scope.[project_id],
    NULL,
    1,
    GETDATE(),
    NULL
FROM [dbo].[sms_placeholder_catalog] AS p
CROSS JOIN (VALUES (1), (2), (3)) AS project_scope([project_id]);

SET IDENTITY_INSERT [dbo].[sms_static] ON;
INSERT INTO [dbo].[sms_static] (
    [static_id],
    [placeholder_id],
    [category_id],
    [hospital_id],
    [unit_id],
    [source_code],
    [value_name],
    [static_value],
    [is_active],
    [create_date],
    [update_date]
)
VALUES
    (1, 6, NULL, 1, NULL, 1001, N'Demo support line', N'1-555-0101', 1, GETDATE(), NULL),
    (2, 6, NULL, 2, NULL, 1002, N'Demo support line', N'1-555-0102', 1, GETDATE(), NULL),
    (3, 6, NULL, 3, NULL, 1003, N'Demo support line', N'1-555-0103', 1, GETDATE(), NULL),
    (4, 6, NULL, 4, NULL, 1004, N'Demo support line', N'1-555-0104', 1, GETDATE(), NULL),
    (5, 8, NULL, 1, NULL, 2001, N'Demo organization', N'Maccabi Demo Center', 1, GETDATE(), NULL),
    (6, 8, NULL, 2, NULL, 2002, N'Demo organization', N'Assuta Demo Hospital', 1, GETDATE(), NULL),
    (7, 8, NULL, 3, NULL, 2003, N'Demo organization', N'Meuhedet Demo Clinic', 1, GETDATE(), NULL),
    (8, 8, NULL, 4, NULL, 2004, N'Demo organization', N'Leumit Demo Center', 1, GETDATE(), NULL);
SET IDENTITY_INSERT [dbo].[sms_static] OFF;

SET IDENTITY_INSERT [dbo].[sms_template] ON;
INSERT INTO [dbo].[sms_template] (
    [template_id],
    [template_description],
    [template_name],
    [category_id],
    [is_active],
    [is_editable],
    [version_number],
    [create_date],
    [update_date],
    [update_user]
)
VALUES
    (1, 'DEMO_APPOINTMENT_REMINDER', 'Appointment reminder', 1, 1, 1, 1, GETDATE(), NULL, N'EXAMPLE\demo.admin'),
    (2, 'DEMO_LAB_RESULTS_READY', 'Lab results ready', 2, 1, 1, 1, GETDATE(), NULL, N'EXAMPLE\demo.admin'),
    (3, 'DEMO_PROCEDURE_PREPARATION', 'Procedure preparation', 3, 1, 1, 1, GETDATE(), NULL, N'EXAMPLE\demo.admin'),
    (4, 'DEMO_DISCHARGE_INSTRUCTIONS', 'Discharge instructions', 4, 1, 1, 1, GETDATE(), NULL, N'EXAMPLE\demo.admin'),
    (5, 'DEMO_MEDICATION_REMINDER', 'Medication reminder', 5, 1, 1, 1, GETDATE(), NULL, N'EXAMPLE\demo.admin'),
    (6, 'DEMO_FOLLOWUP_REMINDER', 'Follow-up reminder', 6, 1, 1, 1, GETDATE(), NULL, N'EXAMPLE\demo.admin');
SET IDENTITY_INSERT [dbo].[sms_template] OFF;

INSERT INTO [dbo].[sms_category_step] (
    [category_id],
    [template_id],
    [dependentat_id],
    [delay_in_minutes],
    [is_active],
    [is_editable],
    [create_date],
    [update_date]
)
VALUES
    (1, 1, NULL, 0, 1, 1, GETDATE(), NULL),
    (2, 2, NULL, 0, 1, 1, GETDATE(), NULL),
    (3, 3, NULL, 0, 1, 1, GETDATE(), NULL),
    (4, 4, 3, 30, 1, 1, GETDATE(), NULL),
    (5, 5, NULL, 0, 1, 1, GETDATE(), NULL),
    (6, 6, NULL, 60, 1, 1, GETDATE(), NULL);

SET IDENTITY_INSERT [dbo].[sms_template_language] ON;
INSERT INTO [dbo].[sms_template_language] (
    [template_language_id],
    [template_id],
    [language_code],
    [template_text],
    [is_default],
    [is_active],
    [create_date],
    [update_date],
    [update_user]
)
VALUES
    (1, 1, 'he', N'שלום {PatientName}, נקבע עבורך תור ב-{HospitalName} בתאריך {AppointmentDate} בשעה {AppointmentTime}.', 1, 1, GETDATE(), NULL, N'EXAMPLE\demo.admin'),
    (2, 1, 'en', N'Hello {PatientName}, your appointment at {HospitalName} is scheduled for {AppointmentDate} at {AppointmentTime}.', 0, 1, GETDATE(), NULL, N'EXAMPLE\demo.admin'),
    (3, 2, 'he', N'שלום {PatientName}, תוצאות הבדיקה שלך זמינות לצפייה באזור ההדגמה. לפרטים: {ContactPhone}.', 1, 1, GETDATE(), NULL, N'EXAMPLE\demo.admin'),
    (4, 2, 'en', N'Hello {PatientName}, your demo lab results are ready. For assistance call {ContactPhone}.', 0, 1, GETDATE(), NULL, N'EXAMPLE\demo.admin'),
    (5, 3, 'he', N'שלום {PatientName}, לקראת הפעולה ב-{HospitalName} יש להגיע ל-{UnitName} במועד שנקבע.', 1, 1, GETDATE(), NULL, N'EXAMPLE\demo.admin'),
    (6, 3, 'en', N'Hello {PatientName}, please arrive at {UnitName} for your scheduled procedure at {HospitalName}.', 0, 1, GETDATE(), NULL, N'EXAMPLE\demo.admin'),
    (7, 4, 'he', N'שלום {PatientName}, מצורפות הנחיות שחרור לדוגמה מ-{HospitalName}. לשאלות: {ContactPhone}.', 1, 1, GETDATE(), NULL, N'EXAMPLE\demo.admin'),
    (8, 4, 'en', N'Hello {PatientName}, these are demo discharge instructions from {HospitalName}. Contact {ContactPhone}.', 0, 1, GETDATE(), NULL, N'EXAMPLE\demo.admin'),
    (9, 5, 'he', N'שלום {PatientName}, זוהי תזכורת הדגמה לנטילת התרופה בהתאם להנחיות הצוות המטפל.', 1, 1, GETDATE(), NULL, N'EXAMPLE\demo.admin'),
    (10, 5, 'en', N'Hello {PatientName}, this is a demo reminder to take your medication as instructed.', 0, 1, GETDATE(), NULL, N'EXAMPLE\demo.admin'),
    (11, 6, 'he', N'שלום {PatientName}, נשמח לתאם מעקב ב-{HospitalName}. ניתן ליצור קשר במספר {ContactPhone}.', 1, 1, GETDATE(), NULL, N'EXAMPLE\demo.admin'),
    (12, 6, 'en', N'Hello {PatientName}, please schedule your demo follow-up at {HospitalName} by calling {ContactPhone}.', 0, 1, GETDATE(), NULL, N'EXAMPLE\demo.admin');
SET IDENTITY_INSERT [dbo].[sms_template_language] OFF;

SET IDENTITY_INSERT [dbo].[sms_trigger_catalog] ON;
INSERT INTO [dbo].[sms_trigger_catalog] (
    [trigger_id],
    [trigger_code],
    [trigger_name],
    [description],
    [is_active],
    [create_date],
    [update_date]
)
VALUES
    (1, 'APPOINTMENT_SCHEDULED', 'Appointment scheduled', 'Demo trigger fired when an appointment is scheduled.', 1, GETDATE(), NULL),
    (2, 'LAB_RESULT_READY', 'Lab result ready', 'Demo trigger fired when a laboratory result is ready.', 1, GETDATE(), NULL),
    (3, 'PROCEDURE_SCHEDULED', 'Procedure scheduled', 'Demo trigger fired before a procedure.', 1, GETDATE(), NULL),
    (4, 'PATIENT_DISCHARGED', 'Patient discharged', 'Demo trigger fired after discharge.', 1, GETDATE(), NULL),
    (5, 'DAILY_AT_TIME', 'Daily at configured time', 'Demo recurring reminder trigger.', 1, GETDATE(), NULL),
    (6, 'FOLLOWUP_DUE', 'Follow-up due', 'Demo trigger fired when follow-up is due.', 1, GETDATE(), NULL);
SET IDENTITY_INSERT [dbo].[sms_trigger_catalog] OFF;

SET IDENTITY_INSERT [dbo].[sms_template_trigger] ON;
INSERT INTO [dbo].[sms_template_trigger] (
    [tt_id],
    [template_id],
    [trigger_id],
    [create_date],
    [update_date],
    [update_user]
)
VALUES
    (1, 1, 1, GETDATE(), NULL, N'EXAMPLE\demo.admin'),
    (2, 2, 2, GETDATE(), NULL, N'EXAMPLE\demo.admin'),
    (3, 3, 3, GETDATE(), NULL, N'EXAMPLE\demo.admin'),
    (4, 4, 4, GETDATE(), NULL, N'EXAMPLE\demo.admin'),
    (5, 5, 5, GETDATE(), NULL, N'EXAMPLE\demo.admin'),
    (6, 6, 6, GETDATE(), NULL, N'EXAMPLE\demo.admin');
SET IDENTITY_INSERT [dbo].[sms_template_trigger] OFF;

SET IDENTITY_INSERT [dbo].[sms_rules] ON;
INSERT INTO [dbo].[sms_rules] (
    [rule_id],
    [tt_id],
    [depend_on_tt_id],
    [dependency_max_minutes],
    [max_time_tokef_in_minutes],
    [is_constant],
    [start_time_range],
    [end_time_range],
    [is_recurring],
    [recurring_interval_days],
    [recurring_time_of_day],
    [recurring_stop_condition],
    [create_date],
    [update_date],
    [update_user]
)
VALUES
    (1, 1, NULL, 0, 1440, 0, '08:00', '20:00', 0, NULL, NULL, NULL, GETDATE(), NULL, N'EXAMPLE\demo.admin'),
    (2, 2, NULL, 0, 720, 0, '08:00', '20:00', 0, NULL, NULL, NULL, GETDATE(), NULL, N'EXAMPLE\demo.admin'),
    (3, 3, NULL, 0, 2880, 0, '08:00', '18:00', 0, NULL, NULL, NULL, GETDATE(), NULL, N'EXAMPLE\demo.admin'),
    (4, 4, 3, 720, 1440, 0, '08:00', '20:00', 0, NULL, NULL, NULL, GETDATE(), NULL, N'EXAMPLE\demo.admin'),
    (5, 5, NULL, 0, 10080, 1, '07:00', '21:00', 1, 1, '09:00', 'treatment_completed', GETDATE(), NULL, N'EXAMPLE\demo.admin'),
    (6, 6, NULL, 0, 4320, 0, '08:00', '20:00', 0, NULL, NULL, NULL, GETDATE(), NULL, N'EXAMPLE\demo.admin');
SET IDENTITY_INSERT [dbo].[sms_rules] OFF;

SET IDENTITY_INSERT [dbo].[sms_unit_category] ON;
INSERT INTO [dbo].[sms_unit_category] (
    [uc_id],
    [sms_unit_id],
    [category_id],
    [is_active],
    [create_date],
    [update_user]
)
VALUES
    (1, 1, 1, 1, GETDATE(), N'EXAMPLE\demo.admin'),
    (2, 2, 2, 1, GETDATE(), N'EXAMPLE\demo.admin'),
    (3, 3, 3, 1, GETDATE(), N'EXAMPLE\demo.admin'),
    (4, 4, 4, 1, GETDATE(), N'EXAMPLE\demo.admin'),
    (5, 5, 5, 1, GETDATE(), N'EXAMPLE\demo.admin'),
    (6, 6, 6, 1, GETDATE(), N'EXAMPLE\demo.admin'),
    (7, 7, 1, 1, GETDATE(), N'EXAMPLE\demo.admin'),
    (8, 8, 5, 1, GETDATE(), N'EXAMPLE\demo.admin');
SET IDENTITY_INSERT [dbo].[sms_unit_category] OFF;

INSERT INTO [dbo].[sms_reject_hospital_unit] (
    [template_id],
    [hospital_id],
    [unit_id],
    [create_date],
    [update_date]
)
VALUES
    (1, 2, 201, GETDATE(), NULL),
    (3, 1, 101, GETDATE(), NULL),
    (5, 2, 202, GETDATE(), NULL);

INSERT INTO [dbo].[sms_test_phone_whitelist] (
    [phone_num_male],
    [name],
    [is_active],
    [create_date]
)
VALUES
    (15550100001, N'Demo Recipient 1', 1, GETDATE()),
    (15550100002, N'Demo Recipient 2', 1, GETDATE()),
    (15550100003, N'Demo Recipient 3', 1, GETDATE());

INSERT INTO [dbo].[Sms_AuditLog] (
    [EntityName],
    [EntityId],
    [ActionType],
    [UserId],
    [OldValueJson],
    [NewValueJson],
    [DiffJson],
    [CreatedAt],
    [UpdatedAt]
)
VALUES (
    N'DemoDatabase',
    1,
    N'Seeded',
    NULL,
    NULL,
    N'{"source":"public-demo","containsRealPatientData":false}',
    N'{"status":"created"}',
    SYSUTCDATETIME(),
    NULL
);

COMMIT TRANSACTION;
GO

PRINT 'HospitalSms public demo database created successfully.';
SELECT
    (SELECT COUNT(*) FROM [dbo].[sms_project]) AS [project_count],
    (SELECT COUNT(*) FROM [dbo].[sms_hospital]) AS [hospital_count],
    (SELECT COUNT(*) FROM [dbo].[sms_template]) AS [template_count],
    (SELECT COUNT(*) FROM [dbo].[sms_template_language]) AS [template_language_count],
    (SELECT COUNT(*) FROM [dbo].[sms_unit]) AS [unit_count],
    (SELECT COUNT(*) FROM [dbo].[sms_test_phone_whitelist]) AS [test_phone_count];
GO
