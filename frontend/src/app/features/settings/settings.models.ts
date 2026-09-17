export interface Hospital {
  hospitalTypeId: number;
  hospitalDescription: string;
  hospitalId: number;
  hospitalName: string;
  isActive: boolean;
  createDate?: string;
  updateDate?: string;
}

export interface HospitalUnit {
  smsUnitId?: number;
  hospitalId: number;
  unitId: number;
  unitType?: number | null;
  unitTypeName?: string | null;
  unitName: string;
  isActive: boolean;
  createDate?: string;
  updateDate?: string;
}

export interface SmsCategory {
  categoryId: number;
  categoryName: string;
  projectId: number;
  categoryType: string;
  isActive: boolean;
  isEditable: boolean;
}

export interface SmsUnitCategory {
  ucId: number;
  smsUnitId: number;
  categoryId: number;
  isActive: boolean;
  createDate: string;
}

export interface SmsUnitCategoryDetails {
  ucId: number;
  projectId: number | null;
  projectName: string | null;
  categoryId: number;
  categoryName: string | null;
  hospitalId: number | null;
  hospitalName: string | null;
  smsUnitId: number;
  unitId: number | null;
  unitName: string | null;
  isActive: boolean;
}

export interface CreateSmsUnitCategoryRequest {
  projectId: number;
  categoryId: number;
  hospitalId: number;
  smsUnitId: number;
}

export interface SmsUnitCategoryWriteResponse {
  ucId: number;
  smsUnitId: number;
  categoryId: number;
  isActive: boolean;
}

export interface UpdateSmsUnitCategoryStatusRequest {
  isActive: boolean;
}

export interface SmsStaticValue {
  paramDefaultId: number;
  placeholderId: number;
  hospitalId?: number;
  unitId?: number;
  defaultValue: string;
  isActive: boolean;
}

export interface SmsStaticCatalogItem {
  fieldName: string;
  staticValue: string;
}

export interface TriggerCatalogItem {
  triggerId: number;
  triggerCode: string;
  triggerName: string;
  description: string;
  isActive: boolean;
}

export interface SchemaTable {
  tableName: string;
  hebrewName: string;
  purpose: string;
  keys: string[];
  columns: string[];
}
