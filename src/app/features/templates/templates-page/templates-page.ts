import {
  AfterViewInit,
  Component,
  DestroyRef,
  ElementRef,
  OnInit,
  ViewChild,
  inject,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, Router } from '@angular/router';
import { forkJoin, of, switchMap } from 'rxjs';
import { SaveTemplateRequest, Template, TemplateDetails } from '../../../../types';
import { ApiService } from '../../../api.service';
import { isAuthenticationError } from '../../../authentication-feedback.service';
import { TemplateClinicalTrigger } from '../../routes/route.models';
import { TriggerCatalogItem } from '../../settings/settings.models';
import { PhonePreview } from '../phone-preview/phone-preview';
import { PlaceholderPicker } from '../placeholder-picker/placeholder-picker';
import { calculateSmsSegments } from '../sms-segments';
import {
  LanguageCode,
  Placeholder,
  SendSmsRequest,
  SmsTestPhone,
  TemplateLanguageOption,
} from '../template.models';
import { TestSmsPanel } from '../test-sms-panel/test-sms-panel';

interface TemplateNavigationState {
  createdTemplate?: Template;
  selectedTemplateId?: string;
  toastMessage?: string;
  toastTone?: 'success' | 'error';
}

@Component({
  selector: 'app-templates-page',
  standalone: true,
  imports: [FormsModule, PhonePreview, PlaceholderPicker, TestSmsPanel],
  templateUrl: './templates-page.html',
  styleUrl: './templates-page.scss',
})
export class TemplatesPage implements OnInit, AfterViewInit {
  @ViewChild('templateDeleteDialog')
  private templateDeleteDialog?: ElementRef<HTMLDialogElement>;

  @ViewChild('templateEditDialog')
  private templateEditDialog?: ElementRef<HTMLDialogElement>;

  @ViewChild('templateCreateDialog')
  private templateCreateDialog?: ElementRef<HTMLDialogElement>;

  @ViewChild('templateTextArea')
  private templateTextArea?: ElementRef<HTMLTextAreaElement>;

  private readonly destroyRef = inject(DestroyRef);
  private readonly apiService = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  placeholders: Placeholder[] = [];
  placeholdersLoading = false;
  placeholdersError = '';
  projectId?: number;
  categoryId?: number;
  readonly languageOptions: TemplateLanguageOption[] = [
    { code: 'he', label: 'עברית', dir: 'rtl' },
    { code: 'ar', label: 'ערבית', dir: 'rtl' },
    { code: 'en', label: 'אנגלית', dir: 'ltr' },
    { code: 'ru', label: 'רוסית', dir: 'ltr' },
  ];
  testPhones: SmsTestPhone[] = [];
  testPhonesLoading = false;
  testPhonesError = '';

  newTemplateName = '';
  newTemplateDescription = '';
  newTemplateTriggerId: number | null = null;
  newTemplateActive = true;
  newTemplateError = '';
  createTemplateSubmitting = false;
  triggerCatalog: TriggerCatalogItem[] = [];
  triggerCatalogLoading = false;
  triggerCatalogError = '';
  newlyCreatedTemplateId = '';
  toastMessage = '';
  toastTone: 'success' | 'error' = 'success';
  selectedLanguage: LanguageCode = 'he';
  templates: Template[] = [];
  templateFilter = '';
  selectedTemplateId = '';
  selectedTemplateDetails?: TemplateDetails;
  templatesLoading = false;
  templatesError = '';
  templateDetailsLoading = false;
  templateDetailsError = '';
  templateSaving = false;
  templateSaveError = '';
  templateDeleting = false;
  templateDeleteError = '';
  templatePendingDeletion?: Template;
  templatePendingEdit?: Template;
  templateMetadataDetails?: TemplateDetails;
  templateMetadataName = '';
  templateMetadataDescription = '';
  templateMetadataTriggerId: number | null = null;
  templateMetadataCurrentTrigger?: TemplateClinicalTrigger;
  templateMetadataLoading = false;
  templateMetadataSaving = false;
  templateMetadataError = '';
  testPhone = '';
  testStatus = '';
  testStatusTone: 'success' | 'error' = 'success';
  testSmsSending = false;
  private toastTimer?: ReturnType<typeof setTimeout>;
  private newTemplateBadgeTimer?: ReturnType<typeof setTimeout>;
  private templateMetadataLoadSequence = 0;
  private initialTemplateMetadataTriggerId: number | null = null;
  private viewInitialized = false;
  private templatesLoaded = false;
  private routeDialogOpened = false;
  private readonly isCreateRoute: boolean;
  private readonly routedEditTemplateId: string;
  private readonly restoredCreatedTemplate?: Template;
  private readonly restoredSelectedTemplateId: string;
  private readonly templateNameCollator = new Intl.Collator('he', {
    sensitivity: 'base',
    numeric: true,
  });
  private templateTextSelection?: {
    templateId: string;
    language: LanguageCode;
    start: number;
    end: number;
  };
  private routeScopeValid = true;

  ngOnInit(): void {
    if (!this.readRouteScope()) {
      this.placeholdersError = 'בחירת הפרויקט או הקטגוריה אינה תקינה. יש לחזור למסך בחירת הפרויקט.';
      this.templatesError = this.placeholdersError;
      this.templatesLoaded = true;
      return;
    }

    this.loadPlaceholders();
    this.loadTemplates();
    this.loadTriggerCatalog();
    this.loadTestPhones();
  }

  ngAfterViewInit(): void {
    this.viewInitialized = true;
    this.openDialogFromRoute();
  }

  private readRouteScope(): boolean {
    const queryParams = this.route.snapshot.queryParamMap;
    const rawProjectId = queryParams.get('projectId');
    const rawCategoryId = queryParams.get('categoryId');

    if (rawProjectId == null && rawCategoryId == null) {
      return true;
    }

    const projectId = Number(rawProjectId);
    const categoryId = Number(rawCategoryId);
    this.routeScopeValid = Number.isInteger(projectId)
      && projectId > 0
      && Number.isInteger(categoryId)
      && categoryId > 0;

    if (this.routeScopeValid) {
      this.projectId = projectId;
      this.categoryId = categoryId;
    }

    return this.routeScopeValid;
  }

  private loadPlaceholders(): void {
    this.placeholdersLoading = true;
    this.placeholdersError = '';

    const placeholdersRequest = this.projectId && this.categoryId
      ? this.apiService.getPlaceholdersByScope(this.projectId, this.categoryId)
      : this.apiService.getPlaceholders();

    placeholdersRequest.subscribe({
      next: (placeholders) => {
        this.placeholders = placeholders;
        this.placeholdersLoading = false;
      },
      error: (error) => {
        this.placeholders = [];
        this.placeholdersLoading = false;
        this.placeholdersError = 'לא ניתן לטעון את השדות הדינמיים מהשרת.';
        console.error('Failed to load placeholders for templates', error);
      },
    });
  }

  private loadTemplates(): void {
    this.templatesLoading = true;
    this.templatesError = '';

    const templatesRequest = this.projectId && this.categoryId
      ? this.apiService.getTemplatesByScope(this.projectId, this.categoryId)
      : this.apiService.getAllTemplates();

    templatesRequest.subscribe({
      next: (templates) => {
        const createdTemplate = this.restoredCreatedTemplate;
        const loadedTemplates = createdTemplate
          ? [createdTemplate, ...templates.filter((template) => template.id !== createdTemplate.id)]
          : templates;
        this.templates = this.sortTemplates(loadedTemplates);
        this.templatesLoading = false;
        this.templatesLoaded = true;

        if (createdTemplate) {
          this.showNewTemplateBadge(createdTemplate.id);
          this.selectedTemplateId = createdTemplate.id;
          this.selectedTemplateDetails = {
            id: createdTemplate.id,
            name: createdTemplate.name,
            description: createdTemplate.description,
            isActive: createdTemplate.isActive,
            bodies: {},
          };
        } else {
          const selectedTemplate =
            this.templates.find(
              (template) => template.id === this.restoredSelectedTemplateId,
            ) ?? this.templates[0];
          if (selectedTemplate) {
            this.selectTemplate(selectedTemplate.id);
          }
        }

        this.openDialogFromRoute();
      },
      error: (error) => {
        this.templatesLoading = false;
        this.templatesLoaded = true;
        this.templatesError = 'לא ניתן לטעון את התבניות מהשרת.';
        console.error('Failed to load templates', error);
        this.openDialogFromRoute();
      },
    });
  }

  private loadTriggerCatalog(): void {
    this.triggerCatalogLoading = true;
    this.triggerCatalogError = '';

    this.apiService.getTriggerCatalog().subscribe({
      next: (triggers) => {
        this.triggerCatalog = triggers
          .filter((trigger) => Boolean(trigger.description?.trim()))
          .map((trigger) => ({
            ...trigger,
            description: trigger.description.trim(),
          }));
        this.triggerCatalogLoading = false;
        if (this.triggerCatalog.length === 0) {
          this.triggerCatalogError = 'לא נמצאו אירועים קליניים עם תיאור.';
        }
      },
      error: (error) => {
        this.triggerCatalog = [];
        this.triggerCatalogLoading = false;
        this.triggerCatalogError = 'לא ניתן לטעון את רשימת האירועים הקליניים מהשרת.';
        console.error('Failed to load trigger catalog for template creation', error);
      },
    });
  }

  private loadTestPhones(): void {
    this.testPhonesLoading = true;
    this.testPhonesError = '';

    this.apiService.getTestPhones().subscribe({
      next: (phones) => {
        this.testPhones = phones;
        this.testPhonesLoading = false;

        const selectedPhoneStillExists = phones.some(
          (phone) => phone.phoneNumber === this.testPhone,
        );
        if (!selectedPhoneStillExists) {
          this.testPhone = phones[0]?.phoneNumber ?? '';
        }
      },
      error: (error) => {
        this.testPhones = [];
        this.testPhone = '';
        this.testPhonesLoading = false;
        this.testPhonesError = 'לא ניתן לטעון את רשימת נמעני הבדיקה מהשרת.';
        console.error('Failed to load SMS test phones', error);
      },
    });
  }

  selectTemplate(templateId: string): void {
    if (
      this.newlyCreatedTemplateId
      && templateId !== this.newlyCreatedTemplateId
    ) {
      this.dismissNewTemplateBadge();
    }

    this.templateTextSelection = undefined;
    this.selectedTemplateId = templateId;
    this.selectedTemplateDetails = undefined;
    this.templateDetailsLoading = true;
    this.templateDetailsError = '';
    this.templateSaveError = '';
    this.templateDeleteError = '';
    this.testStatus = '';

    this.apiService.getTemplate(templateId).subscribe({
      next: (details) => {
        if (this.selectedTemplateId !== templateId) {
          return;
        }
        this.selectedTemplateDetails = details;
        this.templateDetailsLoading = false;
      },
      error: (error) => {
        if (this.selectedTemplateId !== templateId) {
          return;
        }
        this.templateDetailsLoading = false;
        this.templateDetailsError = 'לא ניתן לטעון את תוכן התבנית מהשרת.';
        console.error('Failed to load template details', error);
      },
    });
  }
  get selectedTemplate(): Template | undefined {
    return this.templates.find((template) => template.id === this.selectedTemplateId);
  }

  get filteredTemplates(): Template[] {
    const filter = this.templateFilter.trim().toLocaleLowerCase('he');
    if (!filter) {
      return this.templates;
    }

    return this.templates.filter((template) =>
      template.name.toLocaleLowerCase('he').includes(filter)
      || (template.description ?? '').toLocaleLowerCase('he').includes(filter),
    );
  }

  get hasTemplateFilter(): boolean {
    return this.templateFilter.trim().length > 0;
  }

  get canCreateTemplate(): boolean {
    const triggerId = Number(this.newTemplateTriggerId);
    return !this.createTemplateSubmitting
      && this.newTemplateName.trim().length > 0
      && Number.isInteger(triggerId)
      && triggerId > 0;
  }

  get canSaveTemplateMetadata(): boolean {
    const triggerId = Number(this.templateMetadataTriggerId);
    return !this.templateMetadataLoading
      && !this.templateMetadataSaving
      && !this.triggerCatalogLoading
      && Boolean(this.templateMetadataDetails)
      && this.templateMetadataName.trim().length > 0
      && Number.isInteger(triggerId)
      && triggerId > 0;
  }

  isCurrentTemplateTriggerMissingFromCatalog(): boolean {
    const currentTriggerId = this.templateMetadataCurrentTrigger?.triggerId;
    return currentTriggerId != null
      && !this.triggerCatalog.some((trigger) => trigger.triggerId === currentTriggerId);
  }

  private sortTemplates(templates: Template[]): Template[] {
    return [...templates].sort((first, second) =>
      this.templateNameCollator.compare(first.name.trim(), second.name.trim()),
    );
  }

  constructor() {
    const routePath = this.route.snapshot.routeConfig?.path;
    const navigationState = this.router.getCurrentNavigation()?.extras
      .state as TemplateNavigationState | undefined;
    this.isCreateRoute = routePath === 'templates/create';
    this.routedEditTemplateId = this.route.snapshot.paramMap.get('templateId') ?? '';
    this.restoredCreatedTemplate = navigationState?.createdTemplate;
    this.restoredSelectedTemplateId = navigationState?.selectedTemplateId ?? '';

    if (navigationState?.toastMessage) {
      this.showToast(navigationState.toastMessage, navigationState.toastTone);
    }

    this.destroyRef.onDestroy(() => {
      if (this.toastTimer) {
        clearTimeout(this.toastTimer);
      }
      if (this.newTemplateBadgeTimer) {
        clearTimeout(this.newTemplateBadgeTimer);
      }
    });
  }

  private showNewTemplateBadge(templateId: string): void {
    this.dismissNewTemplateBadge();
    this.newlyCreatedTemplateId = templateId;
    this.newTemplateBadgeTimer = setTimeout(
      () => this.dismissNewTemplateBadge(),
      15_000,
    );
  }

  private dismissNewTemplateBadge(): void {
    this.newlyCreatedTemplateId = '';
    if (this.newTemplateBadgeTimer) {
      clearTimeout(this.newTemplateBadgeTimer);
      this.newTemplateBadgeTimer = undefined;
    }
  }

  private openDialogFromRoute(): void {
    if (!this.routeScopeValid || this.routeDialogOpened || !this.viewInitialized) {
      return;
    }

    if (this.isCreateRoute) {
      this.routeDialogOpened = true;
      this.newTemplateName = '';
      this.newTemplateDescription = '';
      this.newTemplateTriggerId = null;
      this.newTemplateActive = true;
      this.newTemplateError = '';
      this.templateCreateDialog?.nativeElement.showModal();
      return;
    }

    if (!this.routedEditTemplateId || !this.templatesLoaded) {
      return;
    }

    this.routeDialogOpened = true;
    const template = this.templates.find(
      (entry) => entry.id === this.routedEditTemplateId,
    );
    if (!template) {
      void this.navigateToTemplates({
        toastMessage: 'לא ניתן למצוא את התבנית לעריכה.',
        toastTone: 'error',
      });
      return;
    }

    this.openTemplateMetadataEdit(template);
  }

  private resetTemplateMetadataEdit(): void {
    this.templateMetadataLoadSequence += 1;
    this.templatePendingEdit = undefined;
    this.templateMetadataDetails = undefined;
    this.templateMetadataTriggerId = null;
    this.templateMetadataCurrentTrigger = undefined;
    this.initialTemplateMetadataTriggerId = null;
    this.templateMetadataLoading = false;
    this.templateMetadataError = '';
  }

  private navigateToTemplates(state: TemplateNavigationState): Promise<boolean> {
    return this.router.navigate(['/templates'], {
      queryParamsHandling: 'preserve',
      replaceUrl: true,
      state,
    });
  }

  openNewTemplateDialog(): void {
    if (
      this.createTemplateSubmitting ||
      this.templateMetadataSaving ||
      this.templateDeleting ||
      this.templateSaving
    ) {
      return;
    }

    void this.router.navigate(['/templates/create'], {
      queryParamsHandling: 'preserve',
      state: { selectedTemplateId: this.selectedTemplateId } satisfies TemplateNavigationState,
    });
  }

  cancelCreateTemplate(): void {
    if (this.createTemplateSubmitting) {
      return;
    }

    this.closeTemplateCreateDialog();
    this.newTemplateError = '';
    void this.navigateToTemplates({ selectedTemplateId: this.selectedTemplateId });
  }

  handleTemplateCreateCancel(event: Event): void {
    event.preventDefault();
    this.cancelCreateTemplate();
  }

  handleTemplateCreateDialogClick(event: MouseEvent): void {
    if (!this.wasDialogContentClicked(event)) {
      this.cancelCreateTemplate();
    }
  }

  createTemplate(): void {
    if (this.createTemplateSubmitting) {
      return;
    }

    const name = this.newTemplateName.trim();
    const description = this.newTemplateDescription.trim();
    const triggerId = Number(this.newTemplateTriggerId);

    if (!this.categoryId) {
      this.showCreateTemplateError('יש לבחור פרויקט וקטגוריה לפני יצירת תבנית.');
      return;
    }

    if (!name) {
      this.showCreateTemplateError('יש למלא שם לתבנית.');
      return;
    }

    if (!Number.isInteger(triggerId) || triggerId <= 0) {
      this.showCreateTemplateError('יש לבחור אירוע קליני מפעיל.');
      return;
    }

    const request: SaveTemplateRequest = {
      name,
      description,
      bodies: {},
      isActive: this.newTemplateActive,
    };

    this.newTemplateError = '';
    this.createTemplateSubmitting = true;

    this.apiService.createTemplate(request, this.categoryId).subscribe({
      next: (createdTemplate) => {
        this.apiService.createTemplateTrigger({
          templateId: Number(createdTemplate.id),
          triggerId,
        }).subscribe({
          next: () => {
            this.createTemplateSubmitting = false;
            this.closeTemplateCreateDialog();
            void this.navigateToTemplates({
              createdTemplate,
              selectedTemplateId: createdTemplate.id,
              toastMessage: 'התבנית נוצרה וקושרה לאירוע הקליני בהצלחה.',
            });
          },
          error: (error) => {
            this.createTemplateSubmitting = false;
            this.closeTemplateCreateDialog();
            void this.navigateToTemplates({
              createdTemplate,
              selectedTemplateId: createdTemplate.id,
              toastMessage: 'התבנית נוצרה, אך הקישור לאירוע הקליני נכשל.',
              toastTone: 'error',
            });
            console.error('Template was created but trigger association failed', error);
          },
        });
      },
      error: (error) => {
        this.createTemplateSubmitting = false;
        if (!isAuthenticationError(error)) {
          this.showCreateTemplateError('לא ניתן ליצור את התבנית. יש לנסות שוב.');
        }
        console.error('Failed to create template', error);
      },
    });
  }

  dismissToast(): void {
    this.toastMessage = '';
    if (this.toastTimer) {
      clearTimeout(this.toastTimer);
      this.toastTimer = undefined;
    }
  }

  private showCreateTemplateError(message: string): void {
    this.newTemplateError = message;
    this.showToast(message, 'error');
  }

  private showToast(message: string, tone: 'success' | 'error' = 'success'): void {
    this.dismissToast();
    this.toastMessage = message;
    this.toastTone = tone;
    this.toastTimer = setTimeout(() => this.dismissToast(), 4000);
  }

  setLanguage(language: LanguageCode): void {
    if (language !== this.selectedLanguage) {
      this.templateTextSelection = undefined;
    }
    this.selectedLanguage = language;
  }

  currentTemplateText(): string {
    return this.selectedTemplateDetails?.bodies[this.selectedLanguage] ?? '';
  }

  hasCurrentTemplateText(): boolean {
    return this.currentTemplateText().trim().length > 0;
  }

  setTemplateText(value: string): void {
    if (this.selectedTemplateDetails) {
      this.selectedTemplateDetails.bodies[this.selectedLanguage] = value;
    }
  }

  saveSelectedTemplate(): void {
    const details = this.selectedTemplateDetails;
    if (!details || this.templateSaving || this.templateDeleting) {
      return;
    }

    const request: SaveTemplateRequest = {
      name: details.name,
      description: details.description,
      bodies: { ...details.bodies },
      isActive: details.isActive,
    };

    this.templateSaving = true;
    this.templateSaveError = '';

    this.apiService.updateTemplate(details.id, request).subscribe({
      next: () => {
        this.templateSaving = false;
        this.showToast('השינויים בתבנית נשמרו בהצלחה.');
      },
      error: (error) => {
        this.templateSaving = false;
        if (!isAuthenticationError(error)) {
          this.templateSaveError = 'לא ניתן לשמור את השינויים בתבנית. יש לנסות שוב.';
        }
        console.error('Failed to update template', error);
      },
    });
  }

  requestTemplateMetadataEdit(template: Template): void {
    if (
      this.templateMetadataLoading ||
      this.templateMetadataSaving ||
      this.templateDeleting ||
      this.templateSaving
    ) {
      return;
    }

    void this.router.navigate(['/templates/edit', template.id], {
      queryParamsHandling: 'preserve',
      state: { selectedTemplateId: this.selectedTemplateId } satisfies TemplateNavigationState,
    });
  }

  private openTemplateMetadataEdit(template: Template): void {
    const templateId = Number(template.id);
    this.templatePendingEdit = template;
    this.templateMetadataDetails = undefined;
    this.templateMetadataName = template.name;
    this.templateMetadataDescription = template.description;
    this.templateMetadataTriggerId = null;
    this.templateMetadataCurrentTrigger = undefined;
    this.initialTemplateMetadataTriggerId = null;
    this.templateMetadataLoading = true;
    this.templateMetadataError = '';
    this.templateEditDialog?.nativeElement.showModal();
    const loadSequence = ++this.templateMetadataLoadSequence;

    if (!Number.isInteger(templateId) || templateId <= 0) {
      this.templateMetadataLoading = false;
      this.templateMetadataError = 'מזהה התבנית אינו תקין.';
      return;
    }

    forkJoin({
      details: this.apiService.getTemplate(template.id),
      triggers: this.apiService.getTriggerByTemplate(templateId),
    }).subscribe({
      next: ({ details, triggers }) => {
        if (
          loadSequence !== this.templateMetadataLoadSequence ||
          this.templatePendingEdit?.id !== template.id
        ) {
          return;
        }

        this.templateMetadataDetails = details;
        this.templateMetadataName = details.name;
        this.templateMetadataDescription = details.description;
        this.templateMetadataLoading = false;

        if (triggers.length !== 1) {
          this.templateMetadataError = triggers.length === 0
            ? 'לא נמצא אירוע קליני המשויך לתבנית.'
            : 'נמצאו מספר אירועים קליניים המשויכים לתבנית. לא ניתן לערוך את השיוך.';
          return;
        }

        const currentTrigger = triggers[0];
        if (!Number.isInteger(currentTrigger.triggerId) || currentTrigger.triggerId <= 0) {
          this.templateMetadataError = 'מזהה האירוע הקליני המשויך לתבנית אינו תקין.';
          return;
        }

        this.templateMetadataCurrentTrigger = currentTrigger;
        this.templateMetadataTriggerId = currentTrigger.triggerId;
        this.initialTemplateMetadataTriggerId = currentTrigger.triggerId;
      },
      error: (error) => {
        if (
          loadSequence !== this.templateMetadataLoadSequence ||
          this.templatePendingEdit?.id !== template.id
        ) {
          return;
        }

        this.templateMetadataLoading = false;
        this.templateMetadataError = 'לא ניתן לטעון את פרטי התבנית. יש לנסות שוב.';
        console.error('Failed to load template metadata', error);
      },
    });
  }

  cancelTemplateMetadataEdit(): void {
    if (this.templateMetadataSaving) {
      return;
    }

    this.closeTemplateEditDialog();
    const editedTemplateId = this.templatePendingEdit?.id ?? this.selectedTemplateId;
    this.resetTemplateMetadataEdit();
    void this.navigateToTemplates({ selectedTemplateId: editedTemplateId });
  }

  handleTemplateEditCancel(event: Event): void {
    event.preventDefault();
    this.cancelTemplateMetadataEdit();
  }

  handleTemplateEditDialogClick(event: MouseEvent): void {
    if (!this.wasDialogContentClicked(event)) {
      this.cancelTemplateMetadataEdit();
    }
  }

  saveTemplateMetadata(): void {
    const details = this.templateMetadataDetails;
    const name = this.templateMetadataName.trim();
    const description = this.templateMetadataDescription.trim();
    const triggerId = Number(this.templateMetadataTriggerId);

    if (!details || this.templateMetadataSaving || this.templateMetadataLoading) {
      return;
    }

    if (!name) {
      this.templateMetadataError = 'יש למלא שם לתבנית.';
      return;
    }

    if (!Number.isInteger(triggerId) || triggerId <= 0) {
      this.templateMetadataError = 'יש לבחור אירוע קליני מפעיל.';
      return;
    }

    const templateId = Number(details.id);
    if (!Number.isInteger(templateId) || templateId <= 0) {
      this.templateMetadataError = 'מזהה התבנית אינו תקין.';
      return;
    }

    const request: SaveTemplateRequest = {
      name,
      description,
      bodies: { ...details.bodies },
      isActive: details.isActive,
    };

    this.templateMetadataSaving = true;
    this.templateMetadataError = '';

    const triggerChanged = triggerId !== this.initialTemplateMetadataTriggerId;
    this.apiService.updateTemplate(details.id, request).pipe(
      switchMap(() => triggerChanged
        ? this.apiService.updateTemplateTrigger(templateId, { triggerId })
        : of(undefined)),
    ).subscribe({
      next: () => {
        this.templateMetadataSaving = false;
        this.templates = this.sortTemplates(
          this.templates.map((template) =>
            template.id === details.id ? { ...template, name, description } : template,
          ),
        );

        if (this.selectedTemplateDetails?.id === details.id) {
          this.selectedTemplateDetails.name = name;
          this.selectedTemplateDetails.description = description;
        }

        this.closeTemplateEditDialog();
        this.resetTemplateMetadataEdit();
        void this.navigateToTemplates({
          selectedTemplateId: details.id,
          toastMessage: triggerChanged
            ? 'פרטי התבנית והאירוע הקליני עודכנו בהצלחה.'
            : 'פרטי התבנית עודכנו בהצלחה.',
        });
      },
      error: (error: unknown) => {
        this.templateMetadataSaving = false;
        if (!isAuthenticationError(error)) {
          if (error instanceof HttpErrorResponse && error.status === 409) {
            this.templateMetadataError = 'לא ניתן לעדכן את האירוע הקליני משום שנמצאו מספר שיוכים לתבנית.';
          } else if (error instanceof HttpErrorResponse && error.status === 404) {
            this.templateMetadataError = 'התבנית, האירוע הקליני או השיוך ביניהם לא נמצאו.';
          } else {
            this.templateMetadataError = 'לא ניתן לעדכן את פרטי התבנית והאירוע הקליני. יש לנסות שוב.';
          }
        }
        console.error('Failed to update template metadata', error);
      },
    });
  }

  requestTemplateDeletion(template: Template): void {
    if (this.templateDeleting || this.templateSaving || this.templateMetadataSaving) {
      return;
    }

    this.templatePendingDeletion = template;
    this.templateDeleteError = '';
    this.templateDeleteDialog?.nativeElement.showModal();
  }

  cancelTemplateDeletion(): void {
    if (this.templateDeleting) {
      return;
    }

    this.closeTemplateDeleteDialog();
    this.templatePendingDeletion = undefined;
    this.templateDeleteError = '';
  }

  handleTemplateDeleteCancel(event: Event): void {
    event.preventDefault();
    this.cancelTemplateDeletion();
  }

  handleTemplateDeleteDialogClick(event: MouseEvent): void {
    if (!this.wasDialogContentClicked(event)) {
      this.cancelTemplateDeletion();
    }
  }

  confirmTemplateDeletion(): void {
    const template = this.templatePendingDeletion;
    if (!template || this.templateDeleting || this.templateSaving) {
      return;
    }

    this.templateDeleting = true;
    this.templateDeleteError = '';

    this.apiService.deleteTemplate(template.id).subscribe({
      next: () => {
        this.templateDeleting = false;
        this.closeTemplateDeleteDialog();
        this.templatePendingDeletion = undefined;
        const deletedTemplateIndex = this.templates.findIndex((entry) => entry.id === template.id);
        const deletedSelectedTemplate = this.selectedTemplateId === template.id;
        this.templates = this.templates.filter((entry) => entry.id !== template.id);

        if (deletedSelectedTemplate) {
          this.selectedTemplateId = '';
          this.selectedTemplateDetails = undefined;
          this.templateDetailsLoading = false;
          this.templateDetailsError = '';
          this.templateSaveError = '';
          this.testStatus = '';

          const nextTemplate =
            this.templates[deletedTemplateIndex] ?? this.templates[deletedTemplateIndex - 1];
          if (nextTemplate) {
            this.selectTemplate(nextTemplate.id);
          }
        }

        this.showToast('התבנית נמחקה בהצלחה.');
      },
      error: (error) => {
        this.templateDeleting = false;
        this.templateDeleteError = 'לא ניתן למחוק את התבנית. יש לנסות שוב.';
        console.error('Failed to delete template', error);
      },
    });
  }

  private closeTemplateDeleteDialog(): void {
    const dialog = this.templateDeleteDialog?.nativeElement;
    if (dialog?.open) {
      dialog.close();
    }
  }

  private closeTemplateEditDialog(): void {
    const dialog = this.templateEditDialog?.nativeElement;
    if (dialog?.open) {
      dialog.close();
    }
  }

  private closeTemplateCreateDialog(): void {
    const dialog = this.templateCreateDialog?.nativeElement;
    if (dialog?.open) {
      dialog.close();
    }
  }

  private wasDialogContentClicked(event: MouseEvent): boolean {
    const dialog = event.currentTarget as HTMLDialogElement;
    const bounds = dialog.getBoundingClientRect();

    return (
      event.clientX >= bounds.left &&
      event.clientX <= bounds.right &&
      event.clientY >= bounds.top &&
      event.clientY <= bounds.bottom
    );
  }

  insertPlaceholder(token: string): void {
    const current = this.currentTemplateText();
    const selection = this.templateTextSelection;
    const hasCurrentSelection = selection
      && selection.templateId === this.selectedTemplateId
      && selection.language === this.selectedLanguage;
    const selectionStart = hasCurrentSelection
      ? Math.min(Math.max(selection.start, 0), current.length)
      : current.length;
    const selectionEnd = hasCurrentSelection
      ? Math.min(Math.max(selection.end, selectionStart), current.length)
      : current.length;
    const prefix = current.slice(0, selectionStart);
    const suffix = current.slice(selectionEnd);
    const leadingSpace = prefix.length > 0 && !/\s$/.test(prefix) ? ' ' : '';
    const trailingSpace = suffix.length > 0 && !/^\s/.test(suffix) ? ' ' : '';
    const insertion = `${leadingSpace}${token}${trailingSpace}`;
    const updatedText = `${prefix}${insertion}${suffix}`;
    const nextCaretPosition = prefix.length + insertion.length;

    this.setTemplateText(updatedText);
    this.templateTextSelection = {
      templateId: this.selectedTemplateId,
      language: this.selectedLanguage,
      start: nextCaretPosition,
      end: nextCaretPosition,
    };

    const textarea = this.templateTextArea?.nativeElement;
    if (textarea) {
      textarea.value = updatedText;
      textarea.focus();
      textarea.setSelectionRange(nextCaretPosition, nextCaretPosition);
    }
  }

  rememberTemplateTextSelection(event: Event): void {
    const textarea = event.target as HTMLTextAreaElement;
    this.templateTextSelection = {
      templateId: this.selectedTemplateId,
      language: this.selectedLanguage,
      start: textarea.selectionStart,
      end: textarea.selectionEnd,
    };
  }

  messageStats() {
    return calculateSmsSegments(this.currentTemplateText());
  }

  activePlaceholderCount(): number {
    return this.placeholders.length;
  }

  runTestSms(): void {
    if (this.testSmsSending) {
      return;
    }

    const template = this.selectedTemplate;
    const details = this.selectedTemplateDetails;
    const recipient = this.selectedTestRecipient();
    const categoryId = this.categoryId;
    const templateId = Number(template?.id);
    const templateCategoryId = Number(template?.categoryId);
    const languageId = this.selectedLanguage;
    const message = this.currentTemplateText();
    const phoneNumber = recipient?.phoneNumber ?? '';

    if (!Number.isInteger(categoryId) || !categoryId || categoryId <= 0) {
      this.setTestSmsStatus('לא ניתן לשלוח ללא קטגוריה תקינה. יש לחזור ולבחור קטגוריה.', 'error');
      return;
    }
    if (!template || !Number.isInteger(templateId) || templateId <= 0) {
      this.setTestSmsStatus('יש לבחור תבנית תקינה לפני שליחת הודעת הניסיון.', 'error');
      return;
    }
    if (templateCategoryId !== categoryId) {
      this.setTestSmsStatus('התבנית שנבחרה אינה משויכת לקטגוריה הנוכחית.', 'error');
      return;
    }
    if (!details || details.id !== template.id) {
      this.setTestSmsStatus('יש להמתין לטעינת נוסח התבנית לפני השליחה.', 'error');
      return;
    }
    if (!this.languageOptions.some((language) => language.code === languageId)) {
      this.setTestSmsStatus('יש לבחור שפת הודעה תקינה לפני השליחה.', 'error');
      return;
    }
    if (!message.trim()) {
      this.setTestSmsStatus('יש להזין נוסח הודעה לפני השליחה.', 'error');
      return;
    }
    if (!recipient || !/^\d+$/.test(phoneNumber)) {
      this.setTestSmsStatus('המספר אינו ברשימת הבדיקה הסגורה או שאינו תקין.', 'error');
      return;
    }

    const request: SendSmsRequest = {
      categoryId,
      templateId,
      languageId,
      message,
      phoneNumber,
    };

    this.testSmsSending = true;
    this.testStatus = '';

    this.apiService.sendSms(request).subscribe({
      next: (response) => {
        this.testSmsSending = false;
        if (!response.success) {
          this.setTestSmsStatus('בקשת ה-SMS לא התקבלה. יש לנסות שוב.', 'error');
          return;
        }

        this.setTestSmsStatus(
          `בקשת ה-SMS עבור ${recipient.name} (${phoneNumber}) נשלחה לתור בהצלחה.`,
          'success',
        );
      },
      error: (error: HttpErrorResponse) => {
        this.testSmsSending = false;
        let message = 'אירעה שגיאה בשליחת בקשת ה-SMS. יש לנסות שוב.';
        if (error.status === 400) {
          message = 'לא ניתן לשלוח את בקשת ה-SMS. יש לבדוק את הנתונים שנבחרו.';
        } else if (error.status === 500) {
          message = 'אירעה שגיאת שרת בשליחת בקשת ה-SMS. יש לנסות שוב.';
        }
        this.setTestSmsStatus(message, 'error');
        console.error('Failed to queue test SMS', error);
      },
    });
  }

  selectTestPhone(phone: string): void {
    this.testPhone = phone;
    this.testStatus = '';
  }

  private setTestSmsStatus(message: string, tone: 'success' | 'error'): void {
    this.testStatus = message;
    this.testStatusTone = tone;
  }

  selectedTestRecipient(): SmsTestPhone | undefined {
    return this.testPhones.find(
      (recipient) => recipient.phoneNumber === this.testPhone,
    );
  }

  languageLabel(language: LanguageCode): string {
    return this.languageOptions.find((option) => option.code === language)?.label ?? language;
  }

  languageDirection(language: LanguageCode): 'rtl' | 'ltr' {
    return this.languageOptions.find((option) => option.code === language)?.dir ?? 'rtl';
  }
}
