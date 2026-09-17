export type LanguageCode = 'he' | 'ar' | 'en' | 'ru';
export type PatientStatus = 'Admitted' | 'Transferred' | 'ER' | 'Discharged';

export interface TemplateLanguageOption {
  code: LanguageCode;
  label: string;
  dir: 'rtl' | 'ltr';
}

export interface SmsTemplate {
  templateId: number;
  templateCode: string;
  templateName: string;
  categoryId: number;
  isActive: boolean;
  isEditable: boolean;
  versionNumber: number;
  updateUser?: string | null;
}

export interface SmsTemplateLanguage {
  templateLanguageId: number;
  templateId: number;
  languageCode: LanguageCode;
  templateText: string;
  isDefault: boolean;
  isActive: boolean;
  updateUser?: string | null;
}

export interface Placeholder {
  placeholderId: number;
  placeholderName: string;
  displayName: string;
}

export interface SmsTestPhone {
  phoneNumber: string;
  name: string;
}

export interface SendSmsRequest {
  categoryId: number;
  templateId: number;
  languageId: string;
  message: string;
  phoneNumber: string;
}

export interface SendSmsResponse {
  success: boolean;
}

export interface PatientContext {
  patientId: string;
  patientName: string;
  languageCode: LanguageCode;
  hospitalId: number;
  locationUnitId: number;
  status: PatientStatus;
  hospitalName: string;
  wardName: string;
  doctorName: string;
  eventCode: string;
  eventObservedAt: string;
  simulatedAt: string;
}
