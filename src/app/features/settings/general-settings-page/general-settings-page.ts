import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';

import { ApiService } from '../../../api.service';
import { SmsUnitCategoryDetails } from '../settings.models';

@Component({
  selector: 'app-general-settings-page',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './general-settings-page.html',
  styleUrl: './general-settings-page.scss',
})
export class GeneralSettingsPage implements OnInit {
  private readonly apiService = inject(ApiService);
  private readonly route = inject(ActivatedRoute);

  rows: SmsUnitCategoryDetails[] = [];
  tableFilter = '';
  showCurrentCategoryOnly = false;
  readonly pageSizeOptions = [25, 50, 100];
  pageSize = 25;
  currentPage = 1;
  currentProjectId: number | null = null;
  currentCategoryId: number | null = null;
  loading = false;
  loadError = '';
  readonly updatingIds = new Set<number>();
  readonly updateErrors = new Map<number, string>();

  get filteredRows(): SmsUnitCategoryDetails[] {
    const terms = this.tableFilter
      .trim()
      .toLocaleLowerCase('he')
      .split(/\s+/)
      .filter(Boolean);
    return this.rows.filter((row) => {
      if (this.showCurrentCategoryOnly && !this.isCurrentContextRow(row)) {
        return false;
      }
      if (terms.length === 0) return true;

      const searchableText = [
        row.ucId,
        row.projectId,
        row.projectName,
        row.categoryId,
        row.categoryName,
        row.hospitalId,
        row.hospitalName,
        row.smsUnitId,
        row.unitId,
        row.unitName,
        row.isActive ? 'פעיל true' : 'לא פעיל false',
      ].join(' ').toLocaleLowerCase('he');

      return terms.every((term) => searchableText.includes(term));
    });
  }

  get paginatedRows(): SmsUnitCategoryDetails[] {
    const startIndex = (this.currentPage - 1) * this.pageSize;
    return this.filteredRows.slice(startIndex, startIndex + this.pageSize);
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.filteredRows.length / this.pageSize));
  }

  get firstVisibleRow(): number {
    return this.filteredRows.length === 0 ? 0 : (this.currentPage - 1) * this.pageSize + 1;
  }

  get lastVisibleRow(): number {
    return Math.min(this.currentPage * this.pageSize, this.filteredRows.length);
  }

  get visiblePageNumbers(): number[] {
    const visibleCount = Math.min(5, this.totalPages);
    const firstPage = Math.max(
      1,
      Math.min(this.currentPage - Math.floor(visibleCount / 2), this.totalPages - visibleCount + 1),
    );
    return Array.from({ length: visibleCount }, (_, index) => firstPage + index);
  }

  get hasCurrentContext(): boolean {
    return this.currentProjectId != null && this.currentCategoryId != null;
  }

  get currentProjectName(): string {
    return this.currentContextRows[0]?.projectName ?? `פרויקט ${this.currentProjectId}`;
  }

  get currentCategoryName(): string {
    return this.currentContextRows[0]?.categoryName ?? `קטגוריה ${this.currentCategoryId}`;
  }

  get currentContextRowCount(): number {
    return this.currentContextRows.length;
  }

  private get currentContextRows(): SmsUnitCategoryDetails[] {
    return this.rows.filter((row) => this.isCurrentContextRow(row));
  }

  updateTableFilter(value: string): void {
    this.tableFilter = value;
    this.currentPage = 1;
  }

  updateCurrentCategoryFilter(showOnlyCurrent: boolean): void {
    this.showCurrentCategoryOnly = showOnlyCurrent;
    this.currentPage = 1;
  }

  updatePageSize(value: number | string): void {
    const pageSize = Number(value);
    if (!this.pageSizeOptions.includes(pageSize)) return;

    this.pageSize = pageSize;
    this.currentPage = 1;
  }

  goToPage(page: number): void {
    if (!Number.isInteger(page)) return;
    this.currentPage = Math.min(Math.max(page, 1), this.totalPages);
  }

  ngOnInit(): void {
    this.readCurrentContext();
    this.loadSettings();
  }

  isCurrentContextRow(row: SmsUnitCategoryDetails): boolean {
    return this.hasCurrentContext
      && row.projectId === this.currentProjectId
      && row.categoryId === this.currentCategoryId;
  }

  private readCurrentContext(): void {
    const projectId = Number(this.route.snapshot.queryParamMap.get('projectId'));
    const categoryId = Number(this.route.snapshot.queryParamMap.get('categoryId'));

    this.currentProjectId = Number.isInteger(projectId) && projectId > 0 ? projectId : null;
    this.currentCategoryId = Number.isInteger(categoryId) && categoryId > 0 ? categoryId : null;
  }

  loadSettings(): void {
    this.loading = true;
    this.loadError = '';
    this.updateErrors.clear();

    this.apiService.getSmsUnitCategoryDetails().subscribe({
      next: (rows) => {
        this.rows = [...rows].sort((first, second) => first.ucId - second.ucId);
        this.currentPage = 1;
        this.loading = false;
      },
      error: (error) => {
        this.rows = [];
        this.loading = false;
        this.loadError = 'לא ניתן לטעון את שיוכי היחידות לקטגוריות מהשרת.';
        console.error('Failed to load unit-category settings', error);
      },
    });
  }

  toggleActive(row: SmsUnitCategoryDetails): void {
    if (this.updatingIds.has(row.ucId)) return;

    const nextStatus = !row.isActive;
    this.updatingIds.add(row.ucId);
    this.updateErrors.delete(row.ucId);

    this.apiService.updateSmsUnitCategoryStatus(row.ucId, nextStatus).subscribe({
      next: () => {
        row.isActive = nextStatus;
        this.goToPage(this.currentPage);
        this.updatingIds.delete(row.ucId);
      },
      error: (error) => {
        this.updatingIds.delete(row.ucId);
        this.updateErrors.set(row.ucId, 'שמירת הסטטוס נכשלה. נסו שוב.');
        console.error(`Failed to update unit-category ${row.ucId}`, error);
      },
    });
  }

}
