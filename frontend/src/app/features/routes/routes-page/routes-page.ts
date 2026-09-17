import { Location } from '@angular/common';
import {
  Component,
  DestroyRef,
  ElementRef,
  HostListener,
  OnInit,
  ViewChild,
  inject,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { catchError, finalize, forkJoin, map, Observable, of, switchMap, throwError } from 'rxjs';

import { Hospital, Template } from '../../../../types';
import { ApiService } from '../../../api.service';
import { isAuthenticationError } from '../../../authentication-feedback.service';
import {
  HospitalUnit,
  SmsCategory,
  SmsUnitCategory,
  SmsUnitCategoryWriteResponse,
  TriggerCatalogItem,
} from '../../settings/settings.models';
import { ClinicalRulesStore } from '../clinical-rules-store';
import {
  AssociatedUnit,
  ClinicalRuleAggregate,
  CreateSmsRuleRequest,
  MAX_RELATIVE_DELAY_MINUTES,
  MAX_VALIDITY_MINUTES,
  MIN_VALIDITY_MINUTES,
  SmsRuleHierarchyItem,
  SmsTrigger,
  SmsTriggerSettings,
  TemplateClinicalTrigger,
  UpdateSmsRuleRequest,
} from '../route.models';

export type RouteCategoryFilter = 'all' | number;
type RuleTimeField = 'recurringTimeOfDay' | 'sendWindowStartTime' | 'sendWindowEndTime';

export interface RouteCardView {
  rule: ClinicalRuleAggregate;
  description: string;
  triggerLabel: string;
  scheduleLabel: string;
  audienceLabel: string;
  departmentLabel: string;
}

interface HospitalExclusionGroup {
  id: number;
  hospitalId: string;
  selectedUnitIds: number[];
  units: HospitalUnit[];
  loading: boolean;
  error: string;
  loadSequence: number;
}

interface BackendProblemDetails {
  status?: number;
  title?: string;
  detail?: string;
}

interface RuleSaveHttpError {
  status?: number;
  error?: BackendProblemDetails | string | null;
}

interface RuleSaveFailure {
  stage: 'hospitalExclusions' | 'smsRule';
  error: RuleSaveHttpError;
}

@Component({
  selector: 'app-routes-page',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './routes-page.html',
  styleUrl: './routes-page.scss',
})
export class RoutesPage implements OnInit {
  @ViewChild('audienceUnitsDropdown')
  private audienceUnitsDropdown?: ElementRef<HTMLDetailsElement>;

  private readonly apiService = inject(ApiService);
  private readonly activatedRoute = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);
  private readonly location = inject(Location);
  private readonly ruleTemplateIds = new Map<number, string>();
  private readonly activeUpdateRuleIds = new Set<number>();
  private readonly activeUpdateErrors = new Map<number, string>();
  private readonly expandedHospitalFilterRuleIds = new Set<number>();
  private readonly expandedValidityRuleIds = new Set<number>();
  private readonly expandedRelativeDelayRuleIds = new Set<number>();
  private readonly configuredValidityRuleIds = new Set<number>();
  private readonly configuredRelativeDelayRuleIds = new Set<number>();
  private readonly editedValidityRuleIds = new Set<number>();
  private readonly editedRelativeDelayRuleIds = new Set<number>();
  private readonly editedRecurringRuleIds = new Set<number>();
  private readonly editedSendWindowRuleIds = new Set<number>();
  private readonly clearedValidityRuleIds = new Set<number>();
  private readonly clearedRelativeDelayRuleIds = new Set<number>();
  private readonly expandedRecurringRuleIds = new Set<number>();
  private readonly expandedSendWindowRuleIds = new Set<number>();
  private readonly clearedRecurringRuleIds = new Set<number>();
  private readonly clearedSendWindowRuleIds = new Set<number>();
  private readonly expandedFallbackRuleIds = new Set<number>();
  private readonly savedHospitalExclusionSignatures = new Map<number, string>();
  private readonly unitCategoryAssociations = new Map<number, SmsUnitCategoryWriteResponse>();
  private readonly savingAudienceSmsUnitIds = new Set<number>();
  private configuredTriggersLoaded = false;
  private clinicalTriggerLoadSequence = 0;
  private audienceUnitLoadSequence = 0;
  private nextHospitalExclusionGroupId = 1;
  readonly clinicalRulesStore = inject(ClinicalRulesStore);
  hospitals: Hospital[] = [];
  hospitalsLoading = false;
  hospitalsError = '';
  hospitalExclusionGroups: HospitalExclusionGroup[] = [];
  selectedAudienceHospitalId = '';
  audienceUnits: HospitalUnit[] = [];
  audienceSelections = new Map<number, Set<number>>();
  audienceUnitsLoading = false;
  audienceError = '';
  categories: SmsCategory[] = [];
  categoriesLoading = false;
  categoriesError = '';
  triggerCatalog: TriggerCatalogItem[] = [];
  configuredTriggers: SmsTriggerSettings[] = [];
  triggerDataLoading = false;
  triggerDataError = '';
  rulesDataLoading = false;
  rulesDataError = '';
  selectedTemplateClinicalTriggers: TemplateClinicalTrigger[] = [];
  clinicalTriggerLoading = false;
  clinicalTriggerError = '';
  ruleSaveLoading = false;
  ruleSaveError = '';
  toastMessage = '';
  toastTone: 'success' | 'error' = 'success';
  private toastTimer?: ReturnType<typeof setTimeout>;

  selectedRuleId = this.rules[0]?.id ?? 0;
  ruleSearch = '';
  categoryFilter = 'all';
  drawerOpen = false;
  isCreatingRule = false;
  templates: Template[] = [];
  templatesLoading = false;
  templatesError = '';
  projectId?: number;
  categoryId?: number;
  readonly recurringStopLabels: Record<string, string> = {
    discharged: this.recurringStopLabel('discharged'),
    never: this.recurringStopLabel('never'),
  };
  readonly minValidityMinutes = MIN_VALIDITY_MINUTES;
  readonly maxValidityMinutes = MAX_VALIDITY_MINUTES;
  readonly maxRelativeDelayMinutes = MAX_RELATIVE_DELAY_MINUTES;

  constructor() {
    this.destroyRef.onDestroy(() => {
      if (this.toastTimer) {
        clearTimeout(this.toastTimer);
      }
    });
  }

  ngOnInit(): void {
    if (!this.readRouteScope()) {
      this.rulesDataError = 'בחירת הפרויקט או הקטגוריה אינה תקינה. יש לחזור למסך בחירת הפרויקט.';
      return;
    }

    this.loadRulesData();
  }

  @HostListener('document:click', ['$event'])
  closeAudienceUnitsDropdownOnOutsideClick(event: MouseEvent): void {
    const dropdown = this.audienceUnitsDropdown?.nativeElement;
    const target = event.target;
    if (!dropdown?.open || !(target instanceof Node) || dropdown.contains(target)) {
      return;
    }

    dropdown.open = false;
  }

  private readRouteScope(): boolean {
    const queryParams = this.activatedRoute.snapshot.queryParamMap;
    const rawProjectId = queryParams.get('projectId');
    const rawCategoryId = queryParams.get('categoryId');

    if (rawProjectId == null || rawCategoryId == null) {
      return false;
    }

    const projectId = Number(rawProjectId);
    const categoryId = Number(rawCategoryId);
    const isValid = Number.isInteger(projectId)
      && projectId > 0
      && Number.isInteger(categoryId)
      && categoryId > 0;

    if (isValid) {
      this.projectId = projectId;
      this.categoryId = categoryId;
    }

    return isValid;
  }

  private loadRulesData(): void {
    const projectId = this.projectId;
    const categoryId = this.categoryId;
    if (!projectId || !categoryId) {
      this.rulesDataError = 'בחירת הפרויקט או הקטגוריה אינה תקינה. יש לחזור למסך בחירת הפרויקט.';
      return;
    }

    this.rulesDataLoading = true;
    this.rulesDataError = '';
    this.templatesLoading = true;
    this.categoriesLoading = true;
    this.triggerDataLoading = true;
    this.hospitalsLoading = true;
    this.templatesError = '';
    this.categoriesError = '';
    this.triggerDataError = '';
    this.hospitalsError = '';
    this.configuredTriggersLoaded = false;
    this.configuredTriggers = [];
    this.clinicalRulesStore.replaceAll([]);
    this.ruleTemplateIds.clear();
    this.savedHospitalExclusionSignatures.clear();
    this.resetAudienceSelection();

    forkJoin({
      ruleHierarchy: this.apiService.getRulesByScope(projectId, categoryId),
      templates: this.apiService.getTemplatesByScope(projectId, categoryId),
      category: this.apiService.getCategory(categoryId),
      triggerCatalog: this.apiService.getTriggerCatalog(),
      hospitals: this.apiService.getHospitals(),
      unitCategories: this.apiService.getSmsUnitCategories(),
    }).pipe(
      switchMap((data) => {
        const unitRequests = data.hospitals
          .map((hospital) => Number(hospital.id))
          .filter((hospitalId) => Number.isInteger(hospitalId) && hospitalId > 0)
          .map((hospitalId) => this.apiService.getHospitalUnits(hospitalId).pipe(
            catchError((error) => {
              console.error(`Failed to preload units for hospital ${hospitalId}`, error);
              return of([] as HospitalUnit[]);
            }),
          ));
        const unitsByHospital$ = unitRequests.length > 0
          ? forkJoin(unitRequests)
          : of([] as HospitalUnit[][]);

        return unitsByHospital$.pipe(
          map((unitsByHospital) => ({
            ...data,
            hospitalUnits: unitsByHospital.flat(),
          })),
        );
      }),
    ).subscribe({
      next: ({
        ruleHierarchy,
        templates,
        category,
        triggerCatalog,
        hospitals,
        unitCategories,
        hospitalUnits,
      }) => {
        this.templates = templates;
        this.categories = [category];
        this.triggerCatalog = triggerCatalog;
        this.configuredTriggers = ruleHierarchy.map((item) =>
          this.toConfiguredTrigger(item),
        );
        this.hospitals = hospitals;
        this.initializeAudienceSelection(unitCategories, hospitalUnits);
        this.configuredTriggersLoaded = true;
        this.clinicalRulesStore.replaceAll(
          this.buildRulesFromBackend(ruleHierarchy, category),
        );

        for (const rule of this.rules) {
          this.ruleTemplateIds.set(rule.id, String(rule.template.templateId));
          this.savedHospitalExclusionSignatures.set(
            rule.id,
            this.hospitalExclusionSignature(rule.associatedUnits),
          );
          const configuredTrigger = this.configuredTriggerFor(rule);
          if (configuredTrigger) {
            this.setLoadedRuleConfigurationState(rule, configuredTrigger);
          }
        }

        this.selectedRuleId = this.rules[0]?.id ?? 0;
        this.rulesDataLoading = false;
        this.templatesLoading = false;
        this.categoriesLoading = false;
        this.triggerDataLoading = false;
        this.hospitalsLoading = false;
        this.openRuleFromRoute();
      },
      error: (error) => {
        this.templates = [];
        this.categories = [];
        this.triggerCatalog = [];
        this.configuredTriggers = [];
        this.hospitals = [];
        this.configuredTriggersLoaded = false;
        this.clinicalRulesStore.replaceAll([]);
        this.rulesDataLoading = false;
        this.templatesLoading = false;
        this.categoriesLoading = false;
        this.triggerDataLoading = false;
        this.hospitalsLoading = false;
        this.resetAudienceSelection();
        this.rulesDataError = 'לא ניתן לטעון את כללי השליחה מהשרת.';
        console.error('Failed to load clinical rules from backend data', error);
      },
    });
  }

  private loadHospitalUnits(group: HospitalExclusionGroup): void {
    const loadSequence = ++group.loadSequence;
    group.loading = true;
    group.error = '';

    this.apiService.getHospitalUnits(Number(group.hospitalId)).subscribe({
      next: (units) => {
        if (!this.hospitalExclusionGroups.includes(group) || loadSequence !== group.loadSequence) {
          return;
        }

        const hospitalId = Number(group.hospitalId);
        const audienceUnitIds = this.audienceSelections.get(hospitalId) ?? new Set<number>();
        const audienceUnits = units.filter((unit) => audienceUnitIds.has(unit.unitId));
        const availableUnitIds = new Set(audienceUnits.map((unit) => unit.unitId));
        group.selectedUnitIds = group.selectedUnitIds.filter(
          (unitId) => availableUnitIds.has(unitId),
        );
        group.units = audienceUnits;
        group.loading = false;

        const rule = this.selectedRule;
        if (rule) {
          this.syncHospitalExclusions(rule);
        }
      },
      error: (error) => {
        if (!this.hospitalExclusionGroups.includes(group) || loadSequence !== group.loadSequence) {
          return;
        }

        group.loading = false;
        group.error = 'לא ניתן לטעון את המחלקות של בית החולים.';
        console.error(`Failed to load units for hospital ${group.hospitalId}`, error);
      },
    });
  }

  private resetAudienceSelection(): void {
    this.audienceUnitLoadSequence += 1;
    this.selectedAudienceHospitalId = '';
    this.audienceUnits = [];
    this.audienceSelections = new Map<number, Set<number>>();
    this.unitCategoryAssociations.clear();
    this.savingAudienceSmsUnitIds.clear();
    this.audienceUnitsLoading = false;
    this.audienceError = '';
  }

  private initializeAudienceSelection(
    unitCategories: SmsUnitCategory[],
    hospitalUnits: HospitalUnit[],
  ): void {
    const categoryId = this.categoryId;
    if (!categoryId) return;

    for (const association of unitCategories) {
      if (association.categoryId !== categoryId) continue;

      this.unitCategoryAssociations.set(association.smsUnitId, {
        ucId: association.ucId,
        smsUnitId: association.smsUnitId,
        categoryId: association.categoryId,
        isActive: association.isActive,
      });
    }

    const unitsByHospital = new Map<number, HospitalUnit[]>();
    for (const unit of hospitalUnits) {
      const units = unitsByHospital.get(unit.hospitalId) ?? [];
      units.push(unit);
      unitsByHospital.set(unit.hospitalId, units);
    }

    for (const [hospitalId, units] of unitsByHospital) {
      this.syncAudienceSelectionForHospital(hospitalId, units);
    }
  }

  private syncAudienceSelectionForHospital(
    hospitalId: number,
    units: HospitalUnit[],
  ): void {
    const selectedUnitIds = new Set(
      units
        .filter((unit) => {
          const smsUnitId = Number(unit.smsUnitId);
          return Number.isInteger(smsUnitId)
            && smsUnitId > 0
            && this.unitCategoryAssociations.get(smsUnitId)?.isActive === true;
        })
        .map((unit) => unit.unitId),
    );
    const nextSelections = new Map(this.audienceSelections);

    if (selectedUnitIds.size > 0) {
      nextSelections.set(hospitalId, selectedUnitIds);
    } else {
      nextSelections.delete(hospitalId);
    }

    this.audienceSelections = nextSelections;
  }

  private scopeTemplateIds(): number[] {
    return [...new Set(
      this.templates
        .map((template) => Number(template.id))
        .filter((templateId) => Number.isInteger(templateId) && templateId > 0),
    )];
  }

  setSelectedAudienceHospital(hospitalId: string): void {
    this.audienceUnitLoadSequence += 1;
    this.selectedAudienceHospitalId = hospitalId;
    this.audienceUnits = [];
    this.audienceUnitsLoading = false;

    const numericHospitalId = Number(hospitalId);
    if (Number.isInteger(numericHospitalId) && numericHospitalId > 0) {
      this.loadAudienceHospitalUnits(numericHospitalId);
    }
  }

  private loadAudienceHospitalUnits(hospitalId: number): void {
    const loadSequence = ++this.audienceUnitLoadSequence;
    this.audienceUnitsLoading = true;
    this.audienceError = '';

    this.apiService.getHospitalUnits(hospitalId).subscribe({
      next: (units) => {
        if (
          loadSequence !== this.audienceUnitLoadSequence
          || Number(this.selectedAudienceHospitalId) !== hospitalId
        ) {
          return;
        }

        this.syncAudienceSelectionForHospital(hospitalId, units);
        this.audienceUnits = units;
        this.audienceUnitsLoading = false;
      },
      error: (error) => {
        if (loadSequence !== this.audienceUnitLoadSequence) return;

        this.audienceUnits = [];
        this.audienceUnitsLoading = false;
        this.audienceError = 'לא ניתן לטעון את המחלקות של בית החולים.';
        console.error(`Failed to load audience units for hospital ${hospitalId}`, error);
      },
    });
  }

  setAudienceUnitSelected(unitId: number, isSelected: boolean): void {
    const hospitalId = Number(this.selectedAudienceHospitalId);
    const projectId = this.projectId;
    const categoryId = this.categoryId;
    const unit = this.audienceUnits.find((candidate) => candidate.unitId === unitId);
    const smsUnitId = Number(unit?.smsUnitId);
    if (
      !Number.isInteger(hospitalId)
      || hospitalId <= 0
      || !projectId
      || !categoryId
      || !Number.isInteger(unitId)
      || unitId <= 0
      || !Number.isInteger(smsUnitId)
      || smsUnitId <= 0
    ) {
      this.audienceError = 'לא ניתן לשמור את המחלקה משום שחסר עבורה smsUnitId תקין.';
      return;
    }

    const wasSelected = this.isAudienceUnitSelected(unitId);
    if (wasSelected === isSelected || this.savingAudienceSmsUnitIds.has(smsUnitId)) return;

    this.audienceError = '';
    this.savingAudienceSmsUnitIds.add(smsUnitId);

    if (isSelected) {
      this.apiService.createSmsUnitCategory({
        projectId,
        categoryId,
        hospitalId,
        smsUnitId,
      }).pipe(
        finalize(() => this.savingAudienceSmsUnitIds.delete(smsUnitId)),
      ).subscribe({
        next: (association) => {
          this.unitCategoryAssociations.set(smsUnitId, association);
          this.updateAudienceSelection(hospitalId, unitId, true);
        },
        error: (error) => {
          if (!isAuthenticationError(error)) {
            this.audienceError = 'לא ניתן להוסיף את המחלקה לקהל היעד. יש לנסות שוב.';
          }
          console.error(`Failed to activate sms unit category ${smsUnitId}`, error);
        },
      });
      return;
    }

    const association = this.unitCategoryAssociations.get(smsUnitId);
    if (!association) {
      this.savingAudienceSmsUnitIds.delete(smsUnitId);
      this.updateAudienceSelection(hospitalId, unitId, false);
      this.pruneRuleExclusionsToAudience();
      return;
    }

    this.apiService.updateSmsUnitCategoryStatus(association.ucId, false).pipe(
      finalize(() => this.savingAudienceSmsUnitIds.delete(smsUnitId)),
    ).subscribe({
      next: () => {
        this.unitCategoryAssociations.set(smsUnitId, {
          ...association,
          isActive: false,
        });
        this.updateAudienceSelection(hospitalId, unitId, false);
        this.pruneRuleExclusionsToAudience();
      },
      error: (error) => {
        if (!isAuthenticationError(error)) {
          this.audienceError = 'לא ניתן להסיר את המחלקה מקהל היעד. יש לנסות שוב.';
        }
        console.error(`Failed to deactivate sms unit category ${association.ucId}`, error);
      },
    });
  }

  isAudienceUnitSaving(unit: HospitalUnit): boolean {
    const smsUnitId = Number(unit.smsUnitId);
    return Number.isInteger(smsUnitId)
      && smsUnitId > 0
      && this.savingAudienceSmsUnitIds.has(smsUnitId);
  }

  private updateAudienceSelection(
    hospitalId: number,
    unitId: number,
    isSelected: boolean,
  ): void {
    const nextSelections = new Map(this.audienceSelections);
    const hospitalUnits = new Set(nextSelections.get(hospitalId) ?? []);

    if (isSelected) {
      hospitalUnits.add(unitId);
      nextSelections.set(hospitalId, hospitalUnits);
    } else {
      hospitalUnits.delete(unitId);
      if (hospitalUnits.size > 0) {
        nextSelections.set(hospitalId, hospitalUnits);
      } else {
        nextSelections.delete(hospitalId);
      }
    }

    this.audienceSelections = nextSelections;
  }

  isAudienceUnitSelected(unitId: number): boolean {
    const hospitalId = Number(this.selectedAudienceHospitalId);
    return this.audienceSelections.get(hospitalId)?.has(unitId) === true;
  }

  isAudienceHospitalSelected(hospitalId: string | number): boolean {
    const numericHospitalId = Number(hospitalId);
    return Number.isInteger(numericHospitalId)
      && numericHospitalId > 0
      && (this.audienceSelections.get(numericHospitalId)?.size ?? 0) > 0;
  }

  private pruneRuleExclusionsToAudience(): void {
    for (const rule of this.rules) {
      rule.associatedUnits = rule.associatedUnits.filter(
        (unit) => this.audienceSelections.get(unit.hospitalId)?.has(unit.unitId) === true,
      );
    }

    for (const group of this.hospitalExclusionGroups) {
      group.loadSequence += 1;
    }
    this.hospitalExclusionGroups = [];
    if (this.drawerOpen) {
      this.initializeHospitalUnitSelection();
    }
  }

  get audienceHospitals(): Hospital[] {
    return this.hospitals.filter((hospital) => this.isAudienceHospitalSelected(hospital.id));
  }

  audienceUnitSelectionLabel(): string {
    const hospitalId = Number(this.selectedAudienceHospitalId);
    if (!Number.isInteger(hospitalId) || hospitalId <= 0) return 'יש לבחור בית חולים תחילה';
    if (this.audienceUnitsLoading) return 'טוען מחלקות...';
    if (this.audienceUnits.length === 0) return 'לא נמצאו מחלקות';

    const selectedUnitIds = this.audienceSelections.get(hospitalId) ?? new Set<number>();
    if (selectedUnitIds.size === 0) return 'בחר מחלקות לשליחה';
    if (selectedUnitIds.size === 1) {
      const selectedUnitId = [...selectedUnitIds][0];
      return this.audienceUnits.find((unit) => unit.unitId === selectedUnitId)?.unitName
        ?? 'מחלקה אחת נבחרה';
    }
    return `${selectedUnitIds.size} מחלקות נבחרו`;
  }

  audienceHospitalOptionLabel(hospital: Hospital): string {
    const selectedCount = this.audienceSelections.get(Number(hospital.id))?.size ?? 0;
    return selectedCount > 0 ? `${hospital.name} (${selectedCount} נבחרו)` : hospital.name;
  }

  get selectedAudienceHospitalIds(): number[] {
    return [...this.audienceSelections.entries()]
      .filter(([, unitIds]) => unitIds.size > 0)
      .map(([hospitalId]) => hospitalId);
  }

  get audienceSelectedUnitCount(): number {
    return [...this.audienceSelections.values()].reduce(
      (total, unitIds) => total + unitIds.size,
      0,
    );
  }

  audienceSummaryLabel(): string {
    const hospitalCount = this.selectedAudienceHospitalIds.length;
    const unitCount = this.audienceSelectedUnitCount;
    if (unitCount === 0) return 'לא הוגדר קהל יעד';
    if (hospitalCount === 1) {
      return `${unitCount} מחלקות ב${this.hospitalName(this.selectedAudienceHospitalIds[0])}`;
    }
    return `${unitCount} מחלקות ב־${hospitalCount} בתי חולים`;
  }

  audienceScopeStatusLabel(): string {
    const templateCount = this.scopeTemplateIds().length;
    return `${this.audienceSummaryLabel()} · חל על ${templateCount} תבניות בקטגוריה`;
  }

  get rules(): ClinicalRuleAggregate[] {
    return this.clinicalRulesStore.getAll();
  }

  private buildRulesFromBackend(
    items: SmsRuleHierarchyItem[],
    scopedCategory: SmsCategory,
  ): ClinicalRuleAggregate[] {
    return items.map((item) => {
      const templateId = item.template.templateId;
      const categoryId = item.category.categoryId;
      const configuredTrigger = this.toConfiguredTrigger(item);
      const recurringTimeOfDay = this.formatRuleTimeInput(
        configuredTrigger.recurringTimeOfDay ?? '',
      );
      const isConstant = configuredTrigger.isConstant === true;
      const isRecurring = configuredTrigger.isRecurring === true;

      return {
        id: templateId,
        template: {
          templateId,
          templateCode: item.template.templateCode,
          templateName: item.template.templateName,
          categoryId,
          isActive: item.template.isActive,
          isEditable: item.template.isEditable,
          versionNumber: item.template.versionNumber,
        },
        category: {
          ...scopedCategory,
          categoryId,
          categoryName: item.category.categoryName,
          projectId: item.projectId,
          isActive: item.category.isActive,
          isEditable: item.category.isEditable,
        },
        languages: [],
        associatedUnits: (item.excludedHospitalUnits ?? []).map((unit) => ({
          hospitalId: unit.hospitalId,
          hospitalName: unit.hospitalName ?? `בית חולים ${unit.hospitalId}`,
          unitId: unit.unitId,
          unitName: unit.unitName ?? `יחידה ${unit.unitId}`,
        })),
        step: {
          categoryStepId: 0,
          categoryId,
          templateId,
          delayInMinutes: 0,
          delayFromStepId: configuredTrigger.dependOnTtId ?? undefined,
          delayFromStepMinutes: configuredTrigger.dependencyMaxMinutes ?? 0,
          hasDependencyLimit: configuredTrigger.maxTimeTokefInMinutes !== null,
          dependencyLimitMinutes: configuredTrigger.maxTimeTokefInMinutes ?? 0,
          isActive: configuredTrigger.triggerIsActive,
          isEditable: item.template.isEditable,
        },
        trigger: {
          triggerId: configuredTrigger.triggerId,
          templateId,
          isConstant,
          sendType: isConstant || isRecurring || Boolean(recurringTimeOfDay)
            ? 'Time-Based'
            : 'Event-Based',
          sendTime: recurringTimeOfDay || undefined,
          triggerEventCode: configuredTrigger.triggerCode,
          isRecurring,
          recurringIntervalDays: configuredTrigger.recurringIntervalDays ?? null,
          recurringTimeOfDay,
          sendWindowStartTime: this.formatRuleTimeInput(
            configuredTrigger.startTimeRange ?? '',
          ),
          sendWindowEndTime: this.formatRuleTimeInput(
            configuredTrigger.endTimeRange ?? '',
          ),
          recurringStopCondition: this.normalizeRecurringStopCondition(
            configuredTrigger.recurringStopCondition,
          ),
          onetimeFallbackEnabled: configuredTrigger.onetimeFallbackEnabled === true,
          onetimeFallbackTime: configuredTrigger.onetimeFallbackTime?.trim() ?? '',
        },
      } satisfies ClinicalRuleAggregate;
    });
  }

  private toConfiguredTrigger(item: SmsRuleHierarchyItem): SmsTriggerSettings {
    return {
      ruleId: item.rule?.ruleId ?? null,
      ttId: item.templateTrigger.ttId,
      templateId: item.template.templateId,
      triggerId: item.templateTrigger.triggerId,
      triggerCode: item.templateTrigger.triggerCode,
      triggerName: item.templateTrigger.triggerName,
      triggerDescription: item.templateTrigger.triggerDescription,
      triggerIsActive: item.templateTrigger.triggerIsActive,
      dependOnTtId: item.rule?.dependOnTtId ?? null,
      dependencyMaxMinutes: item.rule?.dependencyMaxMinutes ?? null,
      maxTimeTokefInMinutes: item.rule?.maxTimeTokefInMinutes ?? null,
      isConstant: item.rule?.isConstant ?? null,
      isRecurring: item.rule?.isRecurring ?? null,
      recurringIntervalDays: item.rule?.recurringIntervalDays ?? null,
      recurringTimeOfDay: item.rule?.recurringTimeOfDay ?? null,
      startTimeRange: item.rule?.startTimeRange ?? null,
      endTimeRange: item.rule?.endTimeRange ?? null,
      recurringStopCondition: item.rule?.recurringStopCondition ?? null,
      onetimeFallbackEnabled: item.rule?.onetimeFallbackEnabled ?? null,
      onetimeFallbackTime: item.rule?.onetimeFallbackTime ?? null,
      createDate: item.rule?.createDate ?? null,
      updateDate: item.rule?.updateDate ?? null,
    };
  }

  private selectConfiguredTrigger(settings: SmsTriggerSettings[]): SmsTriggerSettings | undefined {
    const settingsWithRule = settings.filter((setting) => setting.ruleId !== null);
    if (settingsWithRule.length > 0) {
      return settingsWithRule.reduce((selected, candidate) =>
        (candidate.ruleId ?? 0) > (selected.ruleId ?? 0) ? candidate : selected);
    }

    return settings.reduce<SmsTriggerSettings | undefined>(
      (selected, candidate) => !selected || candidate.ttId < selected.ttId ? candidate : selected,
      undefined,
    );
  }

  get routeCards(): RouteCardView[] {
    return this.filteredRules().map((rule) => ({
      rule,
      description: this.ruleDescription(rule),
      triggerLabel: this.ruleTriggerLabel(rule),
      scheduleLabel: this.ruleScheduleLabel(rule),
      audienceLabel: this.audienceSummaryLabel(),
      departmentLabel: this.ruleDepartmentLabel(rule),
    }));
  }

  get activeRuleCount(): number {
    return this.rules.filter((rule) => rule.template.isActive && rule.step.isActive).length;
  }

  get activeTriggerCount(): number {
    return this.triggerCatalog.filter((trigger) => trigger.isActive).length;
  }

  get unitAssociationCount(): number {
    return this.rules.reduce((total, rule) => total + rule.associatedUnits.length, 0);
  }

  get selectedRule(): ClinicalRuleAggregate | undefined {
    return this.clinicalRulesStore.findById(this.selectedRuleId) ?? this.rules[0];
  }

  get selectedTriggerCatalogItem(): TriggerCatalogItem | undefined {
    const triggerCode = this.selectedConfiguredTrigger?.triggerCode;
    return this.triggerCatalog.find((trigger) => trigger.triggerCode === triggerCode);
  }

  get selectedConfiguredTrigger(): SmsTriggerSettings | undefined {
    return this.selectedRule ? this.configuredTriggerFor(this.selectedRule) : undefined;
  }

  selectedApiTemplateId(rule: ClinicalRuleAggregate): string {
    return this.ruleTemplateIds.get(rule.id) ?? '';
  }

  ruleTemplateId(rule: ClinicalRuleAggregate): number {
    const selectedTemplateId = this.selectedApiTemplateId(rule);
    if (this.isCreatingRule && this.selectedRuleId === rule.id && !selectedTemplateId) {
      return 0;
    }

    const templateId = Number(selectedTemplateId || rule.template.templateId);
    return Number.isInteger(templateId) && templateId > 0 ? templateId : 0;
  }

  setRuleTemplate(rule: ClinicalRuleAggregate, templateId: string): void {
    if (!this.isCreatingRule || this.selectedRuleId !== rule.id) return;

    const template = this.templates.find((entry) => entry.id === templateId);
    if (template) {
      this.syncRuleTemplate(rule, template);
      this.loadTemplateClinicalTrigger(Number(template.id));
    }
  }

  selectedTemplateName(rule: ClinicalRuleAggregate): string {
    return this.apiTemplateFor(rule)?.name ?? rule.template.templateName;
  }

  selectedTemplateCategoryName(rule: ClinicalRuleAggregate): string {
    const categoryId = rule.template.categoryId || this.categoryId;
    return this.categories.find(
      (category) => category.categoryId === Number(categoryId),
    )?.categoryName ?? rule.category.categoryName;
  }

  private apiTemplateFor(rule: ClinicalRuleAggregate): Template | undefined {
    const templateId = this.selectedApiTemplateId(rule);
    return templateId
      ? this.templates.find((template) => template.id === templateId)
      : undefined;
  }

  private syncRuleTemplate(rule: ClinicalRuleAggregate, template: Template): void {
    const templateId = Number(template.id);
    const categoryId = Number(template.categoryId);

    this.ruleTemplateIds.set(rule.id, template.id);
    rule.template.templateId = templateId;
    rule.template.templateCode = template.description;
    rule.template.templateName = template.name;
    rule.template.isActive = template.isActive;
    rule.template.isEditable = template.isEditable;
    rule.template.versionNumber = template.versionNumber;
    rule.step.templateId = templateId;
    rule.step.dependencyLimitStepId = undefined;
    rule.trigger.templateId = templateId;

    const selectedTemplateTtId = this.configuredTriggers.find(
      (trigger) => trigger.templateId === templateId,
    )?.ttId;
    if (selectedTemplateTtId && rule.step.delayFromStepId === selectedTemplateTtId) {
      rule.step.delayFromStepId = undefined;
    }

    if (Number.isFinite(categoryId)) {
      rule.template.categoryId = categoryId;
      rule.step.categoryId = categoryId;
      this.syncRuleCategory(rule);
    }

    if (this.configuredTriggersLoaded) {
      this.applyConfiguredTrigger(rule);
    }
  }

  private syncRuleCategory(rule: ClinicalRuleAggregate): void {
    const category = this.categories.find(
      (entry) => entry.categoryId === rule.template.categoryId,
    );
    if (category) {
      rule.category = category;
    }
  }

  setRuleTrigger(rule: ClinicalRuleAggregate, triggerCode: string): void {
    rule.trigger.triggerEventCode = triggerCode;
    rule.trigger.sendType = triggerCode === 'EVT_DAILY_ROUND' ? 'Time-Based' : 'Event-Based';
  }

  configuredTriggerFor(rule: ClinicalRuleAggregate): SmsTriggerSettings | undefined {
    const templateId = this.ruleTemplateId(rule);
    if (!templateId) {
      return undefined;
    }

    const templateSettings = this.configuredTriggers.filter(
      (setting) => setting.templateId === templateId,
    );
    return this.selectConfiguredTrigger(templateSettings);
  }

  private applyConfiguredTrigger(rule: ClinicalRuleAggregate): void {
    const templateId = this.ruleTemplateId(rule);
    if (!templateId) {
      return;
    }

    const configuredTrigger = this.configuredTriggerFor(rule);

    if (!configuredTrigger) {
      this.clearConfiguredRuleValues(rule, templateId);
      return;
    }

    const recurringTimeOfDay = this.formatRuleTimeInput(
      configuredTrigger.recurringTimeOfDay ?? '',
    );
    const isConstant = configuredTrigger.isConstant === true;
    const isRecurring = configuredTrigger.isRecurring === true;

    rule.trigger = {
      triggerId: configuredTrigger.triggerId,
      templateId,
      isConstant,
      sendType: isConstant || isRecurring || Boolean(recurringTimeOfDay)
        ? 'Time-Based'
        : 'Event-Based',
      sendTime: recurringTimeOfDay || undefined,
      triggerEventCode: configuredTrigger.triggerCode,
      isRecurring,
      recurringIntervalDays: configuredTrigger.recurringIntervalDays ?? null,
      recurringTimeOfDay,
      sendWindowStartTime: this.formatRuleTimeInput(
        configuredTrigger.startTimeRange ?? '',
      ),
      sendWindowEndTime: this.formatRuleTimeInput(
        configuredTrigger.endTimeRange ?? '',
      ),
      recurringStopCondition: this.normalizeRecurringStopCondition(
        configuredTrigger.recurringStopCondition,
      ),
      onetimeFallbackEnabled: configuredTrigger.onetimeFallbackEnabled === true,
      onetimeFallbackTime: configuredTrigger.onetimeFallbackTime?.trim() ?? '',
    };

    rule.step.delayFromStepId = configuredTrigger.dependOnTtId ?? undefined;
    rule.step.delayFromStepMinutes = configuredTrigger.dependencyMaxMinutes ?? 0;
    rule.step.hasDependencyLimit = configuredTrigger.maxTimeTokefInMinutes !== null;
    rule.step.dependencyLimitMinutes = configuredTrigger.maxTimeTokefInMinutes ?? 0;

    this.setLoadedRuleConfigurationState(rule, configuredTrigger);
  }

  private clearConfiguredRuleValues(rule: ClinicalRuleAggregate, templateId: number): void {
    rule.trigger = {
      triggerId: 0,
      templateId,
      isConstant: false,
      sendType: 'Event-Based',
      triggerEventCode: undefined,
      isRecurring: false,
      recurringIntervalDays: null,
      recurringTimeOfDay: '',
      sendWindowStartTime: '',
      sendWindowEndTime: '',
      recurringStopCondition: 'never',
      onetimeFallbackEnabled: false,
      onetimeFallbackTime: '',
    };
    rule.step.delayFromStepId = undefined;
    rule.step.delayFromStepMinutes = 0;
    rule.step.hasDependencyLimit = false;
    rule.step.dependencyLimitMinutes = 0;
    this.configuredValidityRuleIds.delete(rule.id);
    this.configuredRelativeDelayRuleIds.delete(rule.id);
  }

  private setLoadedRuleConfigurationState(
    rule: ClinicalRuleAggregate,
    configuredTrigger: SmsTriggerSettings,
  ): void {
    if (configuredTrigger.maxTimeTokefInMinutes !== null) {
      this.configuredValidityRuleIds.add(rule.id);
    } else {
      this.configuredValidityRuleIds.delete(rule.id);
    }

    if (
      configuredTrigger.dependOnTtId !== null
      || configuredTrigger.dependencyMaxMinutes !== null
    ) {
      this.configuredRelativeDelayRuleIds.add(rule.id);
    } else {
      this.configuredRelativeDelayRuleIds.delete(rule.id);
    }
  }

  private normalizeRecurringStopCondition(
    value: string | null,
  ): SmsTrigger['recurringStopCondition'] {
    return value === 'discharged' ? 'discharged' : 'never';
  }

  triggerCatalogItem(rule: ClinicalRuleAggregate): TriggerCatalogItem | undefined {
    return this.triggerCatalog.find((trigger) => trigger.triggerCode === rule.trigger.triggerEventCode);
  }

  filteredRules(): ClinicalRuleAggregate[] {
    const term = this.ruleSearch.trim().toLowerCase();
    return this.rules.filter((rule) => {
      const categoryMatches =
        this.categoryFilter === 'all' || String(rule.category.categoryId) === this.categoryFilter;
      const searchableText = [
        rule.template.templateName,
        rule.template.templateCode,
        rule.category.categoryName,
        rule.category.categoryType,
        this.ruleTriggerLabel(rule),
        this.ruleUnitsPreview(rule),
      ].join(' ').toLowerCase();
      return categoryMatches && (!term || searchableText.includes(term));
    });
  }

  setCategoryFilter(categoryId: RouteCategoryFilter): void {
    this.categoryFilter = categoryId === 'all' ? 'all' : String(categoryId);
  }

  updateRuleSearch(value: string): void {
    this.ruleSearch = value;
    const normalizedValue = value.trim().toLocaleLowerCase('he');

    if (!normalizedValue) {
      this.setCategoryFilter('all');
      return;
    }

    const matchingCategory = this.categories.find((category) =>
      normalizedValue.includes(category.categoryName.toLocaleLowerCase('he')),
    );

    if (matchingCategory) {
      this.setCategoryFilter(matchingCategory.categoryId);
    }
  }

  openRule(ruleId: number, updateUrl = true): void {
    this.isCreatingRule = false;

    const targetRule = this.clinicalRulesStore.findById(ruleId);
    const targetTemplateId = targetRule ? this.ruleTemplateId(targetRule) : 0;
    if (updateUrl && targetTemplateId) {
      this.location.go(this.withScope(`/rules/edit/${targetTemplateId}`));
    }

    this.selectedRuleId = ruleId;
    this.ruleSaveLoading = false;
    this.ruleSaveError = '';

    const rule = this.selectedRule;
    if (rule) {
      this.collapseRuleSections(rule);
      this.resetRuleEditState(rule);
    }

    this.initializeHospitalUnitSelection();
    this.drawerOpen = true;

    const templateId = rule ? this.ruleTemplateId(rule) : 0;
    this.loadTemplateClinicalTrigger(templateId);
  }

  private openRuleFromRoute(): void {
    if (this.activatedRoute.snapshot.routeConfig?.path === 'rules/create') {
      if (!this.drawerOpen || !this.isCreatingRule) {
        this.createDraftRule(false);
      }
      return;
    }

    const templateId = Number(this.activatedRoute.snapshot.paramMap.get('templateId'));
    if (!Number.isInteger(templateId) || templateId <= 0) return;

    const rule = this.rules.find((candidate) => this.ruleTemplateId(candidate) === templateId);
    if (!rule || (this.drawerOpen && this.selectedRuleId === rule.id)) return;

    this.openRule(rule.id, false);
  }

  closeDrawer(): void {
    if (this.ruleSaveLoading) return;

    const rule = this.selectedRule;
    const discardedDraftRuleId = this.isCreatingRule ? rule?.id : undefined;
    if (rule) {
      this.collapseRuleSections(rule);
      this.resetRuleEditState(rule);
    }

    this.resetTemplateClinicalTrigger();
    this.hospitalExclusionGroups = [];
    this.ruleSaveError = '';
    this.drawerOpen = false;
    this.isCreatingRule = false;

    if (discardedDraftRuleId !== undefined) {
      this.clinicalRulesStore.remove(discardedDraftRuleId);
      this.ruleTemplateIds.delete(discardedDraftRuleId);
      this.savedHospitalExclusionSignatures.delete(discardedDraftRuleId);
      this.selectedRuleId = this.rules[0]?.id ?? 0;
    }

    const currentPath = this.location.path().split('?', 1)[0];
    if (currentPath === '/rules/create' || currentPath.startsWith('/rules/edit/')) {
      this.location.replaceState(this.withScope('/rules'));
    }
  }

  private collapseRuleSections(rule: ClinicalRuleAggregate): void {
    this.expandedHospitalFilterRuleIds.delete(rule.id);
    this.expandedValidityRuleIds.delete(rule.id);
    this.expandedRelativeDelayRuleIds.delete(rule.id);
    this.expandedRecurringRuleIds.delete(rule.id);
    this.expandedSendWindowRuleIds.delete(rule.id);
    this.expandedFallbackRuleIds.delete(rule.id);
  }

  private resetRuleEditState(rule: ClinicalRuleAggregate): void {
    this.editedValidityRuleIds.delete(rule.id);
    this.editedRelativeDelayRuleIds.delete(rule.id);
    this.editedRecurringRuleIds.delete(rule.id);
    this.editedSendWindowRuleIds.delete(rule.id);
    this.clearedValidityRuleIds.delete(rule.id);
    this.clearedRelativeDelayRuleIds.delete(rule.id);
    this.clearedRecurringRuleIds.delete(rule.id);
    this.clearedSendWindowRuleIds.delete(rule.id);
  }

  private loadTemplateClinicalTrigger(templateId: number): void {
    const loadSequence = ++this.clinicalTriggerLoadSequence;
    this.selectedTemplateClinicalTriggers = [];
    this.clinicalTriggerError = '';

    if (
      !Number.isInteger(templateId)
      || templateId <= 0
      || !this.projectId
      || !this.categoryId
    ) {
      this.clinicalTriggerLoading = false;
      return;
    }

    this.clinicalTriggerLoading = true;
    this.apiService.getRulesByTemplateScope(
      this.projectId,
      this.categoryId,
      templateId,
    ).subscribe({
      next: (items) => {
        if (loadSequence !== this.clinicalTriggerLoadSequence) return;

        const triggers = items.map((item) => ({
          ttId: item.templateTrigger.ttId,
          templateId: item.template.templateId,
          triggerId: item.templateTrigger.triggerId,
          triggerName: item.templateTrigger.triggerName,
          triggerCode: item.templateTrigger.triggerCode,
          description: item.templateTrigger.triggerDescription,
          isActive: item.templateTrigger.triggerIsActive,
        } satisfies TemplateClinicalTrigger));
        this.selectedTemplateClinicalTriggers = triggers;
        this.clinicalTriggerLoading = false;

        if (triggers.length === 0) {
          this.clinicalTriggerError = `לא הוגדר אירוע קליני מפעיל לתבנית ID ${templateId}.`;
        }
      },
      error: (error) => {
        if (loadSequence !== this.clinicalTriggerLoadSequence) return;

        this.clinicalTriggerLoading = false;
        this.clinicalTriggerError = error?.status === 404
          ? `לא הוגדר אירוע קליני מפעיל לתבנית ID ${templateId}.`
          : 'לא ניתן לטעון את האירוע הקליני המפעיל מהשרת.';
        console.error(`Failed to load the clinical trigger for template ${templateId}`, error);
      },
    });
  }

  private resetTemplateClinicalTrigger(): void {
    this.clinicalTriggerLoadSequence += 1;
    this.selectedTemplateClinicalTriggers = [];
    this.clinicalTriggerLoading = false;
    this.clinicalTriggerError = '';
  }

  private initializeHospitalUnitSelection(): void {
    const rule = this.selectedRule;
    const unitsByHospital = new Map<number, AssociatedUnit[]>();

    if (rule && this.audienceSelectedUnitCount > 0) {
      rule.associatedUnits = rule.associatedUnits.filter(
        (unit) => this.audienceSelections.get(unit.hospitalId)?.has(unit.unitId) === true,
      );
    }

    for (const unit of rule?.associatedUnits ?? []) {
      const hospitalUnits = unitsByHospital.get(unit.hospitalId) ?? [];
      hospitalUnits.push(unit);
      unitsByHospital.set(unit.hospitalId, hospitalUnits);
    }

    this.hospitalExclusionGroups = this.audienceHospitals.map(
      (hospital) => this.createHospitalExclusionGroup(
        hospital.id,
        unitsByHospital.get(Number(hospital.id)) ?? [],
      ),
    );

    for (const group of this.hospitalExclusionGroups) {
      this.loadHospitalUnits(group);
    }
  }

  isRuleActiveUpdating(rule: ClinicalRuleAggregate): boolean {
    return this.activeUpdateRuleIds.has(rule.id);
  }

  ruleActiveUpdateError(rule: ClinicalRuleAggregate): string {
    return this.activeUpdateErrors.get(rule.id) ?? '';
  }

  isValiditySectionOpen(rule: ClinicalRuleAggregate): boolean {
    return this.expandedValidityRuleIds.has(rule.id);
  }

  toggleValiditySection(rule: ClinicalRuleAggregate): void {
    if (this.isValiditySectionOpen(rule)) {
      this.expandedValidityRuleIds.delete(rule.id);
      return;
    }
    this.expandedValidityRuleIds.add(rule.id);
  }

  setValidityMinutes(rule: ClinicalRuleAggregate, value: number | null): void {
    this.editedValidityRuleIds.add(rule.id);
    this.clearedValidityRuleIds.delete(rule.id);
    if (value == null) {
      rule.step.dependencyLimitMinutes = null;
      rule.step.hasDependencyLimit = false;
      this.configuredValidityRuleIds.delete(rule.id);
      return;
    }

    rule.step.dependencyLimitMinutes = value;
    rule.step.hasDependencyLimit = true;
    rule.step.dependencyLimitStepId = undefined;
    this.configuredValidityRuleIds.add(rule.id);
  }

  clearValidityRule(rule: ClinicalRuleAggregate): void {
    rule.step.dependencyLimitMinutes = null;
    rule.step.hasDependencyLimit = false;
    rule.step.dependencyLimitStepId = undefined;
    this.configuredValidityRuleIds.delete(rule.id);
    this.editedValidityRuleIds.add(rule.id);
    this.clearedValidityRuleIds.add(rule.id);
    this.ruleSaveError = '';
  }

  private isValidityConfigured(rule: ClinicalRuleAggregate): boolean {
    return this.configuredValidityRuleIds.has(rule.id)
      || (rule.step.dependencyLimitMinutes ?? 0) > 0
      || rule.step.hasDependencyLimit;
  }

  isHospitalFilterOpen(rule: ClinicalRuleAggregate): boolean {
    return this.expandedHospitalFilterRuleIds.has(rule.id);
  }

  toggleHospitalFilter(rule: ClinicalRuleAggregate): void {
    if (this.isHospitalFilterOpen(rule)) {
      this.expandedHospitalFilterRuleIds.delete(rule.id);
      return;
    }
    this.expandedHospitalFilterRuleIds.add(rule.id);
  }

  isRelativeDelayOpen(rule: ClinicalRuleAggregate): boolean {
    return this.expandedRelativeDelayRuleIds.has(rule.id);
  }

  toggleRelativeDelay(rule: ClinicalRuleAggregate): void {
    if (this.isRelativeDelayOpen(rule)) {
      this.expandedRelativeDelayRuleIds.delete(rule.id);
      return;
    }
    this.expandedRelativeDelayRuleIds.add(rule.id);
  }

  setRelativeDelayMinutes(rule: ClinicalRuleAggregate, value: number | null): void {
    this.editedRelativeDelayRuleIds.add(rule.id);
    this.clearedRelativeDelayRuleIds.delete(rule.id);
    if (value == null) {
      rule.step.delayFromStepMinutes = null;
      if (!rule.step.delayFromStepId) {
        this.configuredRelativeDelayRuleIds.delete(rule.id);
      }
      return;
    }

    rule.step.delayFromStepMinutes = value;
    this.configuredRelativeDelayRuleIds.add(rule.id);
  }

  clearRelativeDelayRule(rule: ClinicalRuleAggregate): void {
    rule.step.delayFromStepId = undefined;
    rule.step.delayFromStepMinutes = null;
    this.configuredRelativeDelayRuleIds.delete(rule.id);
    this.editedRelativeDelayRuleIds.add(rule.id);
    this.clearedRelativeDelayRuleIds.add(rule.id);
    this.ruleSaveError = '';
  }

  private isRelativeDelayConfigured(rule: ClinicalRuleAggregate): boolean {
    return this.configuredRelativeDelayRuleIds.has(rule.id)
      || (rule.step.delayFromStepMinutes ?? 0) > 0
      || Boolean(rule.step.delayFromStepId);
  }

  isRecurringSectionOpen(rule: ClinicalRuleAggregate): boolean {
    return this.expandedRecurringRuleIds.has(rule.id);
  }

  toggleRecurringSection(rule: ClinicalRuleAggregate): void {
    if (this.isRecurringSectionOpen(rule)) {
      this.expandedRecurringRuleIds.delete(rule.id);
      return;
    }
    this.expandedRecurringRuleIds.add(rule.id);
  }

  setRecurringIntervalDays(rule: ClinicalRuleAggregate, value: number | null): void {
    rule.trigger.recurringIntervalDays = value;
    this.markRecurringRuleEdited(rule);
  }

  setRuleTime(rule: ClinicalRuleAggregate, field: RuleTimeField, value: string): void {
    rule.trigger[field] = field === 'recurringTimeOfDay'
      ? this.formatValidTimeInput(value)
      : this.formatRuleTimeInput(value);
    if (field === 'recurringTimeOfDay') {
      this.markRecurringRuleEdited(rule);
    } else {
      this.clearedSendWindowRuleIds.delete(rule.id);
      this.editedSendWindowRuleIds.add(rule.id);
    }
  }

  clearRecurringRule(rule: ClinicalRuleAggregate): void {
    rule.trigger.isRecurring = false;
    rule.trigger.recurringIntervalDays = null;
    rule.trigger.recurringTimeOfDay = '';
    rule.trigger.recurringStopCondition = 'never';
    rule.trigger.onetimeFallbackEnabled = false;
    rule.trigger.onetimeFallbackTime = '';
    this.clearedRecurringRuleIds.add(rule.id);
    this.editedRecurringRuleIds.add(rule.id);
    this.ruleSaveError = '';
  }

  private markRecurringRuleEdited(rule: ClinicalRuleAggregate): void {
    rule.trigger.isRecurring = true;
    this.clearedRecurringRuleIds.delete(rule.id);
    this.editedRecurringRuleIds.add(rule.id);
  }

  setRecurringStopCondition(
    rule: ClinicalRuleAggregate,
    value: SmsTrigger['recurringStopCondition'],
  ): void {
    rule.trigger.recurringStopCondition = value;
    this.markRecurringRuleEdited(rule);
  }

  restrictRuleTimeKey(
    rule: ClinicalRuleAggregate,
    field: RuleTimeField,
    event: KeyboardEvent,
  ): void {
    const input = event.target as HTMLInputElement;
    const deletingAutoColon = event.key === 'Backspace'
      && input.value.endsWith(':')
      && input.selectionStart === input.value.length
      && input.selectionEnd === input.value.length;
    if (deletingAutoColon) {
      event.preventDefault();
      const updatedValue = input.value.slice(0, -2);
      input.value = updatedValue;
      rule.trigger[field] = updatedValue;
      if (field === 'recurringTimeOfDay') {
        this.markRecurringRuleEdited(rule);
      } else {
        this.editedSendWindowRuleIds.add(rule.id);
      }
      return;
    }

    const isShortcut = event.ctrlKey || event.metaKey || event.altKey;
    if (!isShortcut && event.key.length === 1 && !/^\d$/.test(event.key)) {
      event.preventDefault();
    }
  }

  isSendWindowSectionOpen(rule: ClinicalRuleAggregate): boolean {
    return this.expandedSendWindowRuleIds.has(rule.id);
  }

  toggleSendWindowSection(rule: ClinicalRuleAggregate): void {
    if (this.isSendWindowSectionOpen(rule)) {
      this.expandedSendWindowRuleIds.delete(rule.id);
      return;
    }
    this.expandedSendWindowRuleIds.add(rule.id);
    this.scrollRuleSectionIntoView(`send-window-settings-${rule.id}`);
  }

  clearSendWindowRule(rule: ClinicalRuleAggregate): void {
    rule.trigger.sendWindowStartTime = '';
    rule.trigger.sendWindowEndTime = '';
    this.clearedSendWindowRuleIds.add(rule.id);
    this.editedSendWindowRuleIds.add(rule.id);
    this.ruleSaveError = '';
  }

  private scrollRuleSectionIntoView(sectionId: string): void {
    setTimeout(() => {
      document.getElementById(sectionId)?.scrollIntoView({
        behavior: 'smooth',
        block: 'nearest',
      });
    });
  }

  private formatRuleTimeInput(value: string): string {
    const digits = value.replace(/\D/g, '').slice(0, 4);
    if (digits.length < 2) {
      return digits;
    }
    return `${digits.slice(0, 2)}:${digits.slice(2)}`;
  }

  private formatValidTimeInput(value: string): string {
    const digits = value.replace(/\D/g, '').slice(0, 4);
    if (!digits) return '';

    const firstHourDigit = Number(digits[0]);
    if (firstHourDigit > 2) return '';
    if (digits.length === 1) return digits;

    const hour = digits.slice(0, 2);
    if (Number(hour) > 23) return digits[0];

    let formatted = `${hour}:`;
    if (digits.length === 2) return formatted;

    const firstMinuteDigit = Number(digits[2]);
    if (firstMinuteDigit > 5) return formatted;

    formatted += digits[2];
    if (digits.length === 4) {
      formatted += digits[3];
    }
    return formatted;
  }

  isFallbackSectionOpen(rule: ClinicalRuleAggregate): boolean {
    return this.expandedFallbackRuleIds.has(rule.id);
  }

  toggleFallbackSection(rule: ClinicalRuleAggregate): void {
    if (this.isFallbackSectionOpen(rule)) {
      this.expandedFallbackRuleIds.delete(rule.id);
      rule.trigger.onetimeFallbackEnabled = false;
      this.editedRecurringRuleIds.add(rule.id);
      return;
    }
    this.expandedFallbackRuleIds.add(rule.id);
    rule.trigger.onetimeFallbackEnabled = true;
    this.editedRecurringRuleIds.add(rule.id);
  }

  relativeAnchorTemplateName(rule: ClinicalRuleAggregate): string {
    const selectedTemplateId = this.configuredTriggers.find(
      (trigger) => trigger.ttId === rule.step.delayFromStepId,
    )?.templateId;
    return this.templates.find(
      (template) => Number(template.id) === selectedTemplateId,
    )?.name ?? 'בחר תבנית שעליה הכלל תלוי';
  }

  relativeAnchorTemplates(rule: ClinicalRuleAggregate): Template[] {
    const currentTemplateId = this.ruleTemplateId(rule);
    const scopedCategoryId = this.categoryId ?? rule.category.categoryId;
    return this.templates.filter(
      (template) => template.isActive
        && (this.relativeAnchorTrigger(template)?.ttId ?? 0) > 0
        && Number(template.categoryId) === scopedCategoryId
        && Number(template.id) !== currentTemplateId,
    );
  }

  relativeAnchorTemplateTriggerId(template: Template): number {
    return this.relativeAnchorTrigger(template)?.ttId ?? 0;
  }

  private relativeAnchorTrigger(template: Template): SmsTriggerSettings | undefined {
    const templateId = Number(template.id);
    const activeTriggers = this.configuredTriggers.filter(
      (trigger) => trigger.templateId === templateId && trigger.triggerIsActive,
    );
    return this.selectConfiguredTrigger(activeTriggers);
  }

  setRelativeAnchorTemplate(
    rule: ClinicalRuleAggregate,
    templateTriggerId: number | '',
    dropdown: HTMLDetailsElement,
  ): void {
    rule.step.delayFromStepId = templateTriggerId || undefined;
    this.editedRelativeDelayRuleIds.add(rule.id);
    this.clearedRelativeDelayRuleIds.delete(rule.id);
    dropdown.open = false;
  }

  setRuleActive(rule: ClinicalRuleAggregate, isActive: boolean): void {
    const templateId = this.selectedApiTemplateId(rule);
    if (!templateId || this.isRuleActiveUpdating(rule) || rule.template.isActive === isActive) {
      return;
    }

    const apiTemplate = this.apiTemplateFor(rule);
    if (!apiTemplate) {
      this.activeUpdateErrors.set(rule.id, 'לא ניתן למצוא את פרטי התבנית לעדכון.');
      return;
    }
    const previousValue = rule.template.isActive;

    this.activeUpdateErrors.delete(rule.id);
    this.activeUpdateRuleIds.add(rule.id);
    rule.template.isActive = isActive;
    apiTemplate.isActive = isActive;

    this.apiService.updateTemplateActive(apiTemplate, isActive).subscribe({
      next: () => {
        this.activeUpdateRuleIds.delete(rule.id);
        apiTemplate.versionNumber += 1;
        rule.template.versionNumber = apiTemplate.versionNumber;
      },
      error: (error) => {
        rule.template.isActive = previousValue;
        apiTemplate.isActive = previousValue;
        this.activeUpdateRuleIds.delete(rule.id);
        if (!isAuthenticationError(error)) {
          this.activeUpdateErrors.set(rule.id, 'לא ניתן לעדכן את סטטוס התבנית. יש לנסות שוב.');
        }
        console.error('Failed to update template active state', error);
      },
    });
  }

  createDraftRule(updateUrl = true): void {
    const category = this.categories[0];
    if (!category || this.templates.length === 0) {
      this.showToast('לא ניתן ליצור כלל לפני שנתוני התבניות והקטגוריות נטענו מהשרת.', 'error');
      return;
    }

    const nextRuleId = Math.max(0, ...this.rules.map((rule) => rule.id)) + 1;
    const trigger = this.triggerCatalog[0];
    const draft: ClinicalRuleAggregate = {
      id: nextRuleId,
      template: {
        templateId: 0,
        templateCode: '',
        templateName: 'כלל שליחה חדש',
        categoryId: category.categoryId,
        isActive: false,
        isEditable: true,
        versionNumber: 0,
      },
      category,
      languages: [],
      associatedUnits: [],
      step: {
        categoryStepId: 0,
        categoryId: category.categoryId,
        templateId: 0,
        delayInMinutes: 0,
        delayFromStepMinutes: 0,
        hasDependencyLimit: false,
        dependencyLimitMinutes: 0,
        isActive: true,
        isEditable: true,
      },
      trigger: {
        triggerId: trigger?.triggerId ?? 0,
        templateId: 0,
        isConstant: false,
        sendType: 'Event-Based',
        triggerEventCode: trigger?.triggerCode,
        isRecurring: false,
        recurringIntervalDays: null,
        recurringTimeOfDay: '',
        sendWindowStartTime: '',
        sendWindowEndTime: '',
        recurringStopCondition: 'never',
        onetimeFallbackEnabled: false,
        onetimeFallbackTime: '',
      },
    };
    this.clinicalRulesStore.add(draft);
    this.isCreatingRule = true;
    this.selectedRuleId = draft.id;
    this.ruleSaveLoading = false;
    this.ruleSaveError = '';
    this.resetTemplateClinicalTrigger();
    this.resetRuleEditState(draft);
    this.initializeHospitalUnitSelection();
    this.clinicalRulesStore.selectedTemplateId = draft.template.templateId;
    this.drawerOpen = true;

    if (updateUrl) {
      this.location.go(this.withScope('/rules/create'));
    }
  }

  private withScope(path: string): string {
    return this.projectId && this.categoryId
      ? `${path}?projectId=${this.projectId}&categoryId=${this.categoryId}`
      : path;
  }

  saveSelectedRule(): void {
    const rule = this.selectedRule;
    if (!rule || this.ruleSaveLoading) return;

    const isNewRule = this.isCreatingRule;

    const templateId = this.ruleTemplateId(rule);
    if (!templateId) {
      this.showRuleSaveError('לא ניתן לשמור את ההחרגות ללא תבנית תקינה.');
      return;
    }

    const validityConfigured = this.isValidityConfigured(rule);
    const relativeDelayConfigured = this.isRelativeDelayConfigured(rule);
    const recurringConfigured = rule.trigger.isRecurring;
    const validityCleared = this.clearedValidityRuleIds.has(rule.id);
    const recurringCleared = this.clearedRecurringRuleIds.has(rule.id);
    const sendWindowCleared = this.clearedSendWindowRuleIds.has(rule.id);
    const validityEdited = this.editedValidityRuleIds.has(rule.id);
    const relativeDelayEdited = this.editedRelativeDelayRuleIds.has(rule.id);
    const recurringEdited = this.editedRecurringRuleIds.has(rule.id);
    const sendWindowEdited = this.editedSendWindowRuleIds.has(rule.id);
    const configuredRule = this.configuredTriggerFor(rule);
    const existingRuleId = configuredRule?.ruleId ?? null;
    const ttId = Number(
      configuredRule?.ttId ?? this.selectedTemplateClinicalTriggers[0]?.ttId,
    );

    const validityMinutesInput = rule.step.dependencyLimitMinutes as number | null;
    const maxTimeTokefInMinutes = Number(validityMinutesInput);
    if (
      validityEdited
      && !validityCleared
      && (
        validityMinutesInput == null
        || !Number.isInteger(maxTimeTokefInMinutes)
        || maxTimeTokefInMinutes < MIN_VALIDITY_MINUTES
        || maxTimeTokefInMinutes > MAX_VALIDITY_MINUTES
      )
    ) {
      this.showRuleSaveError('יש להזין מספר שלם בין 1 ל־60 דקות בכלל 3.');
      return;
    }

    const dependencyMinutesInput = rule.step.delayFromStepMinutes as number | null;
    const dependencyMaxMinutes = Number(dependencyMinutesInput);
    if (
      !rule.step.delayFromStepId
      || dependencyMinutesInput == null
      || !Number.isFinite(dependencyMaxMinutes)
      || dependencyMaxMinutes < 0
      || dependencyMaxMinutes > MAX_RELATIVE_DELAY_MINUTES
    ) {
      this.showRuleSaveError('כלל 4 הוא שדה חובה: יש לבחור תבנית ולהזין מספר דקות בין 0 ל־60.');
      return;
    }

    const recurringIntervalDaysInput = rule.trigger.recurringIntervalDays as number | null;
    const recurringIntervalDays = Number(recurringIntervalDaysInput);
    const recurringSendTime = rule.trigger.recurringTimeOfDay?.trim() ?? '';
    const recurringStopCondition = rule.trigger.recurringStopCondition?.trim() ?? '';
    const recurringStopConditionForApi = recurringStopCondition === 'discharged'
      || recurringStopCondition === 'never'
      ? recurringStopCondition
      : null;
    const timePattern = /^(?:[01]\d|2[0-3]):[0-5]\d$/;
    const recurringTimeIsValid = timePattern.test(recurringSendTime);
    const recurringStopConditionIsValid = recurringStopConditionForApi !== null;
    if (
      recurringEdited
      && !recurringCleared
      && (
        recurringIntervalDaysInput == null
        || !Number.isInteger(recurringIntervalDays)
        || recurringIntervalDays <= 0
        || !recurringTimeIsValid
        || !recurringStopConditionIsValid
      )
    ) {
      this.showRuleSaveError('יש למלא מספר ימים שלם וחיובי, שעה תקינה בין 00:00 ל־23:59 ותנאי עצירה בכלל 5.');
      return;
    }

    const sendWindowStartTime = rule.trigger.sendWindowStartTime?.trim() ?? '';
    const sendWindowEndTime = rule.trigger.sendWindowEndTime?.trim() ?? '';
    if (
      sendWindowEdited
      && !sendWindowCleared
      && (!timePattern.test(sendWindowStartTime) || !timePattern.test(sendWindowEndTime))
    ) {
      this.showRuleSaveError('יש למלא שעות תקינות בשדות "משעה" ו"עד שעה" בכלל 6.');
      return;
    }

    if (this.audienceSelectedUnitCount > 0) {
      this.syncHospitalExclusions(rule);
    }
    this.ruleSaveLoading = true;
    this.ruleSaveError = '';

    const hospitalExclusionSignature = this.hospitalExclusionSignature(rule.associatedUnits);
    const savedHospitalExclusionSignature =
      this.savedHospitalExclusionSignatures.get(rule.id) ?? '';
    const hospitalExclusionsChanged = hospitalExclusionSignature
      !== savedHospitalExclusionSignature;
    const hospitalExclusionRequest = {
      templateId,
      items: rule.associatedUnits.map(({ hospitalId, unitId }) => ({ hospitalId, unitId })),
    };
    const saveHospitalExclusions$: Observable<unknown> = hospitalExclusionsChanged
      ? (savedHospitalExclusionSignature
        ? this.apiService.replaceRejectHospitalUnits(hospitalExclusionRequest)
        : this.apiService.createRejectHospitalUnits(hospitalExclusionRequest))
      : of(null);

    const createSmsRuleRequest: CreateSmsRuleRequest = {
      ttId,
      dependOnTtId: rule.step.delayFromStepId ?? null,
      dependencyMaxMinutes: relativeDelayConfigured ? dependencyMaxMinutes : 0,
      maxTimeTokefInMinutes: validityConfigured ? maxTimeTokefInMinutes : null,
      isRecurring: recurringConfigured,
    };

    if (recurringConfigured) {
      createSmsRuleRequest.recurringIntervalDays = recurringIntervalDays;
      createSmsRuleRequest.recurringTimeOfDay = recurringSendTime;
      createSmsRuleRequest.recurringStopCondition = recurringStopConditionForApi;
      createSmsRuleRequest.onetimeFallbackEnabled = rule.trigger.onetimeFallbackEnabled;
      createSmsRuleRequest.onetimeFallbackTime = rule.trigger.onetimeFallbackEnabled
        ? rule.trigger.onetimeFallbackTime.trim()
        : null;
    }

    if (sendWindowEdited && !sendWindowCleared) {
      createSmsRuleRequest.startTimeRange = sendWindowStartTime;
      createSmsRuleRequest.endTimeRange = sendWindowEndTime;
    }

    const updateSmsRuleRequest: UpdateSmsRuleRequest = { ttId };
    if (configuredRule) {
      if (validityEdited) {
        const currentMaxTimeTokef = validityConfigured ? maxTimeTokefInMinutes : null;
        if (currentMaxTimeTokef !== configuredRule.maxTimeTokefInMinutes) {
          updateSmsRuleRequest.maxTimeTokefInMinutes = currentMaxTimeTokef;
        }
      }

      if (relativeDelayEdited) {
        const currentDependOnTtId = rule.step.delayFromStepId ?? null;
        const currentDependencyMaxMinutes = relativeDelayConfigured ? dependencyMaxMinutes : 0;
        if (
          currentDependOnTtId !== configuredRule.dependOnTtId
          || currentDependencyMaxMinutes !== (configuredRule.dependencyMaxMinutes ?? 0)
        ) {
          updateSmsRuleRequest.dependOnTtId = currentDependOnTtId;
          updateSmsRuleRequest.dependencyMaxMinutes = currentDependencyMaxMinutes;
        }
      }

      if (recurringEdited) {
        const wasRecurring = configuredRule.isRecurring === true;
        if (recurringCleared) {
          updateSmsRuleRequest.isRecurring = false;
          updateSmsRuleRequest.recurringIntervalDays = null;
          updateSmsRuleRequest.recurringTimeOfDay = null;
          updateSmsRuleRequest.recurringStopCondition = null;
          updateSmsRuleRequest.onetimeFallbackEnabled = false;
          updateSmsRuleRequest.onetimeFallbackTime = null;
        } else if (recurringConfigured !== wasRecurring) {
          updateSmsRuleRequest.isRecurring = recurringConfigured;
        }

        if (recurringConfigured && !recurringCleared) {
          if (recurringIntervalDays !== configuredRule.recurringIntervalDays) {
            updateSmsRuleRequest.recurringIntervalDays = recurringIntervalDays;
          }
          if (recurringSendTime !== (configuredRule.recurringTimeOfDay ?? '')) {
            updateSmsRuleRequest.recurringTimeOfDay = recurringSendTime;
          }
          if (recurringStopConditionForApi !== configuredRule.recurringStopCondition) {
            updateSmsRuleRequest.recurringStopCondition = recurringStopConditionForApi;
          }

          const fallbackEnabled = rule.trigger.onetimeFallbackEnabled;
          const fallbackTime = fallbackEnabled ? rule.trigger.onetimeFallbackTime.trim() : null;
          if (fallbackEnabled !== (configuredRule.onetimeFallbackEnabled === true)) {
            updateSmsRuleRequest.onetimeFallbackEnabled = fallbackEnabled;
          }
          if (fallbackTime !== configuredRule.onetimeFallbackTime) {
            updateSmsRuleRequest.onetimeFallbackTime = fallbackTime;
          }
        }
      }

      if (sendWindowEdited) {
        const currentSendWindowStartTime = sendWindowCleared ? null : sendWindowStartTime;
        const currentSendWindowEndTime = sendWindowCleared ? null : sendWindowEndTime;
        if (
          currentSendWindowStartTime !== (configuredRule.startTimeRange ?? null)
          || currentSendWindowEndTime !== (configuredRule.endTimeRange ?? null)
        ) {
          updateSmsRuleRequest.startTimeRange = currentSendWindowStartTime;
          updateSmsRuleRequest.endTimeRange = currentSendWindowEndTime;
        }
      }
    }

    const hasPartialUpdate = Object.keys(updateSmsRuleRequest).length > 1;
    const shouldCreateSmsRule = existingRuleId === null
      && (
        (validityEdited && validityConfigured)
        || (relativeDelayEdited && relativeDelayConfigured)
        || (recurringEdited && recurringConfigured)
        || (sendWindowEdited && !sendWindowCleared)
      );
    const shouldUpdateSmsRule = existingRuleId !== null && hasPartialUpdate;
    const shouldSaveSmsRule = shouldCreateSmsRule || shouldUpdateSmsRule;
    if (shouldSaveSmsRule && (!Number.isInteger(ttId) || ttId <= 0)) {
      this.ruleSaveLoading = false;
      this.showRuleSaveError('לא ניתן לשמור: הבקאנד לא החזיר ttId עבור התבנית שנבחרה.');
      return;
    }

    if (!hospitalExclusionsChanged && !shouldSaveSmsRule) {
      this.ruleSaveLoading = false;
      this.closeDrawer();
      return;
    }

    const smsRuleRequest = existingRuleId === null
      ? createSmsRuleRequest
      : updateSmsRuleRequest;

    saveHospitalExclusions$.pipe(
      catchError((error) => throwError(() => ({
        stage: 'hospitalExclusions',
        error,
      } satisfies RuleSaveFailure))),
      switchMap(() => shouldSaveSmsRule
        ? (existingRuleId
          ? this.apiService.updateSmsRule(existingRuleId, smsRuleRequest)
          : this.apiService.createSmsRule(createSmsRuleRequest)).pipe(
          catchError((error) => throwError(() => ({
            stage: 'smsRule',
            error,
          } satisfies RuleSaveFailure))),
        )
        : of(null)),
    ).subscribe({
      next: (savedRuleResponse) => {
        const savedRuleId = existingRuleId ?? this.ruleIdFromSaveResponse(savedRuleResponse);
        if (configuredRule && savedRuleId) {
          configuredRule.ruleId = savedRuleId;
          if (smsRuleRequest.dependOnTtId !== undefined) {
            configuredRule.dependOnTtId = smsRuleRequest.dependOnTtId;
          }
          if (smsRuleRequest.dependencyMaxMinutes !== undefined) {
            configuredRule.dependencyMaxMinutes = smsRuleRequest.dependencyMaxMinutes ?? null;
          }
          if (smsRuleRequest.maxTimeTokefInMinutes !== undefined) {
            configuredRule.maxTimeTokefInMinutes = smsRuleRequest.maxTimeTokefInMinutes;
          }
          if (smsRuleRequest.isRecurring !== undefined) {
            configuredRule.isRecurring = smsRuleRequest.isRecurring;
          }
          if (smsRuleRequest.recurringIntervalDays !== undefined) {
            configuredRule.recurringIntervalDays = smsRuleRequest.recurringIntervalDays;
          }
          if (smsRuleRequest.recurringTimeOfDay !== undefined) {
            configuredRule.recurringTimeOfDay = smsRuleRequest.recurringTimeOfDay;
          }
          if (smsRuleRequest.startTimeRange !== undefined) {
            configuredRule.startTimeRange = smsRuleRequest.startTimeRange;
          }
          if (smsRuleRequest.endTimeRange !== undefined) {
            configuredRule.endTimeRange = smsRuleRequest.endTimeRange;
          }
          if (smsRuleRequest.recurringStopCondition !== undefined) {
            configuredRule.recurringStopCondition = smsRuleRequest.recurringStopCondition;
          }
          if (smsRuleRequest.onetimeFallbackEnabled !== undefined) {
            configuredRule.onetimeFallbackEnabled = smsRuleRequest.onetimeFallbackEnabled;
          }
          if (smsRuleRequest.onetimeFallbackTime !== undefined) {
            configuredRule.onetimeFallbackTime = smsRuleRequest.onetimeFallbackTime;
          }
          configuredRule.updateDate = new Date().toISOString();
        }

        rule.template.versionNumber += 1;

        if (isNewRule) {
          const existingAggregate = this.rules.find(
            (candidate) => candidate.id !== rule.id && this.ruleTemplateId(candidate) === templateId,
          );
          if (existingAggregate) {
            this.applySavedDraftToRule(rule, existingAggregate);
            this.savedHospitalExclusionSignatures.set(
              existingAggregate.id,
              hospitalExclusionSignature,
            );
          } else {
            this.isCreatingRule = false;
          }
        }

        this.savedHospitalExclusionSignatures.set(rule.id, hospitalExclusionSignature);
        this.resetRuleEditState(rule);
        this.ruleSaveLoading = false;
        this.closeDrawer();
        if (isNewRule && !existingRuleId) {
          this.showToast('הכלל החדש נוצר בהצלחה.', 'success');
        } else {
          this.showToast('הכלל עודכן בהצלחה.', 'success');
        }
      },
      error: (failure: RuleSaveFailure) => {
        this.ruleSaveLoading = false;
        if (!isAuthenticationError(failure.error)) {
          const message = failure.stage === 'smsRule'
            ? this.smsRuleSaveError(failure.error)
            : this.rejectHospitalUnitsSaveError(failure.error.status);
          this.showRuleSaveError(message);
        }
        console.error(
          `Failed to save ${failure.stage} for template ${templateId}`,
          failure.error,
        );
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

  private showRuleSaveError(message: string): void {
    this.ruleSaveError = message;
    this.showToast(message, 'error');
  }

  private ruleIdFromSaveResponse(response: unknown): number | null {
    if (typeof response !== 'object' || response === null || !('ruleId' in response)) {
      return null;
    }
    const ruleId = Number(response.ruleId);
    return Number.isInteger(ruleId) && ruleId > 0 ? ruleId : null;
  }

  private applySavedDraftToRule(
    draft: ClinicalRuleAggregate,
    target: ClinicalRuleAggregate,
  ): void {
    target.template = { ...draft.template };
    target.category = draft.category;
    target.associatedUnits = draft.associatedUnits.map((unit) => ({ ...unit }));
    target.step = {
      ...draft.step,
      categoryStepId: target.step.categoryStepId,
    };
    target.trigger = { ...draft.trigger };
  }

  private showToast(message: string, tone: 'success' | 'error'): void {
    this.dismissToast();
    this.toastMessage = message;
    this.toastTone = tone;
    this.toastTimer = setTimeout(() => this.dismissToast(), 4000);
  }

  private smsRuleSaveError(error: RuleSaveHttpError): string {
    const responseStatus = typeof error.error === 'object' && error.error !== null
      ? error.error.status
      : undefined;
    const status = error.status ?? responseStatus;

    if (status === 409) {
      return 'לתבנית שנבחרה כבר קיים כלל שליחה, לא ניתן ליצור כלל נוסף יש לערוך את הכלל הקיים.';
    }
    if (status === 400) {
      return 'אחד מהערכים בכלל אינו תקין או אינו תואם לחוזה של הבקאנד.';
    }
    if (status === 404) {
      return 'ה־endpoint לשמירת כלל התוקף לא זמין. יש להפעיל מחדש את הבקאנד.';
    }
    return 'לא ניתן לשמור את כלל התוקף. יש לנסות שוב.';
  }

  private rejectHospitalUnitsSaveError(status?: number): string {
    if (status === 400) return 'פרטי בית החולים או המחלקה אינם תקינים.';
    if (status === 404) return 'אחת מהמחלקות שנבחרו אינה קיימת בבית החולים.';
    return 'לא ניתן לשמור את החרגות המחלקות. יש לנסות שוב.';
  }

  clearHospitalExclusions(rule: ClinicalRuleAggregate): void {
    for (const group of this.hospitalExclusionGroups) {
      group.loadSequence += 1;
    }
    this.hospitalExclusionGroups = [];
    rule.associatedUnits = [];
    this.ruleSaveError = '';
    this.initializeHospitalUnitSelection();
  }

  setHospitalUnitExcluded(
    rule: ClinicalRuleAggregate,
    group: HospitalExclusionGroup,
    unitId: number,
    isExcluded: boolean,
  ): void {
    if (isExcluded && !group.selectedUnitIds.includes(unitId)) {
      group.selectedUnitIds = [...group.selectedUnitIds, unitId];
    } else if (!isExcluded) {
      group.selectedUnitIds = group.selectedUnitIds.filter((id) => id !== unitId);
    }
    this.syncHospitalExclusions(rule);
  }

  setHospitalExcluded(
    rule: ClinicalRuleAggregate,
    group: HospitalExclusionGroup,
    isExcluded: boolean,
  ): void {
    group.selectedUnitIds = isExcluded
      ? group.units.map((unit) => unit.unitId)
      : [];
    this.syncHospitalExclusions(rule);
  }

  isHospitalFullyExcluded(group: HospitalExclusionGroup): boolean {
    const audienceUnitIds = this.audienceSelections.get(Number(group.hospitalId));
    return Boolean(audienceUnitIds?.size)
      && [...(audienceUnitIds ?? [])].every(
        (unitId) => group.selectedUnitIds.includes(unitId),
      );
  }

  isHospitalPartiallyExcluded(group: HospitalExclusionGroup): boolean {
    return group.selectedUnitIds.length > 0 && !this.isHospitalFullyExcluded(group);
  }

  isHospitalUnitExcluded(group: HospitalExclusionGroup, unitId: number): boolean {
    return group.selectedUnitIds.includes(unitId);
  }

  hospitalUnitSelectionLabel(group: HospitalExclusionGroup): string {
    if (!group.hospitalId) return 'יש לבחור בית חולים תחילה';
    if (group.loading) return 'טוען מחלקות...';
    if (group.units.length === 0) return 'לא נמצאו מחלקות';
    if (group.selectedUnitIds.length === 0) return 'בחר מחלקות להחרגה';
    if (this.isHospitalFullyExcluded(group)) return 'כל המחלקות שנבחרו מוחרגות';
    if (group.selectedUnitIds.length === 1) {
      const selectedUnit = group.units.find(
        (unit) => unit.unitId === group.selectedUnitIds[0],
      );
      return selectedUnit
        ? `${selectedUnit.unitName} (${selectedUnit.unitId})`
        : 'מחלקה אחת נבחרה';
    }
    return `${group.selectedUnitIds.length} מחלקות נבחרו`;
  }

  private createHospitalExclusionGroup(
    hospitalId = '',
    selectedUnits: AssociatedUnit[] = [],
  ): HospitalExclusionGroup {
    return {
      id: this.nextHospitalExclusionGroupId++,
      hospitalId,
      selectedUnitIds: selectedUnits.map((unit) => unit.unitId),
      units: selectedUnits.map((unit) => ({
        hospitalId: unit.hospitalId,
        unitId: unit.unitId,
        unitName: unit.unitName,
        isActive: true,
      })),
      loading: false,
      error: '',
      loadSequence: 0,
    };
  }

  private syncHospitalExclusions(rule: ClinicalRuleAggregate): void {
    rule.associatedUnits = this.hospitalExclusionGroups.flatMap((group) => {
      const hospitalId = Number(group.hospitalId);
      if (!Number.isInteger(hospitalId) || hospitalId <= 0) return [];

      return group.selectedUnitIds.flatMap((unitId) => {
        const unit = group.units.find((candidate) => candidate.unitId === unitId);
        if (!unit) return [];
        return [{
          hospitalId,
          unitId,
          hospitalName: this.hospitalName(hospitalId),
          unitName: unit.unitName,
        }];
      });
    });
  }

  private hospitalExclusionSignature(units: AssociatedUnit[]): string {
    return units
      .map(({ hospitalId, unitId }) => `${hospitalId}:${unitId}`)
      .sort()
      .join('|');
  }

  ruleUnitsPreview(rule: ClinicalRuleAggregate): string {
    return rule.associatedUnits.length === 0
      ? 'ללא החרגות'
      : rule.associatedUnits.map((unit) => unit.unitName).join(', ');
  }

  ruleDescription(rule: ClinicalRuleAggregate): string {
    return this.triggerCatalogItem(rule)?.description ??
      `חוק משלוח מסוג ${this.sendTypeLabel(rule.trigger.sendType)} עבור ${rule.category.categoryName}.`;
  }

  ruleTriggerLabel(rule: ClinicalRuleAggregate): string {
    return this.triggerCatalogItem(rule)?.triggerName ?? rule.trigger.triggerEventCode ?? 'טריגר לא מוגדר';
  }

  ruleScheduleLabel(rule: ClinicalRuleAggregate): string {
    if (rule.trigger.isRecurring) return `מחזורי · כל ${rule.trigger.recurringIntervalDays} ימים`;
    if (rule.step.delayFromStepId) {
      return `${rule.step.delayFromStepMinutes} דק׳ אחרי ${this.relativeAnchorTemplateName(rule)}`;
    }
    return rule.step.delayInMinutes === 0 ? 'מיידי' : `${rule.step.delayInMinutes} דק׳ מהאירוע`;
  }

  ruleDepartmentLabel(rule: ClinicalRuleAggregate): string {
    if (rule.associatedUnits.length === 0) return 'ללא מחלקות מוחרגות';
    if (rule.associatedUnits.length <= 2) {
      return rule.associatedUnits.map((unit) => unit.unitName).join(', ');
    }
    return `${rule.associatedUnits.length} מחלקות מוחרגות`;
  }

  templateName(templateId?: number): string {
    if (!templateId) return 'מערכתי';
    return this.templates.find((template) => Number(template.id) === templateId)?.name
      ?? this.clinicalRulesStore.findByTemplateId(templateId)?.template.templateName
      ?? 'לא ידוע';
  }

  hospitalName(hospitalId: number | string): string {
    const numericHospitalId = Number(hospitalId);
    return this.hospitals.find((hospital) => Number(hospital.id) === numericHospitalId)?.name
      ?? 'לא ידוע';
  }

  sendTypeLabel(sendType: string): string {
    return sendType === 'Time-Based' ? 'מבוסס זמן' : 'מבוסס אירוע';
  }

  recurringStopLabel(condition: string): string {
    const labels: Record<string, string> = {
      discharged: 'עצירה בשחרור המטופל',
      never: 'ללא עצירה אוטומטית',
    };
    return labels[condition] ?? condition;
  }
}
