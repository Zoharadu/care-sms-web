import { HttpClient } from '@angular/common/http';
import { of } from 'rxjs';

import { environment } from '../environments/environment';
import { ApiService } from './api.service';
import { CreateSmsRuleRequest, UpdateSmsRuleRequest } from './features/routes/route.models';

describe('ApiService audited write payloads', () => {
  let originalBaseUrl: string;
  let httpGet: jasmine.Spy;
  let httpPost: jasmine.Spy;
  let httpPut: jasmine.Spy;
  let httpPatch: jasmine.Spy;
  let apiService: ApiService;

  const templateResponse = {
    templateId: 11,
    templateCode: 'TEST_TEMPLATE',
    templateName: 'Test template',
    categoryId: 4,
    isActive: true,
    isEditable: true,
    versionNumber: 1,
    createDate: '2026-09-02T10:00:00Z',
    updateDate: null,
    updateUser: 'EXAMPLE\\server-user',
  };

  beforeEach(() => {
    originalBaseUrl = environment.baseUrl;
    environment.baseUrl = '/api/';
    httpGet = jasmine.createSpy('HttpClient.get');
    httpPost = jasmine.createSpy('HttpClient.post');
    httpPut = jasmine.createSpy('HttpClient.put');
    httpPatch = jasmine.createSpy('HttpClient.patch');
    apiService = new ApiService({
      get: httpGet,
      post: httpPost,
      put: httpPut,
      patch: httpPatch,
    } as unknown as HttpClient);
  });

  afterEach(() => {
    environment.baseUrl = originalBaseUrl;
  });

  it('omits updateUser from template and template-language create payloads', () => {
    httpPost.and.callFake((url: string) => url.endsWith('/SmsTemplates')
      ? of(templateResponse)
      : of({
          templateLanguageId: 21,
          templateId: 11,
          languageCode: 'he',
          templateText: 'שלום',
          isDefault: true,
          isActive: false,
          updateUser: 'EXAMPLE\\server-user',
        }));
    httpPut.and.returnValue(of({}));

    apiService.createTemplate({
      name: 'Test template',
      description: 'TEST_TEMPLATE',
      bodies: { he: 'שלום' },
      isActive: true,
    }, 4).subscribe();

    expect(httpPost).toHaveBeenCalledTimes(2);
    expect(httpPut).toHaveBeenCalledTimes(1);
    expectAllBodiesToOmitUpdateUser(httpPost.calls.allArgs());
    expectAllBodiesToOmitUpdateUser(httpPut.calls.allArgs());
  });

  it('omits updateUser from template and template-language update payloads', () => {
    httpGet.and.callFake((url: string) => url.includes('SmsTemplateLanguages')
      ? of([{
          templateLanguageId: 21,
          templateId: 11,
          languageCode: 'he',
          templateText: 'ישן',
          isDefault: true,
          isActive: true,
          updateUser: 'EXAMPLE\\server-user',
        }])
      : of(templateResponse));
    httpPut.and.returnValue(of({}));

    apiService.updateTemplate('11', {
      name: 'Updated template',
      description: 'TEST_TEMPLATE',
      bodies: { he: 'חדש' },
      isActive: true,
    }).subscribe();

    expect(httpPut).toHaveBeenCalledTimes(2);
    expectAllBodiesToOmitUpdateUser(httpPut.calls.allArgs());
  });

  it('omits updateUser from rule create and update payloads', () => {
    httpPost.and.returnValue(of({ ruleId: 31 }));
    httpPut.and.returnValue(of({ ruleId: 31 }));
    const createRequest: CreateSmsRuleRequest = {
      ttId: 21,
      dependOnTtId: null,
      maxTimeTokefInMinutes: 30,
    };
    const updateRequest: UpdateSmsRuleRequest = {
      ttId: 21,
      dependOnTtId: null,
      maxTimeTokefInMinutes: 45,
    };

    apiService.createSmsRule(createRequest).subscribe();
    apiService.updateSmsRule(31, updateRequest).subscribe();

    expectAllBodiesToOmitUpdateUser(httpPost.calls.allArgs());
    expectAllBodiesToOmitUpdateUser(httpPut.calls.allArgs());
  });

  it('rejects unrealistic validity minutes before sending a rule request', () => {
    const invalidValues = [0, -1, 1.5, 61];

    for (const maxTimeTokefInMinutes of invalidValues) {
      let receivedError: unknown;
      apiService.updateSmsRule(31, {
        ttId: 21,
        maxTimeTokefInMinutes,
      }).subscribe({ error: (error) => receivedError = error });

      expect(receivedError).toEqual(jasmine.any(Error));
    }

    expect(httpPut).not.toHaveBeenCalled();
  });

  it('rejects rule 4 delays above 60 minutes before sending a request', () => {
    let createError: unknown;
    let updateError: unknown;

    apiService.createSmsRule({
      ttId: 21,
      dependOnTtId: 20,
      dependencyMaxMinutes: 61,
      maxTimeTokefInMinutes: null,
    }).subscribe({ error: (error) => createError = error });
    apiService.updateSmsRule(31, {
      ttId: 21,
      dependencyMaxMinutes: 61,
    }).subscribe({ error: (error) => updateError = error });

    expect(createError).toEqual(jasmine.any(Error));
    expect(updateError).toEqual(jasmine.any(Error));
    expect(httpPost).not.toHaveBeenCalled();
    expect(httpPut).not.toHaveBeenCalled();
  });

  it('sends both rule 6 times in the same SmsRules create and update requests', () => {
    httpPost.and.returnValue(of({ ruleId: 31 }));
    httpPut.and.returnValue(of({ ruleId: 31 }));

    apiService.createSmsRule({
      ttId: 21,
      dependOnTtId: null,
      maxTimeTokefInMinutes: null,
      startTimeRange: '08:00',
      endTimeRange: '17:00',
    }).subscribe();
    apiService.updateSmsRule(31, {
      ttId: 21,
      startTimeRange: '09:00',
      endTimeRange: '16:00',
    }).subscribe();

    expect(httpPost).toHaveBeenCalledWith('/api/SmsRules', jasmine.objectContaining({
      startTimeRange: '08:00',
      endTimeRange: '17:00',
    }));
    expect(httpPut).toHaveBeenCalledWith('/api/SmsRules/31', jasmine.objectContaining({
      startTimeRange: '09:00',
      endTimeRange: '16:00',
    }));
  });

  it('rejects an incomplete or invalid rule 6 time pair before sending a request', () => {
    let incompleteError: unknown;
    let invalidError: unknown;

    apiService.createSmsRule({
      ttId: 21,
      dependOnTtId: null,
      maxTimeTokefInMinutes: null,
      startTimeRange: '08:00',
    }).subscribe({ error: (error) => incompleteError = error });
    apiService.updateSmsRule(31, {
      ttId: 21,
      startTimeRange: '25:00',
      endTimeRange: '17:00',
    }).subscribe({ error: (error) => invalidError = error });

    expect(incompleteError).toEqual(jasmine.any(Error));
    expect(invalidError).toEqual(jasmine.any(Error));
    expect(httpPost).not.toHaveBeenCalled();
    expect(httpPut).not.toHaveBeenCalled();
  });

  it('allows only the two supported recurring stop conditions', () => {
    httpPut.and.returnValue(of({ ruleId: 31 }));
    let invalidError: unknown;

    apiService.updateSmsRule(31, {
      ttId: 21,
      recurringStopCondition: 'never',
    }).subscribe();
    apiService.updateSmsRule(31, {
      ttId: 21,
      recurringStopCondition: 'NEVER',
    }).subscribe({ error: (error) => invalidError = error });

    expect(httpPut).toHaveBeenCalledOnceWith('/api/SmsRules/31', {
      ttId: 21,
      recurringStopCondition: 'never',
    });
    expect(invalidError).toEqual(jasmine.any(Error));
  });

  it('omits updateUser from a template-trigger create payload', () => {
    httpPost.and.returnValue(of({ ttId: 21, updateUser: 'EXAMPLE\\server-user' }));

    apiService.createTemplateTrigger({ templateId: 11, triggerId: 7 }).subscribe();

    expectAllBodiesToOmitUpdateUser(httpPost.calls.allArgs());
  });

  it('updates a template trigger with the template id in the URL and only triggerId in the body', () => {
    httpPut.and.returnValue(of(undefined));

    apiService.updateTemplateTrigger(11, { triggerId: 8 }).subscribe();

    expect(httpPut).toHaveBeenCalledOnceWith(
      '/api/SmsTriggers/template-trigger/11',
      { triggerId: 8 },
    );
    expectAllBodiesToOmitUpdateUser(httpPut.calls.allArgs());
  });

  it('posts the complete hospital-unit exclusion set for a new template association', () => {
    const request = {
      templateId: 10,
      items: [
        { hospitalId: 20, unitId: 1549947 },
        { hospitalId: 20, unitId: 15500431 },
      ],
    };
    httpPost.and.returnValue(of({
      templateId: 10,
      savedCount: 2,
      items: request.items,
    }));

    apiService.createRejectHospitalUnits(request).subscribe();

    expect(httpPost).toHaveBeenCalledOnceWith(
      '/api/SmsRejectHospitalUnits/replace',
      request,
    );
  });

  it('puts the complete hospital-unit exclusion set when updating a saved association', () => {
    const request = {
      templateId: 10,
      items: [{ hospitalId: 20, unitId: 1549947 }],
    };
    httpPut.and.returnValue(of({
      templateId: 10,
      savedCount: 1,
      items: request.items,
    }));

    apiService.replaceRejectHospitalUnits(request).subscribe();

    expect(httpPut).toHaveBeenCalledOnceWith(
      '/api/SmsRejectHospitalUnits/replace',
      request,
    );
  });

  it('loads and merges the units catalog for every hospital', () => {
    const hospital20Unit = {
      hospitalId: 20,
      unitId: 101,
      unitType: 1,
      unitTypeName: 'מחלקה',
      unitName: 'פנימית א',
      isActive: true,
    };
    const hospital21Unit = {
      hospitalId: 21,
      unitId: 202,
      unitType: 2,
      unitTypeName: 'מכון',
      unitName: 'קרדיולוגיה',
      isActive: true,
    };

    httpGet.and.callFake((url: string) => {
      if (url.endsWith('/SmsHospitals')) {
        return of([
          {
            hospitalId: 20,
            hospitalTypeId: 1,
            hospitalDescription: 'כלליים',
            hospitalName: 'בילינסון',
            isActive: true,
            createDate: '2026-07-23T14:03:08.867',
            updateDate: null,
          },
          {
            hospitalId: 21,
            hospitalTypeId: 1,
            hospitalDescription: 'כלליים',
            hospitalName: 'כרמל',
            isActive: true,
            createDate: '2026-07-23T14:03:08.867',
            updateDate: null,
          },
        ]);
      }

      if (url.endsWith('/Units?hospitalId=20')) {
        return of([hospital20Unit]);
      }

      if (url.endsWith('/Units?hospitalId=21')) {
        return of([hospital21Unit]);
      }

      return of([]);
    });

    let result: unknown[] = [];
    apiService.getUnitsCatalog().subscribe((units) => {
      result = units;
    });

    expect(result).toEqual([hospital20Unit, hospital21Unit]);
    expect(httpGet).toHaveBeenCalledWith('/api/SmsHospitals');
    expect(httpGet).toHaveBeenCalledWith('/api/Units?hospitalId=20');
    expect(httpGet).toHaveBeenCalledWith('/api/Units?hospitalId=21');
    expect(
      httpGet.calls.allArgs().some(([url]) => String(url).includes('SmsUnitTypes')),
    ).toBeFalse();
  });

  it('loads unit-category records from the dedicated endpoint', () => {
    const response = [{
      ucId: 1,
      smsUnitId: 10,
      categoryId: 3,
      isActive: true,
      createDate: '2026-09-06T08:00:00',
    }];
    httpGet.and.returnValue(of(response));

    let result: unknown;
    apiService.getSmsUnitCategories().subscribe((records) => {
      result = records;
    });

    expect(result).toEqual(response);
    expect(httpGet).toHaveBeenCalledOnceWith('/api/SmsUnitCategories');
  });

  it('loads complete unit-category details from one endpoint', () => {
    const response = [{
      ucId: 2,
      projectName: 'מיון - מלר״ד',
      categoryName: 'מסלול מיון יולדות',
      hospitalName: 'בילינסון',
      unitName: 'מיון יולדות',
      isActive: true,
    }];
    httpGet.and.returnValue(of(response));

    let result: unknown;
    apiService.getSmsUnitCategoryDetails().subscribe((records) => {
      result = records;
    });

    expect(result).toEqual(response);
    expect(httpGet).toHaveBeenCalledOnceWith('/api/SmsUnitCategories/details');
  });

  it('creates a unit-category association with identifiers only', () => {
    const request = {
      projectId: 2,
      categoryId: 3,
      hospitalId: 20,
      smsUnitId: 101,
    };
    const response = {
      ucId: 24,
      smsUnitId: 101,
      categoryId: 3,
      isActive: true,
    };
    httpPost.and.returnValue(of(response));

    let result: unknown;
    apiService.createSmsUnitCategory(request).subscribe((association) => {
      result = association;
    });

    expect(result).toEqual(response);
    expect(httpPost).toHaveBeenCalledOnceWith('/api/SmsUnitCategories', request);
    expectAllBodiesToOmitUpdateUser(httpPost.calls.allArgs());
  });

  it('patches only isActive when updating a unit-category record', () => {
    httpPatch.and.returnValue(of(undefined));

    apiService.updateSmsUnitCategoryStatus(1, false).subscribe();

    expect(httpPatch).toHaveBeenCalledOnceWith(
      '/api/SmsUnitCategories/1',
      { isActive: false },
    );
    expectAllBodiesToOmitUpdateUser(httpPatch.calls.allArgs());
  });

  function expectAllBodiesToOmitUpdateUser(calls: readonly unknown[][]): void {
    for (const [, body] of calls) {
      expect(body).not.toBeNull();
      expect(typeof body).toBe('object');
      expect(Object.prototype.hasOwnProperty.call(body, 'updateUser')).toBeFalse();
    }
  }
});
