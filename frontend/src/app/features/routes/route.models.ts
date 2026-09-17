import { SmsCategory } from '../settings/settings.models';
import { SmsTemplate, SmsTemplateLanguage } from '../templates/template.models';

export type SendType = 'Event-Based' | 'Time-Based';
export type RecurringStopCondition = 'discharged' | 'never';

export const MIN_VALIDITY_MINUTES = 1;
export const MAX_VALIDITY_MINUTES = 60;
export const MAX_RELATIVE_DELAY_MINUTES = 60;

export interface AssociatedUnit {
  hospitalId: number;
  unitId: number;
  hospitalName: string;
  unitName: string;
}

export interface SmsCategoryStep {
  categoryStepId: number;
  categoryId: number;
  templateId: number;
  dependentAtId?: number;
  delayInMinutes: number;
  delayFromStepId?: number;
  delayFromStepMinutes: number | null;
  hasDependencyLimit: boolean;
  dependencyLimitStepId?: number;
  dependencyLimitMinutes: number | null;
  isActive: boolean;
  isEditable: boolean;
}

export interface SmsTrigger {
  triggerId: number;
  templateId: number;
  isConstant: boolean;
  sendType: SendType;
  sendTime?: string;
  triggerEventCode?: string;
  isRecurring: boolean;
  recurringIntervalDays: number | null;
  recurringTimeOfDay: string;
  sendWindowStartTime: string;
  sendWindowEndTime: string;
  recurringStopCondition: RecurringStopCondition;
  onetimeFallbackEnabled: boolean;
  onetimeFallbackTime: string;
}

export interface SmsTriggerSettings {
  ruleId: number | null;
  ttId: number;
  templateId: number;
  triggerId: number;
  triggerCode: string;
  triggerName: string;
  triggerDescription: string | null;
  triggerIsActive: boolean;
  dependOnTtId: number | null;
  dependencyMaxMinutes: number | null;
  maxTimeTokefInMinutes: number | null;
  isConstant: boolean | null;
  isRecurring: boolean | null;
  recurringIntervalDays: number | null;
  recurringTimeOfDay: string | null;
  startTimeRange?: string | null;
  endTimeRange?: string | null;
  recurringStopCondition: string | null;
  onetimeFallbackEnabled: boolean | null;
  onetimeFallbackTime: string | null;
  createDate: string | null;
  updateDate: string | null;
  updateUser?: string | null;
}

export interface TemplateClinicalTrigger {
  ttId: number;
  templateId: number;
  triggerId: number;
  triggerName: string;
  triggerCode: string;
  description: string | null;
  isActive: boolean;
  updateUser?: string | null;
}

export interface CreateTemplateTriggerRequest {
  templateId: number;
  triggerId: number;
}

export interface UpdateTemplateTriggerRequest {
  triggerId: number;
}

export interface RejectHospitalUnitItem {
  hospitalId: number;
  unitId: number;
}

export interface SmsRejectHospitalUnitRecord extends RejectHospitalUnitItem {
  templateId: number;
  createDate: string;
  updateDate: string | null;
}

export interface ReplaceRejectHospitalUnitsRequest {
  templateId: number;
  items: RejectHospitalUnitItem[];
}

export interface ReplaceRejectHospitalUnitsResponse {
  templateId: number;
  savedCount: number;
  items: RejectHospitalUnitItem[] | null;
}

export interface CreateSmsRuleRequest {
  ttId: number;
  dependOnTtId: number | null;
  dependencyMaxMinutes?: number | null;
  maxTimeTokefInMinutes: number | null;
  isConstant?: boolean | null;
  isRecurring?: boolean | null;
  recurringIntervalDays?: number | null;
  recurringTimeOfDay?: string | null;
  startTimeRange?: string | null;
  endTimeRange?: string | null;
  recurringStopCondition?: string | null;
  onetimeFallbackEnabled?: boolean | null;
  onetimeFallbackTime?: string | null;
}

export interface UpdateSmsRuleRequest {
  ttId: number;
  dependOnTtId?: number | null;
  dependencyMaxMinutes?: number | null;
  maxTimeTokefInMinutes?: number | null;
  isConstant?: boolean | null;
  isRecurring?: boolean | null;
  recurringIntervalDays?: number | null;
  recurringTimeOfDay?: string | null;
  startTimeRange?: string | null;
  endTimeRange?: string | null;
  recurringStopCondition?: string | null;
  onetimeFallbackEnabled?: boolean | null;
  onetimeFallbackTime?: string | null;
}

export interface SmsRuleResponse extends CreateSmsRuleRequest {
  ruleId: number;
  createDate: string;
  updateDate: string | null;
  updateUser?: string | null;
}

export interface SmsRuleHierarchyCategory {
  categoryId: number;
  categoryName: string;
  isActive: boolean;
  isEditable: boolean;
}

export interface SmsRuleHierarchyTemplate {
  templateId: number;
  templateCode: string;
  templateName: string;
  categoryId: number;
  isActive: boolean;
  isEditable: boolean;
  versionNumber: number;
  updateUser?: string | null;
}

export interface SmsRuleHierarchyTemplateTrigger {
  ttId: number;
  triggerId: number;
  triggerCode: string;
  triggerName: string;
  triggerDescription: string | null;
  triggerIsActive: boolean;
  createDate: string | null;
  updateDate: string | null;
  updateUser?: string | null;
}

export interface SmsRuleHierarchyRule {
  ruleId: number;
  ttId: number;
  dependOnTtId: number | null;
  dependencyMaxMinutes: number | null;
  maxTimeTokefInMinutes: number | null;
  isConstant: boolean | null;
  isRecurring: boolean | null;
  recurringIntervalDays: number | null;
  recurringTimeOfDay: string | null;
  startTimeRange?: string | null;
  endTimeRange?: string | null;
  recurringStopCondition: string | null;
  onetimeFallbackEnabled: boolean | null;
  onetimeFallbackTime: string | null;
  createDate: string | null;
  updateDate: string | null;
  updateUser?: string | null;
}

export interface SmsRuleHierarchyExcludedHospitalUnit {
  hospitalId: number;
  hospitalName: string | null;
  unitId: number;
  unitName: string | null;
}

export interface SmsRuleHierarchyItem {
  projectId: number;
  category: SmsRuleHierarchyCategory;
  template: SmsRuleHierarchyTemplate;
  templateTrigger: SmsRuleHierarchyTemplateTrigger;
  rule: SmsRuleHierarchyRule | null;
  excludedHospitalUnits: SmsRuleHierarchyExcludedHospitalUnit[];
}

export interface ClinicalRuleAggregate {
  id: number;
  template: SmsTemplate;
  category: SmsCategory;
  languages: SmsTemplateLanguage[];
  associatedUnits: AssociatedUnit[];
  step: SmsCategoryStep;
  trigger: SmsTrigger;
}
