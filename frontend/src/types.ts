export type ApiLanguageCode = 'he' | 'en' | 'ar' | 'ru';

export interface Placeholder {
  placeholderId: number;
  placeholderName: string;
  displayName: string;
}

export interface TemplateVariant {
  languageCode: ApiLanguageCode;
  body: string;
  placeholders: string[];
}

export interface TemplateVersion {
  id: string;
  templateId: string;
  versionNumber: number;
  status: string;
  authorId: string;
  createdAt: string;
}

export interface Template {
  id: string;
  name: string;
  description: string;
  categoryId?: string | number;
  isActive: boolean;
  isEditable: boolean;
  versionNumber: number;
  currentVersionId: string;
  createdAt: string;
  updatedAt: string | null;
  versions: TemplateVersion[];
}

export interface TemplateDetails {
  id: string;
  name: string;
  description: string;
  isActive: boolean;
  bodies: Partial<Record<ApiLanguageCode, string>>;
}

export interface SaveTemplateRequest {
  name: string;
  description: string;
  bodies: Partial<Record<ApiLanguageCode, string>>;
  isActive: boolean;
}

export interface RenderResult {
  renderedBody: string;
  charCount: number;
  smsSegments: number;
  placeholdersFound: string[];
}

export interface RouteStep {
  id: string;
  routeId: string;
  orderNumber: number;
  templateId: string;
  delayInMinutes: number;
}

export interface Route {
  id: string;
  name: string;
  description: string;
  isActive: boolean;
  hospitalId: string;
  unitId?: string;
  createdAt: string;
  steps: Array<RouteStep> | null;
  routeType?: 'אשפוז' | 'מיון';
}
export interface Hospital {
  id: string;
  name: string;
  description?: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
  units?: Unit[];
}


export interface Unit {
  id: string;
  hospitalId: string;
  name: string;
  isActive: boolean;
  createdAt?: string;
  updatedAt?: string;
  kod_mosad?: number;
  kod_unit?: number;
}
export interface UnitParameterDto {
  allocationId: string;
  placeholderId: string;
  param: string;
  description: string;
  category: string;
  textValue?: string;
  createdAt?: string;
}

export interface UnitWithParametersDto {
  id: string;
  hospitalId: string;
  name: string;
  isActive: boolean;
  createdAt?: string;
  updatedAt?: string;
  kod_mosad?: number;
  kod_unit?: number;
  parameters: UnitParameterDto[];
}
