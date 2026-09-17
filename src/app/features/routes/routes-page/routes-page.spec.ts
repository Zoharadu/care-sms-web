import { Location } from '@angular/common';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { of } from 'rxjs';

import { ApiService } from '../../../api.service';
import { RoutesPage } from './routes-page';

describe('RoutesPage audience unit categories', () => {
  let fixture: ComponentFixture<RoutesPage>;
  let component: RoutesPage;
  let apiService: jasmine.SpyObj<ApiService>;

  const unit = {
    smsUnitId: 101,
    hospitalId: 20,
    unitId: 501,
    unitName: 'Test unit',
    isActive: true,
  };

  beforeEach(async () => {
    apiService = jasmine.createSpyObj<ApiService>('ApiService', [
      'createSmsUnitCategory',
      'createRejectHospitalUnits',
      'createSmsRule',
      'getCategory',
      'getHospitalUnits',
      'getHospitals',
      'getRulesByScope',
      'getRulesByTemplateScope',
      'getSmsUnitCategories',
      'getTemplatesByScope',
      'getTriggerCatalog',
      'replaceRejectHospitalUnits',
      'updateSmsRule',
      'updateSmsUnitCategoryStatus',
    ]);
    apiService.getRulesByScope.and.returnValue(of([]));
    apiService.getTemplatesByScope.and.returnValue(of([]));
    apiService.getCategory.and.returnValue(of({
      categoryId: 3,
      categoryName: 'Test category',
      projectId: 2,
      categoryType: 'Test project',
      isActive: true,
      isEditable: true,
    }));
    apiService.getTriggerCatalog.and.returnValue(of([]));
    apiService.getHospitals.and.returnValue(of([{
      id: '20',
      name: 'Test hospital',
      isActive: true,
      createdAt: '2026-09-09T08:00:00Z',
      updatedAt: null,
    }]));
    apiService.getHospitalUnits.and.returnValue(of([unit]));
    apiService.getSmsUnitCategories.and.returnValue(of([{
      ucId: 24,
      smsUnitId: 101,
      categoryId: 3,
      isActive: true,
      createDate: '2026-09-09T08:00:00Z',
    }]));
    apiService.createSmsUnitCategory.and.returnValue(of({
      ucId: 24,
      smsUnitId: 101,
      categoryId: 3,
      isActive: true,
    }));
    apiService.updateSmsUnitCategoryStatus.and.returnValue(of(undefined));
    apiService.createRejectHospitalUnits.and.callFake((request) => of({
      templateId: request.templateId,
      savedCount: request.items.length,
      items: request.items,
    }));
    apiService.replaceRejectHospitalUnits.and.callFake((request) => of({
      templateId: request.templateId,
      savedCount: request.items.length,
      items: request.items,
    }));
    apiService.createSmsRule.and.returnValue(of({} as any));
    apiService.updateSmsRule.and.returnValue(of({} as any));
    apiService.getRulesByTemplateScope.and.returnValue(of([]));

    await TestBed.configureTestingModule({
      imports: [RoutesPage],
      providers: [
        { provide: ApiService, useValue: apiService },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              queryParamMap: convertToParamMap({ projectId: '2', categoryId: '3' }),
              paramMap: convertToParamMap({}),
              routeConfig: { path: 'rules' },
            },
          },
        },
        {
          provide: Location,
          useValue: jasmine.createSpyObj<Location>('Location', [
            'go',
            'path',
            'replaceState',
          ]),
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(RoutesPage);
    component = fixture.componentInstance;
    const location = TestBed.inject(Location) as jasmine.SpyObj<Location>;
    location.path.and.returnValue('/rules');
  });

  it('restores active unit-category associations for the current category', () => {
    fixture.detectChanges();

    component.setSelectedAudienceHospital('20');

    expect(component.isAudienceUnitSelected(501)).toBeTrue();
    expect(component.audienceSelectedUnitCount).toBe(1);
  });

  it('keeps the audience units dropdown open for internal clicks and closes it outside', () => {
    fixture.detectChanges();
    const dropdown = fixture.nativeElement.querySelector(
      '.audience-units-dropdown',
    ) as HTMLDetailsElement;
    const options = dropdown.querySelector('.hospital-unit-options') as HTMLElement;

    dropdown.open = true;
    options.dispatchEvent(new MouseEvent('click', { bubbles: true }));
    expect(dropdown.open).toBeTrue();

    document.body.dispatchEvent(new MouseEvent('click', { bubbles: true }));
    expect(dropdown.open).toBeFalse();
  });

  it('creates a selected association and deactivates it when unselected', () => {
    apiService.getSmsUnitCategories.and.returnValue(of([]));
    fixture.detectChanges();
    component.setSelectedAudienceHospital('20');

    component.setAudienceUnitSelected(501, true);

    expect(apiService.createSmsUnitCategory).toHaveBeenCalledOnceWith({
      projectId: 2,
      categoryId: 3,
      hospitalId: 20,
      smsUnitId: 101,
    });
    expect(component.isAudienceUnitSelected(501)).toBeTrue();

    component.setAudienceUnitSelected(501, false);

    expect(apiService.updateSmsUnitCategoryStatus).toHaveBeenCalledOnceWith(24, false);
    expect(component.isAudienceUnitSelected(501)).toBeFalse();
  });

  it('saves changed exclusions without updating a complete untouched rule 4', () => {
    fixture.detectChanges();
    const rule = configuredRuleAggregate();
    rule.associatedUnits = [{
      hospitalId: 20,
      hospitalName: 'Test hospital',
      unitId: 501,
      unitName: 'Test unit',
    }];
    configureRuleForSave(rule, '');
    component.audienceSelections.clear();
    component.toggleValiditySection(rule);
    component.toggleRelativeDelay(rule);
    component.toggleRecurringSection(rule);
    component.toggleSendWindowSection(rule);

    component.saveSelectedRule();

    expect(apiService.createRejectHospitalUnits).toHaveBeenCalledOnceWith({
      templateId: 10,
      items: [{ hospitalId: 20, unitId: 501 }],
    });
    expect(apiService.updateSmsRule).not.toHaveBeenCalled();
    expect(component.ruleSaveError).toBe('');
  });

  it('requires rule 3 after its value was touched, but explicit clear allows other changes to save', () => {
    fixture.detectChanges();
    const draft = configuredRuleAggregate();
    draft.associatedUnits = [{
      hospitalId: 20,
      hospitalName: 'Test hospital',
      unitId: 501,
      unitName: 'Test unit',
    }];
    const rule = configureRuleForSave(draft, '');
    component.audienceSelections.clear();
    component.setValidityMinutes(rule, null);

    component.saveSelectedRule();

    expect(component.ruleSaveError).toContain('בין 1 ל־60');
    expect(apiService.createRejectHospitalUnits).not.toHaveBeenCalled();

    component.clearValidityRule(rule);
    component.saveSelectedRule();

    expect(apiService.createRejectHospitalUnits).toHaveBeenCalled();
  });

  it('accepts only whole validity minutes between 1 and 60', () => {
    fixture.detectChanges();
    const rule = configureRuleForSave(configuredRuleAggregate(), '');
    component.audienceSelections.clear();

    for (const invalidValue of [0, -1, 1.5, 61]) {
      component.setValidityMinutes(rule, invalidValue);
      component.saveSelectedRule();

      expect(component.ruleSaveError).toContain('בין 1 ל־60');
      expect(apiService.updateSmsRule).not.toHaveBeenCalled();
    }

    component.setValidityMinutes(rule, 60);
    component.saveSelectedRule();

    expect(apiService.updateSmsRule).toHaveBeenCalledOnceWith(31, {
      ttId: 100,
      maxTimeTokefInMinutes: 60,
    });
  });

  it('always requires both rule 4 parameters before saving the drawer', () => {
    fixture.detectChanges();
    const rule = configureRuleForSave(configuredRuleAggregate(), '');
    component.audienceSelections.clear();
    rule.step.delayFromStepId = undefined;

    component.saveSelectedRule();

    expect(component.ruleSaveError).toContain('כלל 4');
    expect(apiService.updateSmsRule).not.toHaveBeenCalled();
  });

  it('allows at most 60 delay minutes in rule 4', () => {
    fixture.detectChanges();
    const rule = configureRuleForSave(configuredRuleAggregate(), '');
    component.audienceSelections.clear();
    component.setRelativeDelayMinutes(rule, 61);

    component.saveSelectedRule();

    expect(component.ruleSaveError).toContain('בין 0 ל־60');
    expect(apiService.updateSmsRule).not.toHaveBeenCalled();

    component.setRelativeDelayMinutes(rule, 60);
    component.saveSelectedRule();

    expect(apiService.updateSmsRule).toHaveBeenCalledOnceWith(31, {
      ttId: 100,
      dependOnTtId: 200,
      dependencyMaxMinutes: 60,
    });
  });

  it('requires valid rule 5 parameters and rejects impossible hours', () => {
    fixture.detectChanges();
    const rule = configureRuleForSave(configuredRuleAggregate(), '');
    component.audienceSelections.clear();
    component.setRecurringIntervalDays(rule, 2);
    component.setRuleTime(rule, 'recurringTimeOfDay', '2500');
    component.setRecurringStopCondition(rule, 'discharged');

    expect(rule.trigger.recurringTimeOfDay).toBe('2');

    component.saveSelectedRule();

    expect(component.ruleSaveError).toContain('00:00 ל־23:59');
    expect(apiService.updateSmsRule).not.toHaveBeenCalled();

    component.setRuleTime(rule, 'recurringTimeOfDay', '1200');
    component.saveSelectedRule();

    expect(apiService.updateSmsRule).toHaveBeenCalledOnceWith(31, jasmine.objectContaining({
      ttId: 100,
      isRecurring: true,
      recurringIntervalDays: 2,
      recurringTimeOfDay: '12:00',
      recurringStopCondition: 'discharged',
    }));
  });

  it('sends never when rule 5 is configured without automatic stopping', () => {
    fixture.detectChanges();
    const rule = configureRuleForSave(configuredRuleAggregate(), '');
    component.audienceSelections.clear();
    component.setRecurringIntervalDays(rule, 5);
    component.setRuleTime(rule, 'recurringTimeOfDay', '2000');
    component.setRecurringStopCondition(rule, 'never');

    component.saveSelectedRule();

    expect(apiService.updateSmsRule).toHaveBeenCalledOnceWith(31, jasmine.objectContaining({
      ttId: 100,
      isRecurring: true,
      recurringIntervalDays: 5,
      recurringTimeOfDay: '20:00',
      recurringStopCondition: 'never',
    }));
  });

  it('requires both rule 6 times after a field was touched, while clear removes the validation', () => {
    fixture.detectChanges();
    const draft = configuredRuleAggregate();
    draft.associatedUnits = [{
      hospitalId: 20,
      hospitalName: 'Test hospital',
      unitId: 501,
      unitName: 'Test unit',
    }];
    const rule = configureRuleForSave(draft, '');
    component.audienceSelections.clear();
    component.setRuleTime(rule, 'sendWindowStartTime', '0800');

    component.saveSelectedRule();

    expect(component.ruleSaveError).toContain('כלל 6');
    expect(apiService.createRejectHospitalUnits).not.toHaveBeenCalled();

    component.clearSendWindowRule(rule);
    component.saveSelectedRule();

    expect(apiService.createRejectHospitalUnits).toHaveBeenCalled();
  });

  it('sends both rule 6 times in the existing SmsRules update request', () => {
    fixture.detectChanges();
    const rule = configureRuleForSave(configuredRuleAggregate(), '');
    component.audienceSelections.clear();
    component.setRuleTime(rule, 'sendWindowStartTime', '0800');
    component.setRuleTime(rule, 'sendWindowEndTime', '1700');

    component.saveSelectedRule();

    expect(apiService.updateSmsRule).toHaveBeenCalledOnceWith(31, {
      ttId: 100,
      startTimeRange: '08:00',
      endTimeRange: '17:00',
    });
  });

  it('creates an SmsRules record when rule 6 is the only configured rule', () => {
    fixture.detectChanges();
    const rule = configureRuleForSave(configuredRuleAggregate(), '');
    component.configuredTriggers = [{ ...configuredTrigger(), ruleId: null }];
    component.audienceSelections.clear();
    component.setRuleTime(rule, 'sendWindowStartTime', '0730');
    component.setRuleTime(rule, 'sendWindowEndTime', '1530');

    component.saveSelectedRule();

    expect(apiService.createSmsRule).toHaveBeenCalledOnceWith(jasmine.objectContaining({
      ttId: 100,
      startTimeRange: '07:30',
      endTimeRange: '15:30',
    }));
  });

  it('updates only the edited rule fields and does not resave unchanged exclusions', () => {
    fixture.detectChanges();
    const rule = configureRuleForSave(configuredRuleAggregate(), '');
    component.audienceSelections.clear();
    component.setValidityMinutes(rule, 45);

    component.saveSelectedRule();

    expect(apiService.createRejectHospitalUnits).not.toHaveBeenCalled();
    expect(apiService.replaceRejectHospitalUnits).not.toHaveBeenCalled();
    expect(apiService.updateSmsRule).toHaveBeenCalledOnceWith(31, {
      ttId: 100,
      maxTimeTokefInMinutes: 45,
    });
  });

  function configureRuleForSave(rule: any, exclusionSignature: string): any {
    component.clinicalRulesStore.replaceAll([rule]);
    component.selectedRuleId = rule.id;
    (component as any).ruleTemplateIds.set(rule.id, String(rule.template.templateId));
    (component as any).savedHospitalExclusionSignatures.set(rule.id, exclusionSignature);
    component.configuredTriggers = [configuredTrigger()];
    return component.clinicalRulesStore.findById(rule.id)!;
  }

  function configuredRuleAggregate(): any {
    return {
      id: 10,
      template: {
        templateId: 10,
        templateCode: 'TEST',
        templateName: 'Test template',
        categoryId: 3,
        isActive: true,
        isEditable: true,
        versionNumber: 1,
      },
      category: {
        categoryId: 3,
        categoryName: 'Test category',
        projectId: 2,
        categoryType: 'Test project',
        isActive: true,
        isEditable: true,
      },
      languages: [],
      associatedUnits: [],
      step: {
        categoryStepId: 0,
        categoryId: 3,
        templateId: 10,
        delayInMinutes: 0,
        delayFromStepId: 200,
        delayFromStepMinutes: 15,
        hasDependencyLimit: false,
        dependencyLimitMinutes: 0,
        isActive: true,
        isEditable: true,
      },
      trigger: {
        triggerId: 7,
        templateId: 10,
        isConstant: false,
        sendType: 'Event-Based',
        isRecurring: false,
        recurringIntervalDays: 1,
        recurringTimeOfDay: '',
        sendWindowStartTime: '',
        sendWindowEndTime: '',
        recurringStopCondition: 'never',
        onetimeFallbackEnabled: false,
        onetimeFallbackTime: '',
      },
    };
  }

  function configuredTrigger(): any {
    return {
      ruleId: 31,
      ttId: 100,
      templateId: 10,
      triggerId: 7,
      triggerCode: 'TEST_TRIGGER',
      triggerName: 'Test trigger',
      triggerDescription: null,
      triggerIsActive: true,
      dependOnTtId: 200,
      dependencyMaxMinutes: 15,
      maxTimeTokefInMinutes: null,
      isConstant: false,
      isRecurring: false,
      recurringIntervalDays: null,
      recurringTimeOfDay: null,
      recurringStopCondition: null,
      onetimeFallbackEnabled: false,
      onetimeFallbackTime: null,
      createDate: null,
      updateDate: null,
    };
  }
});
