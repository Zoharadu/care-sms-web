using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HospitalSms.Domain.Entities;

[Table("bthol_fact_sent_messages", Schema = "dbo")]
public class BtholFactSentMessage
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("row_id")]
    public long RowId { get; set; }

    [Column("kod_msg_project")]
    public int KodMsgProject { get; set; }

    [Column("kod_sug_msg")]
    public int KodSugMsg { get; set; }

    [Column("kod_mosad")]
    public int KodMosad { get; set; }

    [Column("kod_language")]
    public byte KodLanguage { get; set; }

    [Column("msg_text")]
    [MaxLength(1000)]
    public string MsgText { get; set; } = null!;

    [Column("mispar_kabala")]
    public long MisparKabala { get; set; }

    [Column("teudat_zehut_src")]
    public long TeudatZehutSrc { get; set; }

    [Column("phone_num_male")]
    public long PhoneNumMale { get; set; }

    [Column("msg_taarich_shaa")]
    public DateTime? MsgTaarichShaa { get; set; }

    [Column("sapak_msg_id")]
    public long SapakMsgId { get; set; }

    [Column("msg_status")]
    public byte MsgStatus { get; set; }

    [Column("taarich_lechishuv")]
    public DateTime? TaarichLechishuv { get; set; }

    [Column("kod_machlaka")]
    public int? KodMachlaka { get; set; }

    [Column("kod_makor_phone_no")]
    public int? KodMakorPhoneNo { get; set; }
}

[Table("bthol_SMS_2_send_list_delta", Schema = "dbo")]
public class BtholSms2SendListDelta
{
    [Column("kod_msg_project")]
    public int KodMsgProject { get; set; }

    [Column("kod_sug_msg")]
    public int KodSugMsg { get; set; }

    [Column("kod_sug_msg_rn")]
    public byte KodSugMsgRn { get; set; }

    [Column("kod_mosad")]
    public int KodMosad { get; set; }

    [Column("kod_language")]
    public byte KodLanguage { get; set; }

    [Column("msg_txt")]
    [MaxLength(1000)]
    public string MsgTxt { get; set; } = null!;

    [Column("mispar_kabala")]
    [MaxLength(20)]
    public string MisparKabala { get; set; } = null!;

    [Column("taarich_lechishuv")]
    public DateTime TaarichLechishuv { get; set; }

    [Column("teudat_zehut_src")]
    [MaxLength(20)]
    public string TeudatZehutSrc { get; set; } = null!;

    [Column("phone_num_male")]
    [MaxLength(20)]
    public string PhoneNumMale { get; set; } = null!;

    [Column("id")]
    [MaxLength(24)]
    public string Id { get; set; } = null!;

    [Column("password")]
    [MaxLength(33)]
    public string Password { get; set; } = null!;

    [Column("sw_test")]
    public int SwTest { get; set; }

    [Column("kod_machlaka")]
    public int KodMachlaka { get; set; }

    [Column("sw_started")]
    public bool SwStarted { get; set; }

    [Column("date_created")]
    public DateTime DateCreated { get; set; }

    [Column("date_started")]
    public DateTime? DateStarted { get; set; }

    [Column("kod_makor_phone_no")]
    public int? KodMakorPhoneNo { get; set; }
}

[Table("bthol_SMS_2_send_list_delta_deleted", Schema = "dbo")]
public class BtholSms2SendListDeltaDeleted
{
    [Column("kod_msg_project")]
    public int KodMsgProject { get; set; }

    [Column("kod_sug_msg")]
    public int KodSugMsg { get; set; }

    [Column("kod_sug_msg_rn")]
    public byte? KodSugMsgRn { get; set; }

    [Column("kod_mosad")]
    public int KodMosad { get; set; }

    [Column("kod_language")]
    public byte KodLanguage { get; set; }

    [Column("msg_txt")]
    [MaxLength(1000)]
    public string MsgTxt { get; set; } = null!;

    [Column("mispar_kabala")]
    [MaxLength(20)]
    public string MisparKabala { get; set; } = null!;

    [Column("taarich_lechishuv")]
    public DateTime TaarichLechishuv { get; set; }

    [Column("teudat_zehut_src")]
    [MaxLength(20)]
    public string TeudatZehutSrc { get; set; } = null!;

    [Column("phone_num_male")]
    [MaxLength(20)]
    public string PhoneNumMale { get; set; } = null!;

    [Column("id")]
    [MaxLength(24)]
    public string Id { get; set; } = null!;

    [Column("password")]
    [MaxLength(33)]
    public string Password { get; set; } = null!;

    [Column("sw_test")]
    public int SwTest { get; set; }

    [Column("kod_machlaka")]
    public int KodMachlaka { get; set; }

    [Column("sw_started")]
    public bool SwStarted { get; set; }

    [Column("date_created")]
    public DateTime DateCreated { get; set; }

    [Column("date_started")]
    public DateTime? DateStarted { get; set; }

    [Column("date_deleted")]
    public DateTime? DateDeleted { get; set; }

    [Column("sw_sent")]
    public bool? SwSent { get; set; }

    [Column("row_id")]
    public long? RowId { get; set; }

    [Column("kod_makor_phone_no")]
    public int? KodMakorPhoneNo { get; set; }
}

[Table("ddl_log", Schema = "dbo")]
public class DdlLog
{
    [Column("PostTime")]
    public DateTime? PostTime { get; set; }

    [Column("sys_user")]
    [MaxLength(100)]
    public string? SysUser { get; set; }

    [Column("DB_User")]
    [MaxLength(100)]
    public string? DbUser { get; set; }

    [Column("OriginalUserName")]
    [MaxLength(100)]
    public string? OriginalUserName { get; set; }

    [Column("Host")]
    [MaxLength(100)]
    public string? Host { get; set; }

    [Column("ApplicationName")]
    [MaxLength(400)]
    public string? ApplicationName { get; set; }

    [Column("Event")]
    [MaxLength(100)]
    public string? Event { get; set; }

    [Column("ObjectName")]
    [MaxLength(100)]
    public string? ObjectName { get; set; }

    [Column("TSQL")]
    [MaxLength(2000)]
    public string? Tsql { get; set; }

    [Column("EventData")]
    public string? EventData { get; set; }
}

[Table("Sms_AuditLog", Schema = "dbo")]
public class SmsAuditLog
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("AuditLogId")]
    public long AuditLogId { get; set; }

    [Column("EntityName")]
    [MaxLength(100)]
    public string EntityName { get; set; } = null!;

    [Column("EntityId")]
    public int EntityId { get; set; }

    [Column("ActionType")]
    [MaxLength(50)]
    public string ActionType { get; set; } = null!;

    [Column("UserId")]
    public int? UserId { get; set; }

    [Column("OldValueJson")]
    public string? OldValueJson { get; set; }

    [Column("NewValueJson")]
    public string? NewValueJson { get; set; }

    [Column("DiffJson")]
    public string? DiffJson { get; set; }

    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("UpdatedAt")]
    public DateTime? UpdatedAt { get; set; }
}

[Table("sms_category", Schema = "dbo")]
public class SmsCategory
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("category_id")]
    public int CategoryId { get; set; }

    [Column("category_name")]
    [MaxLength(50)]
    public string CategoryName { get; set; } = null!;

    [Column("project_id")]
    public int ProjectId { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("is_editable")]
    public bool IsEditable { get; set; } = true;

    [Column("create_date")]
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;

    [Column("update_date")]
    public DateTime? UpdateDate { get; set; }
}

[Table("sms_project", Schema = "dbo")]
public class SmsProject
{
    [Key]
    [Column("project_id")]
    public int ProjectId { get; set; }

    [Column("project_name")]
    [MaxLength(50)]
    public string ProjectName { get; set; } = null!;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("create_date")]
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;

    [Column("update_date")]
    public DateTime? UpdateDate { get; set; }
}

[Table("sms_category_step", Schema = "dbo")]
public class SmsCategoryStep
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("category_step_id")]
    public int CategoryStepId { get; set; }

    [Column("category_id")]
    public int CategoryId { get; set; }

    [Column("template_id")]
    public int TemplateId { get; set; }

    [Column("dependentat_id")]
    public int? DependentatId { get; set; }

    [Column("delay_in_minutes")]
    public byte? DelayInMinutes { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("is_editable")]
    public bool IsEditable { get; set; } = true;

    [Column("create_date")]
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;

    [Column("update_date")]
    public DateTime? UpdateDate { get; set; }
}

[Table("sms_hospital", Schema = "dbo")]
public class SmsHospital
{
    [Key]
    [Column("hospital_id")]
    public int HospitalId { get; set; }

    [Column("hospital_type_id")]
    public int HospitalTypeId { get; set; }

    [Column("hospital_description")]
    [MaxLength(15)]
    public string HospitalDescription { get; set; } = null!;

    [Column("hospital_name")]
    [MaxLength(30)]
    public string HospitalName { get; set; } = null!;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("create_date")]
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;

    [Column("update_date")]
    public DateTime? UpdateDate { get; set; }
}

[Table("sms_placeholder_catalog", Schema = "dbo")]
public class SmsPlaceholderCatalog
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("placeholder_id")]
    public int PlaceholderId { get; set; }

    [Column("placeholder_name")]
    [MaxLength(100)]
    public string PlaceholderName { get; set; } = null!;

    [Column("display_name")]
    [MaxLength(100)]
    public string DisplayName { get; set; } = null!;

    [Column("source_type")]
    [MaxLength(50)]
    public string? SourceType { get; set; }

    [Column("value_source")]
    [MaxLength(200)]
    public string? ValueSource { get; set; }

    [Column("is_required")]
    public bool IsRequired { get; set; } = false;

    [Column("is_static")]
    public bool IsStatic { get; set; } = false;

    [Column("is_editable")]
    public bool IsEditable { get; set; } = true;

    [Column("default_value")]
    [MaxLength(500)]
    public string? DefaultValue { get; set; }

    [Column("create_date")]
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;

    [Column("update_date")]
    public DateTime? UpdateDate { get; set; }
}

[Table("sms_placeholder_scope", Schema = "dbo")]
public class SmsPlaceholderScope
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("placeholder_scope_id")]
    public int PlaceholderScopeId { get; set; }

    [Column("placeholder_id")]
    public int PlaceholderId { get; set; }

    [Column("project_id")]
    public int ProjectId { get; set; }

    [Column("category_id")]
    public int? CategoryId { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("create_date")]
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;

    [Column("update_date")]
    public DateTime? UpdateDate { get; set; }
}

[Table("sms_static", Schema = "dbo")]
public class SmsStatic
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("static_id")]
    public int StaticId { get; set; }

    [Column("placeholder_id")]
    public int PlaceholderId { get; set; }

    [Column("category_id")]
    public int? CategoryId { get; set; }

    [Column("hospital_id")]
    public int? HospitalId { get; set; }

    [Column("unit_id")]
    public int? UnitId { get; set; }

    [Column("source_code")]
    public int? SourceCode { get; set; }

    [Column("value_name")]
    [MaxLength(500)]
    public string? ValueName { get; set; }

    [Column("static_value")]
    [MaxLength(2000)]
    public string StaticValue { get; set; } = null!;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("create_date")]
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;

    [Column("update_date")]
    public DateTime? UpdateDate { get; set; }
}

[Table("sms_template", Schema = "dbo")]
public class SmsTemplate
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("template_id")]
    public int TemplateId { get; set; }

    [Column("template_description")]
    [MaxLength(300)]
    public string? TemplateCode { get; set; }

    [Column("template_name")]
    [MaxLength(100)]
    public string TemplateName { get; set; } = null!;

    [Column("category_id")]
    public int CategoryId { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("is_editable")]
    public bool IsEditable { get; set; } = true;

    [Column("version_number")]
    public int? VersionNumber { get; set; } = 1;

    [Column("create_date")]
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;

    [Column("update_date")]
    public DateTime? UpdateDate { get; set; }

    [Column("update_user", TypeName = "nvarchar(50)")]
    [MaxLength(50)]
    public string? UpdateUser { get; set; }
}

[NotMapped]
public class SmsTemplateHospitalUnit
{
    [Column("template_id")]
    public int TemplateId { get; set; }

    [Column("hospital_id")]
    public int HospitalId { get; set; }

    [Column("unit_id")]
    public int UnitId { get; set; }

    [Column("create_date")]
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;

    [Column("update_date")]
    public DateTime? UpdateDate { get; set; }
}

[Table("sms_reject_hospital_unit", Schema = "dbo")]
public class SmsRejectHospitalUnit
{
    [Column("template_id")]
    public int TemplateId { get; set; }

    [Column("hospital_id")]
    public int HospitalId { get; set; }

    [Column("unit_id")]
    public int UnitId { get; set; }

    [Column("create_date")]
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;

    [Column("update_date")]
    public DateTime? UpdateDate { get; set; }
}

[Table("sms_template_trigger", Schema = "dbo")]
public class SmsTemplateTrigger
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("tt_id")]
    public int TtId { get; set; }

    [Column("template_id")]
    public int TemplateId { get; set; }

    [Column("trigger_id")]
    public int TriggerId { get; set; }

    [Column("create_date")]
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;

    [Column("update_date")]
    public DateTime? UpdateDate { get; set; }

    [Column("update_user", TypeName = "nvarchar(50)")]
    [MaxLength(50)]
    public string? UpdateUser { get; set; }
}

[Table("sms_rules", Schema = "dbo")]
public class SmsRule
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("rule_id")]
    public int RuleId { get; set; }

    [Column("tt_id")]
    public int TtId { get; set; }

    [Column("depend_on_tt_id")]
    public int? DependOnTtId { get; set; }

    [Column("dependency_max_minutes")]
    public double? DependencyMaxMinutes { get; set; }

    [Column("max_time_tokef_in_minutes")]
    public double? MaxTimeTokefInMinutes { get; set; }

    [Column("is_constant")]
    public bool? IsConstant { get; set; }

    [Column("start_time_range")]
    [MaxLength(5)]
    public string? StartTimeRange { get; set; }

    [Column("end_time_range")]
    [MaxLength(5)]
    public string? EndTimeRange { get; set; }

    [Column("is_recurring")]
    public bool? IsRecurring { get; set; }

    [Column("recurring_interval_days")]
    public int? RecurringIntervalDays { get; set; }

    [Column("recurring_time_of_day")]
    [MaxLength(20)]
    public string? RecurringTimeOfDay { get; set; }

    [Column("recurring_stop_condition")]
    [MaxLength(50)]
    public string? RecurringStopCondition { get; set; }

    [Column("create_date")]
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;

    [Column("update_date")]
    public DateTime? UpdateDate { get; set; }

    [Column("update_user", TypeName = "nvarchar(50)")]
    [MaxLength(50)]
    public string? UpdateUser { get; set; }
}

[Table("sms_template_language", Schema = "dbo")]
public class SmsTemplateLanguage
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("template_language_id")]
    public int TemplateLanguageId { get; set; }

    [Column("template_id")]
    public int TemplateId { get; set; }

    [Column("language_code")]
    [MaxLength(10)]
    public string LanguageCode { get; set; } = null!;

    [Column("template_text")]
    [MaxLength(1000)]
    public string TemplateText { get; set; } = null!;

    [Column("is_default")]
    public bool IsDefault { get; set; } = false;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("create_date")]
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;

    [Column("update_date")]
    public DateTime? UpdateDate { get; set; }

    [Column("update_user", TypeName = "nvarchar(50)")]
    [MaxLength(50)]
    public string? UpdateUser { get; set; }
}

[NotMapped]
public class SmsTrigger
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("trigger_id")]
    public int TriggerId { get; set; }

    [Column("template_id")]
    public int TemplateId { get; set; }

    [Column("is_constant")]
    public bool IsConstant { get; set; } = false;

    [Column("send_type")]
    [MaxLength(50)]
    public string SendType { get; set; } = null!;

    [Column("send_time")]
    [MaxLength(50)]
    public string? SendTime { get; set; }

    [Column("trigger_event_code")]
    [MaxLength(50)]
    public string? TriggerEventCode { get; set; }

    [Column("is_recurring")]
    public bool IsRecurring { get; set; } = false;

    [Column("recurring_interval_days")]
    public int? RecurringIntervalDays { get; set; } = 1;

    [Column("recurring_time_of_day")]
    [MaxLength(20)]
    public string? RecurringTimeOfDay { get; set; } = "07:00";

    [Column("recurring_stop_condition")]
    [MaxLength(50)]
    public string? RecurringStopCondition { get; set; } = "discharged";

    [Column("onetime_fallback_enabled")]
    public bool OnetimeFallbackEnabled { get; set; } = false;

    [Column("onetime_fallback_time")]
    [MaxLength(20)]
    public string? OnetimeFallbackTime { get; set; } = "17:00";

    [Column("create_date")]
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;

    [Column("update_date")]
    public DateTime? UpdateDate { get; set; }
}

[Table("sms_trigger_catalog", Schema = "dbo")]
public class SmsTriggerCatalog
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("trigger_id")]
    public int TriggerId { get; set; }

    [Column("trigger_code")]
    [MaxLength(50)]
    public string TriggerCode { get; set; } = null!;

    [Column("trigger_name")]
    [MaxLength(100)]
    public string TriggerName { get; set; } = null!;

    [Column("description")]
    [MaxLength(500)]
    public string? Description { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("create_date")]
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;

    [Column("update_date")]
    public DateTime? UpdateDate { get; set; }
}

[Table("sms_unit", Schema = "dbo")]
public class SmsUnit
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("sms_unit_id")]
    public int SmsUnitId { get; set; }

    [Column("hospital_id")]
    public int HospitalId { get; set; }

    [Column("unit_id")]
    public int UnitId { get; set; }

    [Column("unit_name")]
    [MaxLength(50)]
    public string UnitName { get; set; } = null!;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("create_date")]
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;

    [Column("update_date")]
    public DateTime? UpdateDate { get; set; }
}

[Table("sms_unit_category", Schema = "dbo")]
public class SmsUnitCategory
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("uc_id")]
    public int UcId { get; set; }

    [Column("sms_unit_id")]
    public int SmsUnitId { get; set; }

    [Column("category_id")]
    public int CategoryId { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("create_date")]
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;

    [Column("update_user")]
    [MaxLength(50)]
    public string? UpdateUser { get; set; }
}

public class SmsUnitCategoryView
{
    public int UcId { get; set; }
    public int? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public int? HospitalId { get; set; }
    public string? HospitalName { get; set; }
    public int SmsUnitId { get; set; }
    public int? UnitId { get; set; }
    public string? UnitName { get; set; }
    public bool IsActive { get; set; }
}

[NotMapped]
public class SmsUnitType
{
    [Key]
    [Column("unit_type_id")]
    public int UnitTypeId { get; set; }

    [Column("unit_type_name")]
    [MaxLength(50)]
    public string UnitTypeName { get; set; } = null!;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("create_date")]
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;

    [Column("update_date")]
    public DateTime? UpdateDate { get; set; }
}
