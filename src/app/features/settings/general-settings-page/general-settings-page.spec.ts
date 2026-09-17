import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { of } from 'rxjs';

import { ApiService } from '../../../api.service';
import { GeneralSettingsPage } from './general-settings-page';

describe('GeneralSettingsPage', () => {
  let fixture: ComponentFixture<GeneralSettingsPage>;
  let apiService: jasmine.SpyObj<ApiService>;

  beforeEach(async () => {
    apiService = jasmine.createSpyObj<ApiService>('ApiService', [
      'getSmsUnitCategoryDetails',
      'updateSmsUnitCategoryStatus',
    ]);
    apiService.getSmsUnitCategoryDetails.and.returnValue(of([
      {
        ucId: 1,
        projectId: 2,
        projectName: 'אשפוז',
        categoryId: 5,
        categoryName: 'שחרור',
        hospitalId: 20,
        hospitalName: 'בילינסון',
        smsUnitId: 101,
        unitId: 1549947,
        unitName: 'פנימית א',
        isActive: true,
      },
      {
        ucId: 2,
        projectId: null,
        projectName: null,
        categoryId: 3,
        categoryName: null,
        hospitalId: null,
        hospitalName: null,
        smsUnitId: 102,
        unitId: null,
        unitName: null,
        isActive: false,
      },
    ]));
    apiService.updateSmsUnitCategoryStatus.and.returnValue(of(undefined));

    await TestBed.configureTestingModule({
      imports: [GeneralSettingsPage],
      providers: [
        { provide: ApiService, useValue: apiService },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              queryParamMap: convertToParamMap({ projectId: '2', categoryId: '5' }),
            },
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(GeneralSettingsPage);
    fixture.detectChanges();
  });

  it('displays the details returned by the single endpoint', () => {
    const rows = fixture.nativeElement.querySelectorAll('tbody tr');
    const text = rows[0].textContent as string;

    expect(rows.length).toBe(2);
    expect(text).toContain('1');
    expect(text).toContain('אשפוז');
    expect(text).toContain('שחרור');
    expect(text).toContain('בילינסון');
    expect(text).toContain('פנימית א');
    expect(apiService.getSmsUnitCategoryDetails).toHaveBeenCalledTimes(1);
  });

  it('displays Hebrew headers and the unit ID supplied by the view', () => {
    const headers = Array.from(
      fixture.nativeElement.querySelectorAll('thead th') as NodeListOf<HTMLElement>,
    ).map((header) => header.textContent?.trim());
    const firstRowCells = fixture.nativeElement.querySelectorAll('tbody tr:first-child td');

    expect(headers).toEqual([
      'מזהה שיוך',
      'שם פרויקט',
      'שם קטגוריה',
      'שם בית חולים',
      'מזהה יחידה',
      'שם יחידה',
      'סטטוס',
    ]);
    expect(firstRowCells[4].textContent.trim()).toBe('1549947');
  });

  it('highlights the current category and can filter the table to its rows', () => {
    expect(fixture.nativeElement.querySelectorAll('.current-context-row').length).toBe(1);
    expect(fixture.nativeElement.querySelector('.current-context-badge').textContent)
      .toContain('קטגוריה נוכחית');
    expect(fixture.nativeElement.querySelector('.current-context-project').textContent)
      .toContain('אשפוז');
    expect(fixture.nativeElement.querySelector('.current-context-category').textContent)
      .toContain('שחרור');

    const currentCategoryToggle: HTMLInputElement = fixture.nativeElement.querySelector(
      '.current-context-toggle input',
    );
    currentCategoryToggle.click();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelectorAll('tbody tr').length).toBe(1);
    expect(fixture.nativeElement.querySelector('tbody').textContent).toContain('פנימית א');
  });

  it('updates isActive after a successful PATCH', () => {
    const statusButton: HTMLButtonElement = fixture.nativeElement.querySelector('.status-button');

    statusButton.click();
    fixture.detectChanges();

    expect(apiService.updateSmsUnitCategoryStatus).toHaveBeenCalledOnceWith(1, false);
    expect(statusButton.textContent).toContain('לא פעיל');
  });

  it('filters the table locally without sending another API request', () => {
    const searchInput: HTMLInputElement = fixture.nativeElement.querySelector(
      '.settings-search-field input',
    );

    searchInput.value = 'יחידה שלא קיימת';
    searchInput.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('tbody').textContent)
      .toContain('לא נמצאו תוצאות התואמות לחיפוש');
    expect(apiService.getSmsUnitCategoryDetails).toHaveBeenCalledTimes(1);
  });

  it('paginates the filtered rows locally and resets to the first page after filtering', () => {
    const component = fixture.componentInstance;
    component.rows = Array.from({ length: 30 }, (_, index) => ({
      ucId: index + 1,
      projectId: 2,
      projectName: 'אשפוז',
      categoryId: 5,
      categoryName: 'שחרור',
      hospitalId: 20,
      hospitalName: 'בילינסון',
      smsUnitId: index + 101,
      unitId: index + 1549947,
      unitName: `מחלקה ${index + 1}`,
      isActive: true,
    }));
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelectorAll('tbody tr').length).toBe(25);
    expect(fixture.nativeElement.querySelector('.pagination-summary').textContent)
      .toContain('1–25 מתוך 30');

    component.goToPage(2);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelectorAll('tbody tr').length).toBe(5);
    expect(fixture.nativeElement.querySelector('.pagination-summary').textContent)
      .toContain('26–30 מתוך 30');

    component.updateTableFilter('מחלקה 1');
    expect(component.currentPage).toBe(1);
  });
});
