using Microsoft.EntityFrameworkCore;
using HospitalSms.Domain.Entities;
using HospitalSms.Infrastructure.Data.Converters;

namespace HospitalSms.Infrastructure.Data;

public class HospitalSmsDbContext : DbContext
{
    public HospitalSmsDbContext(DbContextOptions<HospitalSmsDbContext> options) : base(options) { }

    public DbSet<BtholFactSentMessage> BtholFactSentMessages => Set<BtholFactSentMessage>();
    public DbSet<BtholSms2SendListDelta> BtholSms2SendListDeltas => Set<BtholSms2SendListDelta>();
    public DbSet<BtholSms2SendListDeltaDeleted> BtholSms2SendListDeltaDeleted => Set<BtholSms2SendListDeltaDeleted>();
    public DbSet<DdlLog> DdlLogs => Set<DdlLog>();
    public DbSet<SmsAuditLog> SmsAuditLogs => Set<SmsAuditLog>();
    public DbSet<SmsCategory> SmsCategories => Set<SmsCategory>();
    public DbSet<SmsProject> SmsProjects => Set<SmsProject>();
    public DbSet<SmsCategoryStep> SmsCategorySteps => Set<SmsCategoryStep>();
    public DbSet<SmsHospital> SmsHospitals => Set<SmsHospital>();
    public DbSet<SmsPlaceholderCatalog> SmsPlaceholderCatalogs => Set<SmsPlaceholderCatalog>();
    public DbSet<SmsPlaceholderScope> SmsPlaceholderScopes => Set<SmsPlaceholderScope>();
    public DbSet<SmsStatic> SmsStatics => Set<SmsStatic>();
    public DbSet<SmsTemplate> SmsTemplates => Set<SmsTemplate>();
    public DbSet<SmsRejectHospitalUnit> SmsRejectHospitalUnits => Set<SmsRejectHospitalUnit>();
    public DbSet<SmsTemplateTrigger> SmsTemplateTriggers => Set<SmsTemplateTrigger>();
    public DbSet<SmsRule> SmsRules => Set<SmsRule>();
    public DbSet<SmsTemplateLanguage> SmsTemplateLanguages => Set<SmsTemplateLanguage>();
    public DbSet<SmsTriggerCatalog> SmsTriggerCatalogs => Set<SmsTriggerCatalog>();
    public DbSet<SmsUnit> SmsUnits => Set<SmsUnit>();
    public DbSet<SmsUnitCategory> SmsUnitCategories => Set<SmsUnitCategory>();
    public DbSet<SmsUnitCategoryView> SmsUnitCategoryViews => Set<SmsUnitCategoryView>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("dbo");

        modelBuilder.Entity<BtholFactSentMessage>(entity =>
        {
            entity.ToTable("bthol_fact_sent_messages", "dbo");
            entity.HasKey(e => e.RowId);
            entity.Property(e => e.RowId).HasColumnName("row_id").UseIdentityColumn();
            entity.Property(e => e.KodMsgProject).HasColumnName("kod_msg_project");
            entity.Property(e => e.KodSugMsg).HasColumnName("kod_sug_msg");
            entity.Property(e => e.KodMosad).HasColumnName("kod_mosad");
            entity.Property(e => e.KodLanguage).HasColumnName("kod_language");
            entity.Property(e => e.MsgText).HasColumnName("msg_text").HasMaxLength(1000).IsRequired();
            entity.Property(e => e.MisparKabala).HasColumnName("mispar_kabala");
            entity.Property(e => e.TeudatZehutSrc).HasColumnName("teudat_zehut_src");
            entity.Property(e => e.PhoneNumMale).HasColumnName("phone_num_male");
            entity.Property(e => e.MsgTaarichShaa).HasColumnName("msg_taarich_shaa").HasColumnType("datetime");
            entity.Property(e => e.SapakMsgId).HasColumnName("sapak_msg_id");
            entity.Property(e => e.MsgStatus).HasColumnName("msg_status");
            entity.Property(e => e.TaarichLechishuv).HasColumnName("taarich_lechishuv").HasColumnType("datetime");
            entity.Property(e => e.KodMachlaka).HasColumnName("kod_machlaka");
            entity.Property(e => e.KodMakorPhoneNo).HasColumnName("kod_makor_phone_no");
        });

        modelBuilder.Entity<BtholSms2SendListDelta>(entity =>
        {
            entity.ToTable("bthol_SMS_2_send_list_delta", "dbo");
            entity.HasKey(e => new { e.KodMsgProject, e.KodSugMsg, e.KodSugMsgRn, e.KodMosad, e.KodLanguage, e.MisparKabala, e.PhoneNumMale, e.KodMachlaka })
                  .HasName("PK_bthol_SMS_2_send_list_delta");

            entity.Property(e => e.KodMsgProject).HasColumnName("kod_msg_project");
            entity.Property(e => e.KodSugMsg).HasColumnName("kod_sug_msg");
            entity.Property(e => e.KodSugMsgRn).HasColumnName("kod_sug_msg_rn");
            entity.Property(e => e.KodMosad).HasColumnName("kod_mosad");
            entity.Property(e => e.KodLanguage).HasColumnName("kod_language");
            entity.Property(e => e.MsgTxt).HasColumnName("msg_txt").HasMaxLength(1000).IsRequired();
            entity.Property(e => e.MisparKabala).HasColumnName("mispar_kabala").HasMaxLength(20).IsUnicode(false).IsRequired();
            entity.Property(e => e.TaarichLechishuv).HasColumnName("taarich_lechishuv").HasColumnType("datetime");
            entity.Property(e => e.TeudatZehutSrc).HasColumnName("teudat_zehut_src").HasMaxLength(20).IsUnicode(false).IsRequired();
            entity.Property(e => e.PhoneNumMale).HasColumnName("phone_num_male").HasMaxLength(20).IsUnicode(false).IsRequired();
            entity.Property(e => e.Id).HasColumnName("id").HasMaxLength(24).IsUnicode(false).IsRequired();
            entity.Property(e => e.Password).HasColumnName("password").HasMaxLength(33).IsUnicode(false).IsRequired();
            entity.Property(e => e.SwTest).HasColumnName("sw_test");
            entity.Property(e => e.KodMachlaka).HasColumnName("kod_machlaka");
            entity.Property(e => e.SwStarted).HasColumnName("sw_started");
            entity.Property(e => e.DateCreated).HasColumnName("date_created").HasColumnType("datetime");
            entity.Property(e => e.DateStarted).HasColumnName("date_started").HasColumnType("datetime");
            entity.Property(e => e.KodMakorPhoneNo).HasColumnName("kod_makor_phone_no");
        });

        modelBuilder.Entity<BtholSms2SendListDeltaDeleted>(entity =>
        {
            entity.ToTable("bthol_SMS_2_send_list_delta_deleted", "dbo");
            entity.HasKey(e => new { e.KodMsgProject, e.KodSugMsg, e.KodMosad, e.KodLanguage, e.MisparKabala, e.KodMachlaka, e.PhoneNumMale, e.DateCreated })
                  .HasName("PK_bthol_SMS_2_send_list_delta_deleted");

            entity.Property(e => e.KodMsgProject).HasColumnName("kod_msg_project");
            entity.Property(e => e.KodSugMsg).HasColumnName("kod_sug_msg");
            entity.Property(e => e.KodSugMsgRn).HasColumnName("kod_sug_msg_rn");
            entity.Property(e => e.KodMosad).HasColumnName("kod_mosad");
            entity.Property(e => e.KodLanguage).HasColumnName("kod_language");
            entity.Property(e => e.MsgTxt).HasColumnName("msg_txt").HasMaxLength(1000).IsRequired();
            entity.Property(e => e.MisparKabala).HasColumnName("mispar_kabala").HasMaxLength(20).IsUnicode(false).IsRequired();
            entity.Property(e => e.TaarichLechishuv).HasColumnName("taarich_lechishuv").HasColumnType("datetime");
            entity.Property(e => e.TeudatZehutSrc).HasColumnName("teudat_zehut_src").HasMaxLength(20).IsUnicode(false).IsRequired();
            entity.Property(e => e.PhoneNumMale).HasColumnName("phone_num_male").HasMaxLength(20).IsUnicode(false).IsRequired();
            entity.Property(e => e.Id).HasColumnName("id").HasMaxLength(24).IsUnicode(false).IsRequired();
            entity.Property(e => e.Password).HasColumnName("password").HasMaxLength(33).IsUnicode(false).IsRequired();
            entity.Property(e => e.SwTest).HasColumnName("sw_test");
            entity.Property(e => e.KodMachlaka).HasColumnName("kod_machlaka");
            entity.Property(e => e.SwStarted).HasColumnName("sw_started");
            entity.Property(e => e.DateCreated).HasColumnName("date_created").HasColumnType("datetime");
            entity.Property(e => e.DateStarted).HasColumnName("date_started").HasColumnType("datetime");
            entity.Property(e => e.DateDeleted).HasColumnName("date_deleted").HasColumnType("datetime");
            entity.Property(e => e.SwSent).HasColumnName("sw_sent");
            entity.Property(e => e.RowId).HasColumnName("row_id");
            entity.Property(e => e.KodMakorPhoneNo).HasColumnName("kod_makor_phone_no");
        });

        modelBuilder.Entity<DdlLog>(entity =>
        {
            entity.ToTable("ddl_log", "dbo");
            entity.HasNoKey();
            entity.Property(e => e.PostTime).HasColumnName("PostTime").HasColumnType("datetime");
            entity.Property(e => e.SysUser).HasColumnName("sys_user").HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.DbUser).HasColumnName("DB_User").HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.OriginalUserName).HasColumnName("OriginalUserName").HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.Host).HasColumnName("Host").HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.ApplicationName).HasColumnName("ApplicationName").HasMaxLength(400).IsUnicode(false);
            entity.Property(e => e.Event).HasColumnName("Event").HasMaxLength(100).IsUnicode(false);
            entity.Property(e => e.ObjectName).HasColumnName("ObjectName").HasMaxLength(100).IsFixedLength();
            entity.Property(e => e.Tsql).HasColumnName("TSQL").HasMaxLength(2000).IsUnicode(false);
            entity.Property(e => e.EventData).HasColumnName("EventData").HasColumnType("xml");
        });

        modelBuilder.Entity<SmsAuditLog>(entity =>
        {
            entity.ToTable("Sms_AuditLog", "dbo");
            entity.HasKey(e => e.AuditLogId);
            entity.Property(e => e.AuditLogId).HasColumnName("AuditLogId").UseIdentityColumn();
            entity.Property(e => e.EntityName).HasColumnName("EntityName").HasMaxLength(100).IsRequired();
            entity.Property(e => e.EntityId).HasColumnName("EntityId");
            entity.Property(e => e.ActionType).HasColumnName("ActionType").HasMaxLength(50).IsRequired();
            entity.Property(e => e.UserId).HasColumnName("UserId");
            entity.Property(e => e.OldValueJson).HasColumnName("OldValueJson");
            entity.Property(e => e.NewValueJson).HasColumnName("NewValueJson");
            entity.Property(e => e.DiffJson).HasColumnName("DiffJson");
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasDefaultValueSql("sysutcdatetime()");
            entity.Property(e => e.UpdatedAt).HasColumnName("UpdatedAt");
        });

        modelBuilder.Entity<SmsCategory>(entity =>
        {
            entity.ToTable("sms_category", "dbo");
            entity.HasKey(e => e.CategoryId);
            entity.Property(e => e.CategoryId).HasColumnName("category_id").UseIdentityColumn();
            entity.Property(e => e.CategoryName).HasColumnName("category_name").HasMaxLength(50).IsUnicode(false).IsRequired();
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(e => e.IsEditable).HasColumnName("is_editable").HasDefaultValue(true);
            entity.Property(e => e.CreateDate).HasColumnName("create_date").HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdateDate).HasColumnName("update_date").HasColumnType("datetime");
        });

        modelBuilder.Entity<SmsProject>(entity =>
        {
            entity.ToTable("sms_project", "dbo");
            entity.HasKey(e => e.ProjectId);
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.ProjectName).HasColumnName("project_name").HasMaxLength(50).IsUnicode(false).IsRequired();
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(e => e.CreateDate).HasColumnName("create_date").HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdateDate).HasColumnName("update_date").HasColumnType("datetime");
        });

        modelBuilder.Entity<SmsCategoryStep>(entity =>
        {
            entity.ToTable("sms_category_step", "dbo");
            entity.HasKey(e => e.CategoryStepId);
            entity.Property(e => e.CategoryStepId).HasColumnName("category_step_id").UseIdentityColumn();
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.TemplateId).HasColumnName("template_id");
            entity.Property(e => e.DependentatId).HasColumnName("dependentat_id");
            entity.Property(e => e.DelayInMinutes).HasColumnName("delay_in_minutes");
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(e => e.IsEditable).HasColumnName("is_editable").HasDefaultValue(true);
            entity.Property(e => e.CreateDate).HasColumnName("create_date").HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdateDate).HasColumnName("update_date").HasColumnType("datetime");
        });

        modelBuilder.Entity<SmsHospital>(entity =>
        {
            entity.ToTable("sms_hospital", "dbo");
            entity.HasKey(e => e.HospitalId);
            entity.Property(e => e.HospitalId).HasColumnName("hospital_id");
            entity.Property(e => e.HospitalTypeId).HasColumnName("hospital_type_id");
            entity.Property(e => e.HospitalDescription).HasColumnName("hospital_description").HasMaxLength(15).IsUnicode(false).IsRequired();
            entity.Property(e => e.HospitalName).HasColumnName("hospital_name").HasMaxLength(30).IsUnicode(false).IsRequired();
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(e => e.CreateDate).HasColumnName("create_date").HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdateDate).HasColumnName("update_date").HasColumnType("datetime");
        });

        modelBuilder.Entity<SmsPlaceholderCatalog>(entity =>
        {
            entity.ToTable("sms_placeholder_catalog", "dbo");
            entity.HasKey(e => e.PlaceholderId);
            entity.Property(e => e.PlaceholderId).HasColumnName("placeholder_id").UseIdentityColumn();
            entity.Property(e => e.PlaceholderName).HasColumnName("placeholder_name").HasMaxLength(100).IsUnicode(false).IsRequired();
            entity.Property(e => e.DisplayName).HasColumnName("display_name").HasMaxLength(100).IsUnicode(false).IsRequired();
            entity.Property(e => e.SourceType).HasColumnName("source_type").HasMaxLength(50).IsUnicode(false);
            entity.Property(e => e.ValueSource).HasColumnName("value_source").HasMaxLength(200).IsUnicode(false);
            entity.Property(e => e.IsRequired).HasColumnName("is_required").HasDefaultValue(false);
            entity.Property(e => e.IsStatic).HasColumnName("is_static").HasDefaultValue(false);
            entity.Property(e => e.IsEditable).HasColumnName("is_editable").HasDefaultValue(true);
            entity.Property(e => e.DefaultValue).HasColumnName("default_value").HasMaxLength(500).IsUnicode(false);
            entity.Property(e => e.CreateDate).HasColumnName("create_date").HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdateDate).HasColumnName("update_date").HasColumnType("datetime");
        });

        modelBuilder.Entity<SmsPlaceholderScope>(entity =>
        {
            entity.ToTable("sms_placeholder_scope", "dbo");
            entity.HasKey(e => e.PlaceholderScopeId);
            entity.Property(e => e.PlaceholderScopeId).HasColumnName("placeholder_scope_id").UseIdentityColumn();
            entity.Property(e => e.PlaceholderId).HasColumnName("placeholder_id");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(e => e.CreateDate).HasColumnName("create_date").HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdateDate).HasColumnName("update_date").HasColumnType("datetime");
        });

        modelBuilder.Entity<SmsStatic>(entity =>
        {
            entity.ToTable("sms_static", "dbo");
            entity.HasKey(e => e.StaticId);
            entity.Property(e => e.StaticId).HasColumnName("static_id").UseIdentityColumn();
            entity.Property(e => e.PlaceholderId).HasColumnName("placeholder_id");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.HospitalId).HasColumnName("hospital_id");
            entity.Property(e => e.UnitId).HasColumnName("unit_id");
            entity.Property(e => e.SourceCode).HasColumnName("source_code");
            entity.Property(e => e.ValueName).HasColumnName("value_name").HasMaxLength(500);
            entity.Property(e => e.StaticValue).HasColumnName("static_value").HasMaxLength(2000).IsRequired();
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(e => e.CreateDate).HasColumnName("create_date").HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdateDate).HasColumnName("update_date").HasColumnType("datetime");
        });

        modelBuilder.Entity<SmsTemplate>(entity =>
        {
            entity.ToTable("sms_template", "dbo");
            entity.HasKey(e => e.TemplateId);
            entity.Property(e => e.TemplateId).HasColumnName("template_id").UseIdentityColumn();
            entity.Property(e => e.TemplateCode).HasColumnName("template_description").HasMaxLength(300).IsUnicode(false);
            entity.Property(e => e.TemplateName).HasColumnName("template_name").HasMaxLength(100).IsUnicode(false).IsRequired();
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(e => e.IsEditable).HasColumnName("is_editable").HasDefaultValue(true);
            entity.Property(e => e.VersionNumber).HasColumnName("version_number").HasDefaultValue(1);
            entity.Property(e => e.CreateDate).HasColumnName("create_date").HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdateDate).HasColumnName("update_date").HasColumnType("datetime");
            entity.Property(e => e.UpdateUser).HasColumnName("update_user").HasColumnType("nvarchar(50)").HasMaxLength(50).IsUnicode();
        });

        modelBuilder.Entity<SmsRejectHospitalUnit>(entity =>
        {
            entity.ToTable("sms_reject_hospital_unit", "dbo");
            entity.HasKey(e => new { e.TemplateId, e.HospitalId, e.UnitId });
            entity.Property(e => e.TemplateId).HasColumnName("template_id");
            entity.Property(e => e.HospitalId).HasColumnName("hospital_id");
            entity.Property(e => e.UnitId).HasColumnName("unit_id");
            entity.Property(e => e.CreateDate).HasColumnName("create_date").HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdateDate).HasColumnName("update_date").HasColumnType("datetime");
        });

        modelBuilder.Entity<SmsTemplateTrigger>(entity =>
        {
            entity.ToTable("sms_template_trigger", "dbo");
            entity.HasKey(e => e.TtId);
            entity.HasIndex(e => e.TemplateId)
                .IsUnique()
                .HasDatabaseName("UX_sms_template_trigger_template_id");
            entity.Property(e => e.TtId).HasColumnName("tt_id").UseIdentityColumn();
            entity.Property(e => e.TemplateId).HasColumnName("template_id");
            entity.Property(e => e.TriggerId).HasColumnName("trigger_id");
            entity.Property(e => e.CreateDate).HasColumnName("create_date").HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdateDate).HasColumnName("update_date").HasColumnType("datetime");
            entity.Property(e => e.UpdateUser).HasColumnName("update_user").HasColumnType("nvarchar(50)").HasMaxLength(50).IsUnicode();
            entity.HasOne<SmsTemplate>()
                .WithMany()
                .HasForeignKey(e => e.TemplateId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<SmsRule>(entity =>
        {
            entity.ToTable("sms_rules", "dbo");
            entity.HasKey(e => e.RuleId);
            entity.HasIndex(e => e.TtId)
                .IsUnique()
                .HasDatabaseName("UX_sms_rules_tt_id");
            entity.Property(e => e.RuleId).HasColumnName("rule_id").UseIdentityColumn();
            entity.Property(e => e.TtId).HasColumnName("tt_id").IsRequired();
            entity.Property(e => e.DependOnTtId).HasColumnName("depend_on_tt_id");
            entity.Property(e => e.DependencyMaxMinutes).HasColumnName("dependency_max_minutes").HasDefaultValue(0d);
            entity.Property(e => e.MaxTimeTokefInMinutes).HasColumnName("max_time_tokef_in_minutes").HasDefaultValue(60d);
            entity.Property(e => e.IsConstant).HasColumnName("is_constant");
            entity.Property(e => e.StartTimeRange)
                .HasColumnName("start_time_range")
                .HasColumnType("time(7)")
                .HasMaxLength(5)
                .HasConversion<TimeRangeValueConverter>();
            entity.Property(e => e.EndTimeRange)
                .HasColumnName("end_time_range")
                .HasColumnType("time(7)")
                .HasMaxLength(5)
                .HasConversion<TimeRangeValueConverter>();
            entity.Property(e => e.IsRecurring).HasColumnName("is_recurring");
            entity.Property(e => e.RecurringIntervalDays).HasColumnName("recurring_interval_days");
            entity.Property(e => e.RecurringTimeOfDay).HasColumnName("recurring_time_of_day").HasMaxLength(20).IsUnicode(false);
            entity.Property(e => e.RecurringStopCondition).HasColumnName("recurring_stop_condition").HasMaxLength(50).IsUnicode(false);
            entity.Property(e => e.CreateDate).HasColumnName("create_date").HasColumnType("datetime").IsRequired();
            entity.Property(e => e.UpdateDate).HasColumnName("update_date").HasColumnType("datetime");
            entity.Property(e => e.UpdateUser).HasColumnName("update_user").HasColumnType("nvarchar(50)").HasMaxLength(50).IsUnicode();
            entity.HasOne<SmsTemplateTrigger>()
                .WithMany()
                .HasForeignKey(e => e.TtId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<SmsTemplateLanguage>(entity =>
        {
            entity.ToTable("sms_template_language", "dbo");
            entity.HasKey(e => e.TemplateLanguageId);
            entity.Property(e => e.TemplateLanguageId).HasColumnName("template_language_id").UseIdentityColumn();
            entity.Property(e => e.TemplateId).HasColumnName("template_id");
            entity.Property(e => e.LanguageCode).HasColumnName("language_code").HasMaxLength(10).IsUnicode(false).IsRequired();
            entity.Property(e => e.TemplateText).HasColumnName("template_text").HasMaxLength(1000).IsRequired();
            entity.Property(e => e.IsDefault).HasColumnName("is_default").HasDefaultValue(false);
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(e => e.CreateDate).HasColumnName("create_date").HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdateDate).HasColumnName("update_date").HasColumnType("datetime");
            entity.Property(e => e.UpdateUser).HasColumnName("update_user").HasColumnType("nvarchar(50)").HasMaxLength(50).IsUnicode();
        });

        modelBuilder.Entity<SmsTriggerCatalog>(entity =>
        {
            entity.ToTable("sms_trigger_catalog", "dbo");
            entity.HasKey(e => e.TriggerId);
            entity.Property(e => e.TriggerId).HasColumnName("trigger_id").UseIdentityColumn();
            entity.Property(e => e.TriggerCode).HasColumnName("trigger_code").HasMaxLength(50).IsUnicode(false).IsRequired();
            entity.Property(e => e.TriggerName).HasColumnName("trigger_name").HasMaxLength(100).IsUnicode(false).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(500).IsUnicode(false);
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(e => e.CreateDate).HasColumnName("create_date").HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdateDate).HasColumnName("update_date").HasColumnType("datetime");
        });

        modelBuilder.Entity<SmsUnit>(entity =>
        {
            entity.ToTable("sms_unit", "dbo");
            entity.HasKey(e => e.SmsUnitId);
            entity.Property(e => e.SmsUnitId).HasColumnName("sms_unit_id").UseIdentityColumn();
            entity.Property(e => e.HospitalId).HasColumnName("hospital_id");
            entity.Property(e => e.UnitId).HasColumnName("unit_id");
            entity.Property(e => e.UnitName).HasColumnName("unit_name").HasMaxLength(50).IsUnicode(false).IsRequired();
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(e => e.CreateDate).HasColumnName("create_date").HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdateDate).HasColumnName("update_date").HasColumnType("datetime");
        });

        modelBuilder.Entity<SmsUnitCategory>(entity =>
        {
            entity.ToTable("sms_unit_category", "dbo");
            entity.HasKey(e => e.UcId);
            entity.Property(e => e.UcId).HasColumnName("uc_id").UseIdentityColumn();
            entity.Property(e => e.SmsUnitId).HasColumnName("sms_unit_id");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(e => e.CreateDate).HasColumnName("create_date").HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.UpdateUser).HasColumnName("update_user").HasMaxLength(50).IsUnicode();
        });

        modelBuilder.Entity<SmsUnitCategoryView>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("sms_unit_category_v", "dbo");
            entity.Property(e => e.UcId).HasColumnName("uc_id");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.ProjectName).HasColumnName("project_name");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.CategoryName).HasColumnName("category_name");
            entity.Property(e => e.HospitalId).HasColumnName("hospital_id");
            entity.Property(e => e.HospitalName).HasColumnName("hospital_name");
            entity.Property(e => e.SmsUnitId).HasColumnName("sms_unit_id");
            entity.Property(e => e.UnitId).HasColumnName("unit_id");
            entity.Property(e => e.UnitName).HasColumnName("unit_name");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
        });

    }
}
