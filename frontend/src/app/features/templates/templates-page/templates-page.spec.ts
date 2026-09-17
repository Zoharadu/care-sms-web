import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { of } from 'rxjs';

import { Template, TemplateDetails } from '../../../../types';
import { ApiService } from '../../../api.service';
import { TemplateClinicalTrigger } from '../../routes/route.models';
import { TemplatesPage } from './templates-page';

describe('TemplatesPage template trigger editing', () => {
  let fixture: ComponentFixture<TemplatesPage>;
  let component: TemplatesPage;
  let apiService: jasmine.SpyObj<ApiService>;

  const template: Template = {
    id: '10',
    name: 'Test template',
    description: 'TEST_TEMPLATE',
    categoryId: 3,
    isActive: true,
    isEditable: true,
    versionNumber: 1,
    currentVersionId: '1',
    createdAt: '2026-09-14T08:00:00Z',
    updatedAt: null,
    versions: [],
  };

  const details: TemplateDetails = {
    id: '10',
    name: 'Test template',
    description: 'TEST_TEMPLATE',
    isActive: true,
    bodies: { he: 'בדיקה' },
  };

  const currentTrigger: TemplateClinicalTrigger = {
    ttId: 21,
    templateId: 10,
    triggerId: 7,
    triggerName: 'Trigger 7',
    triggerCode: 'TRIGGER_7',
    description: 'אירוע קליני 7',
    isActive: true,
  };

  beforeEach(async () => {
    apiService = jasmine.createSpyObj<ApiService>('ApiService', [
      'getTemplate',
      'getTriggerByTemplate',
      'updateTemplate',
      'updateTemplateTrigger',
    ]);
    apiService.getTemplate.and.returnValue(of(details));
    apiService.getTriggerByTemplate.and.returnValue(of([currentTrigger]));
    apiService.updateTemplate.and.returnValue(of(undefined));
    apiService.updateTemplateTrigger.and.returnValue(of(undefined));

    const router = jasmine.createSpyObj<Router>('Router', [
      'getCurrentNavigation',
      'navigate',
    ]);
    router.getCurrentNavigation.and.returnValue(null);
    router.navigate.and.returnValue(Promise.resolve(true));

    await TestBed.configureTestingModule({
      imports: [TemplatesPage],
      providers: [
        { provide: ApiService, useValue: apiService },
        { provide: Router, useValue: router },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              queryParamMap: convertToParamMap({ projectId: '2', categoryId: '3' }),
              paramMap: convertToParamMap({}),
              routeConfig: { path: 'templates' },
            },
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(TemplatesPage);
    component = fixture.componentInstance;
  });

  it('loads the associated trigger and sends its replacement when the selection changes', () => {
    openTemplateMetadataEdit();

    expect(apiService.getTriggerByTemplate).toHaveBeenCalledOnceWith(10);
    expect(component.templateMetadataTriggerId).toBe(7);

    component.templateMetadataTriggerId = 8;
    component.saveTemplateMetadata();

    expect(apiService.updateTemplateTrigger).toHaveBeenCalledOnceWith(10, { triggerId: 8 });
  });

  it('does not send the trigger PUT when the selected trigger is unchanged', () => {
    openTemplateMetadataEdit();

    component.saveTemplateMetadata();

    expect(apiService.updateTemplate).toHaveBeenCalled();
    expect(apiService.updateTemplateTrigger).not.toHaveBeenCalled();
  });

  function openTemplateMetadataEdit(): void {
    (component as unknown as {
      openTemplateMetadataEdit: (editableTemplate: Template) => void;
    }).openTemplateMetadataEdit(template);
  }
});
