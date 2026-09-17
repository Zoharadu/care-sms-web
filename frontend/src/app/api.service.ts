import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { concat, forkJoin, map, Observable, of, switchMap, throwError, toArray } from 'rxjs';
import {
  ApiLanguageCode,
  Hospital,
  Placeholder,
  RenderResult,
  Route,
  SaveTemplateRequest,
  Template,
  TemplateDetails,
  UnitWithParametersDto,
} from '../types';
import { environment } from '../environments/environment';
import type {
  CreateTemplateTriggerRequest,
  CreateSmsRuleRequest,
  ReplaceRejectHospitalUnitsRequest,
  ReplaceRejectHospitalUnitsResponse,
  SmsRejectHospitalUnitRecord,
  SmsRuleHierarchyItem,
  SmsRuleResponse,
  SmsTriggerSettings,
  TemplateClinicalTrigger,
  UpdateSmsRuleRequest,
  UpdateTemplateTriggerRequest,
} from './features/routes/route.models';
import {
  MAX_RELATIVE_DELAY_MINUTES,
  MAX_VALIDITY_MINUTES,
  MIN_VALIDITY_MINUTES,
} from './features/routes/route.models';
import type { SmsProject } from './features/projects/project.models';
import type {
  SendSmsRequest,
  SendSmsResponse,
  SmsTestPhone,
} from './features/templates/template.models';
import {
  CreateSmsUnitCategoryRequest,
  HospitalUnit,
  SmsCategory,
  SmsStaticCatalogItem,
  SmsUnitCategory,
  SmsUnitCategoryDetails,
  SmsUnitCategoryWriteResponse,
  TriggerCatalogItem,
  UpdateSmsUnitCategoryStatusRequest,
} from './features/settings/settings.models';

interface SmsTemplateHttpModel {
  templateId: number;
  templateCode: string;
  templateName: string;
  categoryId: number;
  isActive: boolean;
  isEditable: boolean;
  versionNumber: number | null;
  createDate: string;
  updateDate: string | null;
  updateUser?: string | null;
}

interface SmsTemplateWriteModel {
  templateId: number;
  templateCode: string;
  templateName: string;
  categoryId: number;
  isActive: boolean;
  isEditable: boolean;
  versionNumber: number;
}

interface SmsTemplateLanguageHttpModel {
  templateLanguageId: number;
  templateId: number;
  languageCode: string;
  templateText: string;
  isDefault: boolean;
  isActive: boolean;
  updateUser?: string | null;
}

interface SmsCategoryHttpModel {
  categoryId: number;
  categoryName: string;
  projectId: number;
  categoryType: string;
  isActive: boolean;
  isEditable: boolean;
}

interface SmsPlaceholderHttpModel {
  placeholderId: number;
  placeholderName: string;
  displayName?: string;
  display_name?: string;
}

interface SmsHospitalHttpModel {
  hospitalId: number;
  hospitalTypeId: number;
  hospitalDescription: string;
  hospitalName: string;
  isActive: boolean;
  createDate: string;
  updateDate: string | null;
}

const RULE_TIME_PATTERN = /^(?:[01]\d|2[0-3]):[0-5]\d$/;

function hasInvalidSendWindow(
  request: Pick<CreateSmsRuleRequest, 'startTimeRange' | 'endTimeRange'>,
): boolean {
  const startSpecified = request.startTimeRange !== undefined;
  const endSpecified = request.endTimeRange !== undefined;
  if (!startSpecified && !endSpecified) {
    return false;
  }

  if (startSpecified !== endSpecified) {
    return true;
  }

  const startTime = request.startTimeRange;
  const endTime = request.endTimeRange;
  return !(
    (startTime === null && endTime === null)
    || (
      typeof startTime === 'string'
      && typeof endTime === 'string'
      && RULE_TIME_PATTERN.test(startTime)
      && RULE_TIME_PATTERN.test(endTime)
    )
  );
}

function hasInvalidRecurringStopCondition(value: string | null | undefined): boolean {
  return value !== undefined
    && value !== null
    && value !== 'discharged'
    && value !== 'never';
}


@Injectable({
  providedIn: 'root'
})
export class ApiService {

  constructor(private http: HttpClient) { }
  renderPreview(body: string, languageCode: string) {
    return this.sendRequest<RenderResult>('preview/render', { body, languageCode }, "POST");
  }
  getProjects(): Observable<SmsProject[]> {
    return this.sendRequest<SmsProject[]>('SmsProjects', {}, "GET");
  }
  getAllTemplates(): Observable<Template[]> {
    return this.sendRequest<SmsTemplateHttpModel[]>('SmsTemplates', {}, "GET").pipe(
      map((templates) => templates.map((template) => this.toTemplate(template))),
    );
  }
  getTemplatesByScope(
    projectId: number,
    categoryId: number,
    isActive?: boolean,
  ): Observable<Template[]> {
    const scopeError = this.validateProjectCategoryScope(projectId, categoryId);
    if (scopeError) {
      return throwError(() => new Error(scopeError));
    }

    const query = new URLSearchParams({
      projectId: String(projectId),
      categoryId: String(categoryId),
    });
    if (isActive != null) {
      query.set('isActive', String(isActive));
    }

    return this.sendRequest<SmsTemplateHttpModel[]>(
      `SmsTemplates?${query.toString()}`,
      {},
      "GET",
    ).pipe(
      map((templates) => templates.map((template) => this.toTemplate(template))),
    );
  }
  getTemplate(id: string): Observable<TemplateDetails> {
    const templateId = this.toNumericTemplateId(id);

    return forkJoin({
      template: this.sendRequest<SmsTemplateHttpModel>(`SmsTemplates/${encodeURIComponent(id)}`, {}, "GET"),
      languages: this.getTemplateLanguages(templateId),
    }).pipe(
      map(({ template, languages }) => this.toTemplateDetails(template, languages)),
    );
  }
  createTemplate(request: SaveTemplateRequest, categoryId: number): Observable<Template> {
    if (!Number.isInteger(categoryId) || categoryId <= 0) {
      return throwError(() => new Error('categoryId must be a positive integer.'));
    }

    return this.sendRequest<SmsTemplateHttpModel>(
      'SmsTemplates',
      this.toTemplateWriteModel(request, categoryId),
      "POST",
    ).pipe(
      switchMap((template) => {
        const languageRequests = this.createTemplateLanguageRequests(
          template.templateId,
          request.bodies,
        );

        return languageRequests.length
          ? concat(...languageRequests).pipe(toArray(), map(() => template))
          : of(template);
      }),
      map((template) => this.toTemplate(template)),
    );
  }

  updateTemplate(id: string, request: SaveTemplateRequest): Observable<unknown> {
    const templateId = this.toNumericTemplateId(id);
    const encodedId = encodeURIComponent(id);

    return forkJoin({
      template: this.sendRequest<SmsTemplateHttpModel>(`SmsTemplates/${encodedId}`, {}, "GET"),
      languages: this.getTemplateLanguages(templateId),
    }).pipe(
      switchMap(({ template, languages }) => {
        const updateRequests: Observable<unknown>[] = [
          this.sendRequest<unknown>(
            `SmsTemplates/${encodedId}`,
            this.toTemplateWriteModel(request, template.categoryId, template),
            "PUT",
          ),
          ...this.updateTemplateLanguageRequests(templateId, request.bodies, languages),
        ];

        return concat(...updateRequests).pipe(toArray());
      }),
    );
  }

  updateTemplateActive(template: Template, isActive: boolean): Observable<unknown> {
    const templateId = this.toNumericTemplateId(template.id);
    const categoryId = Number(template.categoryId);
    if (!Number.isInteger(categoryId) || categoryId <= 0) {
      return throwError(() => new Error('categoryId must be a positive integer.'));
    }

    return this.sendRequest<unknown>(
      `SmsTemplates/${encodeURIComponent(template.id)}`,
      {
        templateId,
        templateCode: template.description,
        templateName: template.name,
        categoryId,
        isActive,
        isEditable: template.isEditable,
        versionNumber: template.versionNumber + 1,
      } satisfies SmsTemplateWriteModel,
      "PUT",
    );
  }

  deleteTemplate(id: string): Observable<unknown> {
    const encodedId = encodeURIComponent(id);
    return this.sendRequest<unknown>(`SmsTemplates/${encodedId}`, undefined, "DELETE");
  }
  deleteRoute(id: string) {
    return this.sendRequest<{ success: boolean }>(`routes/${id}`, null, "DELETE");
  }
  sendSms(request: SendSmsRequest): Observable<SendSmsResponse> {
    return this.sendRequest<SendSmsResponse>('sms/send', request, "POST");
  }
  getTestPhones(): Observable<SmsTestPhone[]> {
    return this.sendRequest<SmsTestPhone[]>('sms/test-phones', {}, "GET");
  }
  getUnitsWithParameters(): Observable<UnitWithParametersDto[]> {
    return this.sendRequest<UnitWithParametersDto[]>('units', {}, "GET");
  }
  getHospitals(): Observable<Hospital[]> {
    return this.sendRequest<SmsHospitalHttpModel[]>('SmsHospitals', {}, "GET").pipe(
      map((hospitals) => hospitals.map((hospital) => ({
        id: String(hospital.hospitalId),
        name: hospital.hospitalName,
        description: hospital.hospitalDescription,
        isActive: hospital.isActive,
        createdAt: hospital.createDate,
        updatedAt: hospital.updateDate,
        units: [],
      }))),
    );
  }
  getHospitalUnits(hospitalId: number): Observable<HospitalUnit[]> {
    if (!Number.isInteger(hospitalId) || hospitalId <= 0) {
      return throwError(() => new Error('hospitalId must be a positive integer.'));
    }

    return this.sendRequest<HospitalUnit[]>(
      `Units?hospitalId=${encodeURIComponent(String(hospitalId))}`,
      {},
      "GET",
    );
  }
  getUnitsCatalog(): Observable<HospitalUnit[]> {
    return this.getHospitals().pipe(
      switchMap((hospitals) => {
        const hospitalIds = hospitals
          .map((hospital) => Number(hospital.id))
          .filter((hospitalId) => Number.isInteger(hospitalId) && hospitalId > 0);

        if (hospitalIds.length === 0) {
          return of([]);
        }

        return forkJoin(
          hospitalIds.map((hospitalId) => this.getHospitalUnits(hospitalId)),
        ).pipe(
          map((unitsByHospital) => unitsByHospital.flat()),
        );
      }),
    );
  }
  getRejectHospitalUnits(): Observable<SmsRejectHospitalUnitRecord[]> {
    return this.sendRequest<SmsRejectHospitalUnitRecord[]>('SmsRejectHospitalUnits', {}, "GET");
  }
  createRejectHospitalUnits(
    request: ReplaceRejectHospitalUnitsRequest,
  ): Observable<ReplaceRejectHospitalUnitsResponse> {
    if (!Number.isInteger(request.templateId) || request.templateId <= 0) {
      return throwError(() => new Error('templateId must be a positive integer.'));
    }

    return this.sendRequest<ReplaceRejectHospitalUnitsResponse>(
      'SmsRejectHospitalUnits/replace',
      request,
      "POST",
    );
  }
  replaceRejectHospitalUnits(
    request: ReplaceRejectHospitalUnitsRequest,
  ): Observable<ReplaceRejectHospitalUnitsResponse> {
    if (!Number.isInteger(request.templateId) || request.templateId <= 0) {
      return throwError(() => new Error('templateId must be a positive integer.'));
    }

    return this.sendRequest<ReplaceRejectHospitalUnitsResponse>(
      'SmsRejectHospitalUnits/replace',
      request,
      "PUT",
    );
  }
  getRulesByScope(projectId: number, categoryId: number): Observable<SmsRuleHierarchyItem[]> {
    const scopeError = this.validateProjectCategoryScope(projectId, categoryId);
    if (scopeError) {
      return throwError(() => new Error(scopeError));
    }

    const query = new URLSearchParams({
      projectId: String(projectId),
      categoryId: String(categoryId),
    });

    return this.sendRequest<SmsRuleHierarchyItem[]>(
      `SmsRules?${query.toString()}`,
      {},
      "GET",
    );
  }
  getRulesByTemplateScope(
    projectId: number,
    categoryId: number,
    templateId: number,
  ): Observable<SmsRuleHierarchyItem[]> {
    const scopeError = this.validateProjectCategoryScope(projectId, categoryId);
    if (scopeError) {
      return throwError(() => new Error(scopeError));
    }
    if (!Number.isInteger(templateId) || templateId <= 0) {
      return throwError(() => new Error('templateId must be a positive integer.'));
    }

    const query = new URLSearchParams({
      projectId: String(projectId),
      categoryId: String(categoryId),
    });

    return this.sendRequest<SmsRuleHierarchyItem[]>(
      `SmsRules/by-template/${encodeURIComponent(String(templateId))}?${query.toString()}`,
      {},
      "GET",
    );
  }
  createSmsRule(request: CreateSmsRuleRequest): Observable<SmsRuleResponse> {
    if (!Number.isInteger(request.ttId) || request.ttId <= 0) {
      return throwError(() => new Error('ttId must be a positive integer.'));
    }
    if (
      request.dependOnTtId != null
      && (!Number.isInteger(request.dependOnTtId) || request.dependOnTtId <= 0)
    ) {
      return throwError(() => new Error('dependOnTtId must be a positive integer.'));
    }

    const dependencyMaxMinutes = request.dependencyMaxMinutes;
    if (
      dependencyMaxMinutes != null
      && (
        !Number.isFinite(dependencyMaxMinutes)
        || dependencyMaxMinutes < 0
        || dependencyMaxMinutes > MAX_RELATIVE_DELAY_MINUTES
      )
    ) {
      return throwError(() => new Error(
        `dependencyMaxMinutes must be between 0 and ${MAX_RELATIVE_DELAY_MINUTES}.`,
      ));
    }

    const maxTimeTokefInMinutes = request.maxTimeTokefInMinutes;
    if (
      maxTimeTokefInMinutes != null
      && (
        !Number.isInteger(maxTimeTokefInMinutes)
        || maxTimeTokefInMinutes < MIN_VALIDITY_MINUTES
        || maxTimeTokefInMinutes > MAX_VALIDITY_MINUTES
      )
    ) {
      return throwError(() => new Error(
        `maxTimeTokefInMinutes must be an integer between ${MIN_VALIDITY_MINUTES} and ${MAX_VALIDITY_MINUTES}.`,
      ));
    }

    const recurringIntervalDays = request.recurringIntervalDays;
    if (
      recurringIntervalDays != null
      && (!Number.isInteger(recurringIntervalDays) || recurringIntervalDays <= 0)
    ) {
      return throwError(() => new Error('recurringIntervalDays must be a positive integer.'));
    }
    if ((request.onetimeFallbackTime?.length ?? 0) > 20) {
      return throwError(() => new Error('onetimeFallbackTime is too long.'));
    }
    if (hasInvalidRecurringStopCondition(request.recurringStopCondition)) {
      return throwError(() => new Error(
        'recurringStopCondition must be discharged, never, or null.',
      ));
    }
    if (hasInvalidSendWindow(request)) {
      return throwError(() => new Error(
        'startTimeRange and endTimeRange must both be valid HH:mm values or null.',
      ));
    }

    return this.sendRequest<SmsRuleResponse>('SmsRules', request, "POST");
  }
  updateSmsRule(ruleId: number, request: UpdateSmsRuleRequest): Observable<SmsRuleResponse> {
    if (!Number.isInteger(ruleId) || ruleId <= 0) {
      return throwError(() => new Error('ruleId must be a positive integer.'));
    }
    if (!Number.isInteger(request.ttId) || request.ttId <= 0) {
      return throwError(() => new Error('ttId must be a positive integer.'));
    }

    const dependencyMaxMinutes = request.dependencyMaxMinutes;
    if (
      dependencyMaxMinutes != null
      && (
        !Number.isFinite(dependencyMaxMinutes)
        || dependencyMaxMinutes < 0
        || dependencyMaxMinutes > MAX_RELATIVE_DELAY_MINUTES
      )
    ) {
      return throwError(() => new Error(
        `dependencyMaxMinutes must be between 0 and ${MAX_RELATIVE_DELAY_MINUTES}.`,
      ));
    }

    const maxTimeTokefInMinutes = request.maxTimeTokefInMinutes;
    if (
      maxTimeTokefInMinutes != null
      && (
        !Number.isInteger(maxTimeTokefInMinutes)
        || maxTimeTokefInMinutes < MIN_VALIDITY_MINUTES
        || maxTimeTokefInMinutes > MAX_VALIDITY_MINUTES
      )
    ) {
      return throwError(() => new Error(
        `maxTimeTokefInMinutes must be an integer between ${MIN_VALIDITY_MINUTES} and ${MAX_VALIDITY_MINUTES}.`,
      ));
    }
    if (hasInvalidSendWindow(request)) {
      return throwError(() => new Error(
        'startTimeRange and endTimeRange must both be valid HH:mm values or null.',
      ));
    }
    if (hasInvalidRecurringStopCondition(request.recurringStopCondition)) {
      return throwError(() => new Error(
        'recurringStopCondition must be discharged, never, or null.',
      ));
    }

    return this.sendRequest<SmsRuleResponse>(
      `SmsRules/${encodeURIComponent(String(ruleId))}`,
      request,
      "PUT",
    );
  }
  getTriggerCatalog(): Observable<TriggerCatalogItem[]> {
    return this.sendRequest<TriggerCatalogItem[]>('SmsTriggers/catalog', {}, "GET");
  }
  getTriggers(): Observable<SmsTriggerSettings[]> {
    return this.sendRequest<SmsTriggerSettings[]>('SmsTriggers', {}, "GET");
  }
  getTriggerByTemplate(templateId: number): Observable<TemplateClinicalTrigger[]> {
    if (!Number.isInteger(templateId) || templateId <= 0) {
      return throwError(() => new Error('templateId must be a positive integer.'));
    }

    return this.sendRequest<TemplateClinicalTrigger[]>(
      `SmsTriggers/by-template/${encodeURIComponent(String(templateId))}`,
      {},
      "GET",
    );
  }
  createTemplateTrigger(request: CreateTemplateTriggerRequest): Observable<unknown> {
    if (!Number.isInteger(request.templateId) || request.templateId <= 0) {
      return throwError(() => new Error('templateId must be a positive integer.'));
    }
    if (!Number.isInteger(request.triggerId) || request.triggerId <= 0) {
      return throwError(() => new Error('triggerId must be a positive integer.'));
    }

    return this.sendRequest<unknown>('SmsTriggers/template-trigger', request, "POST");
  }
  updateTemplateTrigger(
    templateId: number,
    request: UpdateTemplateTriggerRequest,
  ): Observable<void> {
    if (!Number.isInteger(templateId) || templateId <= 0) {
      return throwError(() => new Error('templateId must be a positive integer.'));
    }
    if (!Number.isInteger(request.triggerId) || request.triggerId <= 0) {
      return throwError(() => new Error('triggerId must be a positive integer.'));
    }

    return this.sendRequest<void>(
      `SmsTriggers/template-trigger/${encodeURIComponent(String(templateId))}`,
      request,
      "PUT",
    );
  }
  getPlaceholders(): Observable<Placeholder[]> {
    return this.sendRequest<SmsPlaceholderHttpModel[]>('placeholders', {}, "GET").pipe(
      map((placeholders) => placeholders.map((placeholder) => this.toPlaceholder(placeholder))),
    );
  }
  getPlaceholdersByScope(projectId: number, categoryId: number): Observable<Placeholder[]> {
    const scopeError = this.validateProjectCategoryScope(projectId, categoryId);
    if (scopeError) {
      return throwError(() => new Error(scopeError));
    }

    const query = new URLSearchParams({
      projectId: String(projectId),
      categoryId: String(categoryId),
    });

    return this.sendRequest<SmsPlaceholderHttpModel[]>(
      `placeholders?${query.toString()}`,
      {},
      "GET",
    ).pipe(
      map((placeholders) => placeholders.map((placeholder) => this.toPlaceholder(placeholder))),
    );
  }
  getCategories(): Observable<SmsCategory[]> {
    return this.sendRequest<SmsCategoryHttpModel[]>('SmsCategories', {}, "GET");
  }
  getCategory(categoryId: number): Observable<SmsCategory> {
    if (!Number.isInteger(categoryId) || categoryId <= 0) {
      return throwError(() => new Error('categoryId must be a positive integer.'));
    }

    return this.sendRequest<SmsCategoryHttpModel>(
      `SmsCategories/${encodeURIComponent(String(categoryId))}`,
      {},
      "GET",
    );
  }
  getCategoriesByProject(projectId: number): Observable<SmsCategory[]> {
    if (!Number.isInteger(projectId) || projectId <= 0) {
      return throwError(() => new Error('projectId must be a positive integer.'));
    }

    return this.sendRequest<SmsCategoryHttpModel[]>(
      `SmsCategories?projectId=${encodeURIComponent(String(projectId))}`,
      {},
      "GET",
    );
  }
  getSmsUnitCategories(): Observable<SmsUnitCategory[]> {
    return this.sendRequest<SmsUnitCategory[]>('SmsUnitCategories', {}, "GET");
  }
  getSmsUnitCategoryDetails(): Observable<SmsUnitCategoryDetails[]> {
    return this.sendRequest<SmsUnitCategoryDetails[]>(
      'SmsUnitCategories/details',
      {},
      "GET",
    );
  }
  createSmsUnitCategory(
    request: CreateSmsUnitCategoryRequest,
  ): Observable<SmsUnitCategoryWriteResponse> {
    const identifiers = [
      request.projectId,
      request.categoryId,
      request.hospitalId,
      request.smsUnitId,
    ];
    if (identifiers.some((identifier) => !Number.isInteger(identifier) || identifier <= 0)) {
      return throwError(() => new Error('All unit-category identifiers must be positive integers.'));
    }

    return this.sendRequest<SmsUnitCategoryWriteResponse>(
      'SmsUnitCategories',
      request,
      "POST",
    );
  }
  updateSmsUnitCategoryStatus(ucId: number, isActive: boolean): Observable<void> {
    if (!Number.isInteger(ucId) || ucId <= 0) {
      return throwError(() => new Error('ucId must be a positive integer.'));
    }

    return this.sendRequest<void>(
      `SmsUnitCategories/${encodeURIComponent(String(ucId))}`,
      { isActive } satisfies UpdateSmsUnitCategoryStatusRequest,
      "PATCH",
    );
  }
  getStaticValues(): Observable<SmsStaticCatalogItem[]> {
    return this.sendRequest<SmsStaticCatalogItem[]>('statics', {}, "GET");
  }
  getAllRoutes(): Observable<Route[]> {
    return this.sendRequest<Route[]>('routes', {}, "GET");
  }
  getRoute(id: string): Observable<Route> {
    return this.sendRequest<Route>(`routes/${id}`, {}, "GET");
  }
  saveRoute(route: Partial<Route>): Observable<{ success: boolean }> {
    return this.sendRequest<{ success: boolean }>('routes', route, "POST");
  }
  updateRoute(route: Partial<Route>): Observable<{ success: boolean }> {
    return this.sendRequest<{ success: boolean }>(`routes/${route.id}`, route, "PUT");
  }
  sendRequest<T>(url: string, body?: unknown, method: 'GET' | 'DELETE' | 'PUT' | 'POST' | 'PATCH' = 'POST'): Observable<T> {
    const requestBody = body === null ? {} : body;
    const fullUrl = this.buildApiUrl(url);
    if (method === 'GET') {
      return this.http.get<T>(fullUrl);
    }
    if (method === 'DELETE') {
      return this.http.delete<T>(fullUrl);
    }
    if (method === 'POST') {
      return this.http.post<T>(fullUrl, requestBody);
    }
    if (method === 'PUT') {
      return this.http.put<T>(fullUrl, requestBody);
    }
    if (method === 'PATCH') {
      return this.http.patch<T>(fullUrl, requestBody);
    } else {
      throw new Error('Unsupported HTTP method');
    }
  }

  private buildApiUrl(path: string): string {
    return `${environment.baseUrl.replace(/\/+$/, '')}/${path.replace(/^\/+/, '')}`;
  }

  private validateProjectCategoryScope(projectId: number, categoryId: number): string {
    if (!Number.isInteger(projectId) || projectId <= 0) {
      return 'projectId must be a positive integer.';
    }
    if (!Number.isInteger(categoryId) || categoryId <= 0) {
      return 'categoryId must be a positive integer.';
    }

    return '';
  }

  private toPlaceholder(placeholder: SmsPlaceholderHttpModel): Placeholder {
    return {
      placeholderId: placeholder.placeholderId,
      placeholderName: placeholder.placeholderName,
      displayName: placeholder.displayName ?? placeholder.display_name ?? '',
    };
  }

  private getTemplateLanguages(templateId: number): Observable<SmsTemplateLanguageHttpModel[]> {
    return this.sendRequest<SmsTemplateLanguageHttpModel[]>(
      `SmsTemplateLanguages?templateId=${templateId}`,
      {},
      "GET",
    );
  }

  private toTemplate(template: SmsTemplateHttpModel): Template {
    return {
      id: String(template.templateId),
      name: template.templateName,
      description: template.templateCode,
      categoryId: template.categoryId,
      isActive: template.isActive,
      isEditable: template.isEditable,
      versionNumber: template.versionNumber ?? 0,
      currentVersionId: String(template.versionNumber ?? ''),
      createdAt: template.createDate,
      updatedAt: template.updateDate,
      versions: [],
    };
  }

  private toTemplateDetails(
    template: SmsTemplateHttpModel,
    languages: SmsTemplateLanguageHttpModel[],
  ): TemplateDetails {
    const bodies: Partial<Record<ApiLanguageCode, string>> = {};

    for (const language of languages) {
      const languageCode = language.languageCode.toLowerCase();
      if (this.isApiLanguageCode(languageCode)) {
        bodies[languageCode] = language.templateText;
      }
    }

    return {
      id: String(template.templateId),
      name: template.templateName,
      description: template.templateCode,
      isActive: template.isActive,
      bodies,
    };
  }

  private toTemplateWriteModel(
    request: SaveTemplateRequest,
    categoryId: number,
    existing?: SmsTemplateHttpModel,
  ): SmsTemplateWriteModel {
    return {
      templateId: existing?.templateId ?? 0,
      templateCode: this.toTemplateCode(request.description, request.name, existing?.templateCode),
      templateName: request.name.trim(),
      categoryId,
      isActive: request.isActive,
      isEditable: existing?.isEditable ?? true,
      versionNumber: existing ? (existing.versionNumber ?? 0) + 1 : 1,
    };
  }

  private createTemplateLanguageRequests(
    templateId: number,
    bodies: SaveTemplateRequest['bodies'],
  ): Observable<unknown>[] {
    const entries = Object.entries(bodies).filter(
      (entry): entry is [ApiLanguageCode, string] => Boolean(entry[1]?.trim()),
    );
    const defaultLanguage = entries.some(([languageCode]) => languageCode === 'he')
      ? 'he'
      : entries[0]?.[0];

    return entries.map(([languageCode, templateText]) =>
      this.createTemplateLanguage(
        templateId,
        languageCode,
        templateText,
        languageCode === defaultLanguage,
      ),
    );
  }

  private updateTemplateLanguageRequests(
    templateId: number,
    bodies: SaveTemplateRequest['bodies'],
    existingLanguages: SmsTemplateLanguageHttpModel[],
  ): Observable<unknown>[] {
    const requests: Observable<unknown>[] = [];
    const existingByLanguage = new Map(
      existingLanguages.map((language) => [language.languageCode.toLowerCase(), language]),
    );

    for (const [languageCode, templateText] of Object.entries(bodies)) {
      if (!this.isApiLanguageCode(languageCode) || templateText === undefined) {
        continue;
      }

      const existing = existingByLanguage.get(languageCode);
      if (existing) {
        requests.push(
          this.sendRequest<unknown>(
            `SmsTemplateLanguages/${existing.templateLanguageId}`,
            {
              templateLanguageId: existing.templateLanguageId,
              templateId,
              languageCode,
              templateText,
              isDefault: existing.isDefault,
              isActive: existing.isActive,
            },
            "PUT",
          ),
        );
      } else if (templateText.trim()) {
        requests.push(
          this.createTemplateLanguage(
            templateId,
            languageCode,
            templateText,
            languageCode === 'he',
          ),
        );
      }
    }

    return requests;
  }

  private createTemplateLanguage(
    templateId: number,
    languageCode: ApiLanguageCode,
    templateText: string,
    isDefault: boolean,
  ): Observable<unknown> {
    return this.sendRequest<SmsTemplateLanguageHttpModel>(
      'SmsTemplateLanguages',
      {
        templateLanguageId: 0,
        templateId,
        languageCode,
        templateText,
        isDefault: true,
        isActive: false,
      },
      "POST",
    ).pipe(
      switchMap((created) =>
        this.sendRequest<unknown>(
          `SmsTemplateLanguages/${created.templateLanguageId}`,
          {
            templateLanguageId: created.templateLanguageId,
            templateId,
            languageCode,
            templateText,
            isDefault,
            isActive: true,
          },
          "PUT",
        ),
      ),
    );
  }

  private toNumericTemplateId(id: string): number {
    const templateId = Number(id);
    if (!Number.isInteger(templateId) || templateId <= 0) {
      throw new Error(`Invalid template id: ${id}`);
    }

    return templateId;
  }

  private toTemplateCode(description: string, name: string, existingCode?: string): string {
    const requestedCode = description.trim();
    if (requestedCode) {
      return requestedCode.slice(0, 50);
    }

    if (existingCode) {
      return existingCode;
    }

    const namePart = name.trim().replace(/\s+/g, '_').slice(0, 30);
    const uniquePart = Date.now().toString(36).toUpperCase();
    return `TPL_${namePart}_${uniquePart}`.slice(0, 50);
  }

  private isApiLanguageCode(languageCode: string): languageCode is ApiLanguageCode {
    return languageCode === 'he' || languageCode === 'en' || languageCode === 'ar' || languageCode === 'ru';
  }
}
