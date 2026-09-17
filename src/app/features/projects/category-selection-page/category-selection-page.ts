import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';

import { ApiService } from '../../../api.service';
import { SmsCategory } from '../../settings/settings.models';
import { UserService } from '../../../user.service';

@Component({
  selector: 'app-category-selection-page',
  standalone: true,
  templateUrl: './category-selection-page.html',
  styleUrls: [
    '../project-selection-page/project-selection-page.scss',
    './category-selection-page.scss',
  ],
})
export class CategorySelectionPage implements OnInit {
  private readonly apiService = inject(ApiService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  projectId = 0;
  projectName = '';
  categories: SmsCategory[] = [];
  categoriesLoading = false;
  categoriesError = '';
  userName = '';
  constructor(public userService: UserService) {
  }
  ngOnInit(): void {
    const projectId = Number(this.route.snapshot.paramMap.get('projectId'));
    if (!Number.isInteger(projectId) || projectId <= 0) {
      void this.router.navigate(['/projects']);
      return;
    }
    this.userService.user$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((user) => {
        if (user) {
          this.userName = user.fullName;
        }
      });
    this.projectId = projectId;
    this.loadCategories();
  }

  loadCategories(): void {
    this.categoriesLoading = true;
    this.categoriesError = '';

    this.apiService.getCategoriesByProject(this.projectId).subscribe({
      next: (categories) => {
        this.categories = categories;
        this.projectName = categories[0]?.categoryType ?? `פרויקט ${this.projectId}`;
        this.categoriesLoading = false;
      },
      error: (error) => {
        this.categories = [];
        this.categoriesLoading = false;
        this.categoriesError = 'לא ניתן לטעון את הקטגוריות מהשרת.';
        console.error('Failed to load SMS categories for project', error);
      },
    });
  }

  openCategory(category: SmsCategory): void {
    void this.router.navigate(['/templates'], {
      queryParams: {
        projectId: this.projectId,
        categoryId: category.categoryId,
      },
    });
  }

  backToProjects(): void {
    void this.router.navigate(['/projects']);
  }
}
