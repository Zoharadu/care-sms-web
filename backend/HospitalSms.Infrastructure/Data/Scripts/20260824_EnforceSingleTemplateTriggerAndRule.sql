SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    ;WITH TemplateTriggerRanks AS
    (
        SELECT
            tt_id,
            template_id,
            MIN(tt_id) OVER (PARTITION BY template_id) AS retained_tt_id,
            ROW_NUMBER() OVER (PARTITION BY template_id ORDER BY tt_id) AS row_number
        FROM dbo.sms_template_trigger
    )
    UPDATE rule
    SET rule.depend_on_tt_id = ranked.retained_tt_id
    FROM dbo.sms_rules AS rule
    INNER JOIN TemplateTriggerRanks AS ranked
        ON ranked.tt_id = rule.depend_on_tt_id
    WHERE ranked.row_number > 1;

    ;WITH TemplateTriggerRanks AS
    (
        SELECT
            tt_id,
            template_id,
            MIN(tt_id) OVER (PARTITION BY template_id) AS retained_tt_id,
            ROW_NUMBER() OVER (PARTITION BY template_id ORDER BY tt_id) AS row_number
        FROM dbo.sms_template_trigger
    )
    UPDATE rule
    SET rule.tt_id = ranked.retained_tt_id
    FROM dbo.sms_rules AS rule
    INNER JOIN TemplateTriggerRanks AS ranked
        ON ranked.tt_id = rule.tt_id
    WHERE ranked.row_number > 1;

    ;WITH TemplateTriggerRanks AS
    (
        SELECT
            tt_id,
            ROW_NUMBER() OVER (PARTITION BY template_id ORDER BY tt_id) AS row_number
        FROM dbo.sms_template_trigger
    )
    DELETE template_trigger
    FROM dbo.sms_template_trigger AS template_trigger
    INNER JOIN TemplateTriggerRanks AS ranked
        ON ranked.tt_id = template_trigger.tt_id
    WHERE ranked.row_number > 1;

    ;WITH RuleRanks AS
    (
        SELECT
            rule_id,
            ROW_NUMBER() OVER (PARTITION BY tt_id ORDER BY rule_id) AS row_number
        FROM dbo.sms_rules
    )
    DELETE rule
    FROM dbo.sms_rules AS rule
    INNER JOIN RuleRanks AS ranked
        ON ranked.rule_id = rule.rule_id
    WHERE ranked.row_number > 1;

    IF EXISTS
    (
        SELECT 1
        FROM dbo.sms_template_trigger
        GROUP BY template_id
        HAVING COUNT(*) > 1
    )
    BEGIN
        THROW 51001, 'Duplicate template_id values remain in dbo.sms_template_trigger.', 1;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM dbo.sms_rules
        GROUP BY tt_id
        HAVING COUNT(*) > 1
    )
    BEGIN
        THROW 51002, 'Multiple rules remain for the same tt_id in dbo.sms_rules.', 1;
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.sms_template_trigger')
          AND name = N'UX_sms_template_trigger_template_id'
    )
    BEGIN
        CREATE UNIQUE INDEX UX_sms_template_trigger_template_id
            ON dbo.sms_template_trigger(template_id);
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.sms_rules')
          AND name = N'UX_sms_rules_tt_id'
    )
    BEGIN
        CREATE UNIQUE INDEX UX_sms_rules_tt_id
            ON dbo.sms_rules(tt_id);
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
    BEGIN
        ROLLBACK TRANSACTION;
    END;

    THROW;
END CATCH;
