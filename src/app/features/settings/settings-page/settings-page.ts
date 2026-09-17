import { Component, OnInit, inject } from '@angular/core';
import { Hospital, Placeholder as ApiPlaceholder } from '../../../../types';
import { ApiService } from '../../../api.service';
import { SmsTriggerSettings } from '../../routes/route.models';
import { schemaTables } from '../schema-tables.data';
import {
  HospitalUnit,
  SmsCategory,
  TriggerCatalogItem,
} from '../settings.models';

@Component({
  selector: 'app-settings-page',
  standalone: true,
  templateUrl: './settings-page.html',
  styleUrl: './settings-page.scss',
})
export class SettingsPage implements OnInit {
  private readonly apiService = inject(ApiService);

  hospitals: Hospital[] = [];
  hospitalsLoading = false;
  hospitalsError = '';
  units: HospitalUnit[] = [];
  unitsLoading = false;
  unitsError = '';
  triggerCatalog: TriggerCatalogItem[] = [];
  triggersLoading = false;
  triggersError = '';
  placeholders: ApiPlaceholder[] = [];
  placeholdersLoading = false;
  placeholdersError = '';
  categories: SmsCategory[] = [];
  categoriesLoading = false;
  categoriesError = '';
  smsRules: SmsTriggerSettings[] = [];
  smsRulesLoading = false;
  smsRulesError = '';
  readonly schemaTables = schemaTables;

  ngOnInit(): void {
    this.loadHospitals();
    this.loadUnits();
    this.loadTriggerCatalog();
    this.loadPlaceholders();
    this.loadCategories();
    this.loadSmsRules();
  }

  private loadHospitals(): void {
    this.hospitalsLoading = true;
    this.hospitalsError = '';

    this.apiService.getHospitals().subscribe({
      next: (hospitals) => {
        this.hospitals = hospitals;
        this.hospitalsLoading = false;
      },
      error: (error) => {
        this.hospitalsLoading = false;
        this.hospitalsError = 'לא ניתן לטעון את בתי החולים מהשרת.';
        console.error('Failed to load hospitals', error);
      },
    });
  }

  private loadUnits(): void {
    this.unitsLoading = true;
    this.unitsError = '';

    this.apiService.getUnitsCatalog().subscribe({
      next: (units) => {
        this.units = units.filter((unit) => unit.isActive);
        this.unitsLoading = false;
      },
      error: (error) => {
        this.unitsLoading = false;
        this.unitsError = 'לא ניתן לטעון את היחידות מהשרת.';
        console.error('Failed to load units', error);
      },
    });
  }

  private loadTriggerCatalog(): void {
    this.triggersLoading = true;
    this.triggersError = '';

    this.apiService.getTriggerCatalog().subscribe({
      next: (triggers) => {
        this.triggerCatalog = triggers;
        this.triggersLoading = false;
      },
      error: (error) => {
        this.triggersLoading = false;
        this.triggersError = 'לא ניתן לטעון את הטריגרים מהשרת.';
        console.error('Failed to load trigger catalog', error);
      },
    });
  }

  private loadPlaceholders(): void {
    this.placeholdersLoading = true;
    this.placeholdersError = '';

    this.apiService.getPlaceholders().subscribe({
      next: (placeholders) => {
        this.placeholders = placeholders;
        this.placeholdersLoading = false;
      },
      error: (error) => {
        this.placeholders = [];
        this.placeholdersLoading = false;
        this.placeholdersError = 'לא ניתן לטעון את השדות הדינמיים מהשרת.';
        console.error('Failed to load placeholders', error);
      },
    });
  }

  private loadCategories(): void {
    this.categoriesLoading = true;
    this.categoriesError = '';

    this.apiService.getCategories().subscribe({
      next: (categories) => {
        this.categories = categories;
        this.categoriesLoading = false;
      },
      error: (error) => {
        this.categoriesLoading = false;
        this.categoriesError = 'לא ניתן לטעון את הקטגוריות מהשרת.';
        console.error('Failed to load categories', error);
      },
    });
  }

  private loadSmsRules(): void {
    this.smsRulesLoading = true;
    this.smsRulesError = '';

    this.apiService.getTriggers().subscribe({
      next: (triggers) => {
        this.smsRules = triggers.filter((trigger) => trigger.ruleId != null);
        this.smsRulesLoading = false;
      },
      error: (error) => {
        this.smsRules = [];
        this.smsRulesLoading = false;
        this.smsRulesError = 'לא ניתן לטעון את כללי השליחה מהשרת.';
        console.error('Failed to load SMS rules', error);
      },
    });
  }

  activeUnitsCount(): number {
    return this.units.filter((unit) => unit.isActive).length;
  }

  hospitalName(hospitalId: number): string {
    return this.hospitals.find((hospital) => Number(hospital.id) === hospitalId)?.name
      ?? 'בית חולים לא נמצא';
  }

  activeTriggerCount(): number {
    return this.triggerCatalog.filter((trigger) => trigger.isActive).length;
  }

}
