import { Component, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  NavigationEnd,
  Router,
  RouterLink,
  RouterLinkActive,
  RouterOutlet,
} from '@angular/router';
import { forkJoin } from 'rxjs';

import { ApiService } from './api.service';
import { AuthenticationFeedbackService } from './authentication-feedback.service';
import { UserService } from './user.service';

type AppTab = 'rules' | 'templates' | 'dictionaries' | 'settings';

@Component({
  selector: 'app-root',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './app.html',
  styleUrls: ['./app.scss'],
})
export class App implements OnInit {
  private readonly apiService = inject(ApiService);
  readonly authenticationFeedback = inject(AuthenticationFeedbackService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly router = inject(Router);
  private breadcrumbLoadSequence = 0;
  private breadcrumbScopeKey = '';

  readonly tabs: { id: AppTab; label: string; short: string }[] = [
    { id: 'templates', label: 'תבניות מסרון', short: '01' },
    { id: 'rules', label: 'כללי שליחה', short: '02' },
    { id: 'dictionaries', label: 'מילונים', short: '03' },
    { id: 'settings', label: 'הגדרות', short: '04' },
  ];

  userName = '';
  userInitials = '';
  projectId = 0;
  projectName = '';
  categoryName = '';

  get isProjectSelectionPage(): boolean {
    const path = this.router.url.split(/[?#]/, 1)[0];
    return path === '/'
      || path === '/projects'
      || /^\/projects\/\d+\/categories$/.test(path);
  }

  constructor(private userService: UserService) {


    const routerSubscription = this.router.events.subscribe((event) => {
      if (event instanceof NavigationEnd) {
        this.loadBreadcrumbs(event.urlAfterRedirects);
      }
    });
    this.loadBreadcrumbs(this.router.url);

    this.destroyRef.onDestroy(() => routerSubscription.unsubscribe());
  }
  ngOnInit(): void {
    this.userService.user$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((user) => {
        if (user) {
          this.userName = user.fullName;
          const spaceIndex = this.userName.indexOf(' ');
          this.userInitials = spaceIndex !== -1
            ? this.userName[0] + this.userName[spaceIndex + 1]
            : '';
        }
      });
  }

  private loadBreadcrumbs(url: string): void {
    const queryString = url.split('?', 2)[1]?.split('#', 1)[0] ?? '';
    const queryParams = new URLSearchParams(queryString);
    const projectId = Number(queryParams.get('projectId'));
    const categoryId = Number(queryParams.get('categoryId'));

    if (
      !Number.isInteger(projectId)
      || projectId <= 0
      || !Number.isInteger(categoryId)
      || categoryId <= 0
    ) {
      this.clearBreadcrumbs();
      return;
    }

    const scopeKey = `${projectId}:${categoryId}`;
    if (scopeKey === this.breadcrumbScopeKey && this.projectName && this.categoryName) {
      return;
    }

    this.breadcrumbScopeKey = scopeKey;
    const loadSequence = ++this.breadcrumbLoadSequence;
    forkJoin({
      projects: this.apiService.getProjects(),
      category: this.apiService.getCategory(categoryId),
    }).subscribe({
      next: ({ projects, category }) => {
        if (loadSequence !== this.breadcrumbLoadSequence) return;

        const project = projects.find((entry) => entry.projectId === projectId);
        if (!project || category.projectId !== projectId) {
          this.clearBreadcrumbs();
          return;
        }

        this.projectId = projectId;
        this.projectName = project.projectName;
        this.categoryName = category.categoryName;
      },
      error: () => {
        if (loadSequence === this.breadcrumbLoadSequence) {
          this.clearBreadcrumbs();
        }
      },
    });
  }

  private clearBreadcrumbs(): void {
    this.breadcrumbLoadSequence += 1;
    this.breadcrumbScopeKey = '';
    this.projectId = 0;
    this.projectName = '';
    this.categoryName = '';
  }
}
